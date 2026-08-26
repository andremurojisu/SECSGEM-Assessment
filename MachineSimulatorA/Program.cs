using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MachineSimulatorA
{
    class Program
    {
        static readonly string EquipmentModel = "MG22";
        static readonly string SoftwareRevision = "3.7.0.0";
        static readonly string ListenIp = "127.0.0.1";
        static readonly int ListenPort = 5001;
        // SVID 11: 1 = initial state, 5 = Online Remote.
        static int controlState = 1;
        static Dictionary<int, int[]> reportDefinitions = new Dictionary<int, int[]>();
        static Dictionary<int, int> eventReportLinks = new Dictionary<int, int>();
        static List<int> enabledCeids = new List<int>();
        static string selectedPpid = "";
        static int selectedPortId = 0;
        static string selectedCarrierId = "";
        static string selectedLotId = "";
        static string selectedJobId = "";

        static void Main(string[] args)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Parse(ListenIp), ListenPort);
                listener.Start();
                Console.WriteLine("==============================================");
                Console.WriteLine("            MACHINE SIMULATOR A");
                Console.WriteLine("==============================================");
                Log("Equipment Model : " + EquipmentModel);
                Log("Software Rev    : " + SoftwareRevision);
                Log("Listening       : " + ListenIp + ":" + ListenPort);
                Console.WriteLine();
                while (true)
                {
                    Log("Waiting for host connection...");
                    Console.WriteLine();
                    TcpClient client = listener.AcceptTcpClient();
                    Log("Host connected.");
                    Console.WriteLine();
                    ResetSessionState();
                    try
                    {
                        using (client)
                        using (NetworkStream stream = client.GetStream())
                        using (StreamReader reader = new StreamReader(stream))
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.AutoFlush = true;
                            ProcessHostConnection(reader, writer);
                        }
                    }
                    catch (IOException)
                    {
                    }
                    catch (SocketException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Log("Communication error: " + ex.Message);
                    }
                    Console.WriteLine();
                    Log("Host disconnected.");
                    Console.WriteLine();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine();
                Log("Unable to start Machine A.");
                Log("Socket error: " + ex.Message);
                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit.");
                Console.ReadLine();
            }
            finally
            {
                if (listener != null)
                {
                    try
                    {
                        listener.Stop();
                    }
                    catch
                    {
                    }
                }
            }
        }

        static void ResetSessionState()
        {
            controlState = 1;
            reportDefinitions.Clear();
            eventReportLinks.Clear();
            enabledCeids.Clear();
            selectedPpid = "";
            selectedPortId = 0;
            selectedCarrierId = "";
            selectedLotId = "";
            selectedJobId = "";
        }

        static void ProcessHostConnection(StreamReader reader, StreamWriter writer)
        {
            while (true)
            {
                string message = ReadSecsMessage(reader);
                if (string.IsNullOrWhiteSpace(message))
                {
                    return;
                }
                PrintSecsMessage("[RECEIVED MESSAGE]", message);
                if (message.StartsWith("S6F12"))
                {
                    Log("S6F12 acknowledgement received.");
                    Console.WriteLine();
                    continue;
                }
                string response = HandleHostMessage(message);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    PrintSecsMessage("[SENDING MESSAGE]", response);
                    SendSecsMessage(writer, response);
                }
                if (message.StartsWith("S2F41") && message.Contains("'PPSELECT'") && enabledCeids.Contains(13))
                {
                    Thread.Sleep(100);
                    string s6f11 = BuildProcessRecipeSelectedEvent();
                    Log("CEID 13 triggered: processRecipeSelected.");
                    PrintSecsMessage("[SENDING MESSAGE]", s6f11);
                    SendSecsMessage(writer, s6f11);
                }
            }
        }

        static string HandleHostMessage(string message)
        {
            if (message.StartsWith("S1F13"))
            {
                return
@"S1F14
<L[2]
  <B[1] 00>
  <L[2]
    <A[4] 'MG22'>
    <A[7] '3.7.0.0'>
  >
>";
            }
            if (message.StartsWith("S2F33") && message.Contains("<U4[1] 0>") && message.Contains("<L[0]"))
            {
                reportDefinitions.Clear();
                return
@"S2F34
<B[1] 00>";
            }
            if (message.StartsWith("S2F35") && message.Contains("<U4[1] 0>") && message.Contains("<L[0]"))
            {
                eventReportLinks.Clear();
                return
@"S2F36
<B[1] 00>";
            }
            if (message.StartsWith("S2F37") && message.Contains("false"))
            {
                enabledCeids.Clear();
                return
@"S2F38
<B[1] 00>";
            }
            if (message.StartsWith("S2F33") && message.Contains("<U4[3] 100>"))
            {
                reportDefinitions.Clear();
                reportDefinitions.Add(1, new int[] { 1835, 1836 });
                reportDefinitions.Add(2, new int[] { 1830, 1831 });
                reportDefinitions.Add(3, new int[] { 4306 });
                return
@"S2F34
<B[1] 00>";
            }
            if (message.StartsWith("S2F35") && message.Contains("<L[5]") && message.Contains("<U4[1] 4>") && message.Contains("<U4[1] 5>") && message.Contains("<U4[3] 140>") && message.Contains("<U4[3] 141>"))
            {
                eventReportLinks.Clear();
                eventReportLinks.Add(4, 1);
                eventReportLinks.Add(5, 2);
                eventReportLinks.Add(140, 3);
                eventReportLinks.Add(141, 3);
                return
@"S2F36
<B[1] 00>";
            }
            if (message.StartsWith("S2F37") && message.Contains("true"))
            {
                enabledCeids.Clear();
                enabledCeids.Add(4);
                enabledCeids.Add(5);
                enabledCeids.Add(13);
                enabledCeids.Add(130);
                enabledCeids.Add(131);
                enabledCeids.Add(140);
                enabledCeids.Add(141);
                return
@"S2F38
<B[1] 00>";
            }
            if (message.StartsWith("S1F3") && message.Contains("<U4[2] 12>"))
            {
                return
@"S1F4
<L[1]
  <L[7]
    <U4[1] 4>
    <U4[1] 5>
    <U4[2] 13>
    <U4[3] 130>
    <U4[3] 131>
    <U4[3] 140>
    <U4[3] 141>
  >
>";
            }
            if (message.StartsWith("S2F41") && message.Contains("'REMOTE'"))
            {
                controlState = 5;
                return
@"S2F42
<L[2]
  <B[1] 00>
  <L[0]
  >
>";
            }
            if (message.StartsWith("S1F3") && message.Contains("<U4[2] 11>"))
            {
                return
@"S1F4
<L[1]
  <U4[1] " +
                    controlState +
@">
>";
            }
            if (message.StartsWith("S2F41") && message.Contains("'PPSELECT'"))
            {
                selectedPpid = "PPID";
                selectedPortId = 1;
                selectedCarrierId = "CARRIERID";
                selectedLotId = "LOTID";
                selectedJobId = "JOBID";
                Log("PPSELECT accepted.");
                Log("PPID      : " + selectedPpid);
                Log("PORTID    : " + selectedPortId);
                Log("CARRIERID : " + selectedCarrierId);
                Log("LOTID     : " + selectedLotId);
                Log("JOBID     : " + selectedJobId);
                return
@"S2F42
<L[2]
  <B[1] 04>
  <L[0]
  >
>";
            }
            return
@"UNKNOWN_MESSAGE";
        }

        static string BuildProcessRecipeSelectedEvent()
        {
            return
@"S6F11 W
<L[3]
  <U4[1] 0>
  <U4[2] 13>
  <L[0]
  >
>";
        }

        static string ReadSecsMessage(StreamReader reader)
        {
            string message = "";
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line == "<END>")
                {
                    break;
                }
                if (message.Length > 0)
                {
                    message += Environment.NewLine;
                }
                message += line;
            }
            return message;
        }

        static void SendSecsMessage(StreamWriter writer, string message)
        {
            writer.WriteLine(message);
            writer.WriteLine("<END>");
            writer.Flush();
        }

        static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "    " + message);
        }

        static void PrintSecsMessage(string header, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine(timestamp + "    " + header);
            string[] lines = message.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Console.WriteLine(timestamp + "    " + lines[i]);
            }
            Console.WriteLine();
        }
    }
}
