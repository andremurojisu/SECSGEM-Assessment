using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MachineSimulatorB
{
    class Program
    {
        // Generic mapping: RPTID1 -> SVID1/SVID2, RPTID2 -> SVID3, CEID1/2 -> each report.
        static Dictionary<string, string[]> reportDefinitions = new Dictionary<string, string[]>();
        static Dictionary<string, string> eventReportLinks = new Dictionary<string, string>();
        static List<string> enabledCeids = new List<string>();
        static bool event1Sent = false;

        static void Main(string[] args)
        {
            int port = 5002;
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            Console.WriteLine("==============================================");
            Console.WriteLine("            MACHINE SIMULATOR B");
            Console.WriteLine("==============================================");
            Console.WriteLine("Equipment Model : SIMB");
            Console.WriteLine("Software Rev    : 1.0.0");
            Console.WriteLine("Listening       : 127.0.0.1:" + port);
            Console.WriteLine();
            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.AutoFlush = true;
                    event1Sent = false;
                    Console.WriteLine("Host connected.");
                    Console.WriteLine();
                    while (client.Connected)
                    {
                        string message = ReadSecsMessage(reader);
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            break;
                        }
                        PrintSecsMessage("[RECEIVED MESSAGE]", message);
                        string response = HandleMessage(message);
                        PrintSecsMessage("[SENDING MESSAGE]", response);
                        SendSecsMessage(writer, response);
                        // Send the event only after S1F4 so the request/response flow stays in order.
                        if (message.StartsWith("S1F3") && message.Contains("'SVID1'") && message.Contains("'SVID2'") && message.Contains("'SVID3'") && enabledCeids.Contains("CEID1") && !event1Sent)
                        {
                            event1Sent = true;
                            Thread.Sleep(300);
                            SendEvent1(writer, reader);
                        }
                    }
                }
                Console.WriteLine("Host disconnected.");
                Console.WriteLine();
            }
        }

        static string HandleMessage(string message)
        {
            if (message.StartsWith("S1F13"))
            {
                return
@"S1F14
<L[2]
  <B[1] 00>
  <L[2]
    <A[4] 'DaVinci200'>
    <A[5] 'DaVinci200 Version 4.9.3'>
  >
>";
            }
            if (message.StartsWith("S2F33") && message.Contains("<U4[1] 0>") && message.Contains("<L[0]"))
            {
                reportDefinitions.Clear();
                Console.WriteLine("All report definitions deleted.");
                Console.WriteLine();
                return
@"S2F34
<B[1] 00>";
            }
            if (message.StartsWith("S2F35") && message.Contains("<U4[1] 0>") && message.Contains("<L[0]"))
            {
                eventReportLinks.Clear();
                Console.WriteLine("All event-report links deleted.");
                Console.WriteLine();
                return
@"S2F36
<B[1] 00>";
            }
            if (message.StartsWith("S2F37") && message.Contains("false"))
            {
                enabledCeids.Clear();
                event1Sent = false;
                Console.WriteLine("All CEIDs disabled.");
                Console.WriteLine();
                return
@"S2F38
<B[1] 00>";
            }
            if (message.StartsWith("S2F33") && message.Contains("'RPTID1'") && message.Contains("'RPTID2'"))
            {
                reportDefinitions.Clear();
                reportDefinitions.Add("RPTID1", new string[] { "SVID1", "SVID2" });
                reportDefinitions.Add("RPTID2", new string[] { "SVID3" });
                Console.WriteLine("Report definitions stored.");
                Console.WriteLine("RPTID1 -> SVID1, SVID2");
                Console.WriteLine("RPTID2 -> SVID3");
                Console.WriteLine();
                return
@"S2F34
<B[1] 00>";
            }
            if (message.StartsWith("S2F35") && message.Contains("'CEID1'") && message.Contains("'CEID2'"))
            {
                eventReportLinks.Clear();
                eventReportLinks.Add("CEID1", "RPTID1");
                eventReportLinks.Add("CEID2", "RPTID2");
                Console.WriteLine("Event reports linked.");
                Console.WriteLine("CEID1 -> RPTID1");
                Console.WriteLine("CEID2 -> RPTID2");
                Console.WriteLine();
                return
@"S2F36
<B[1] 00>";
            }
            if (message.StartsWith("S2F37") && message.Contains("true") && message.Contains("'CEID1'"))
            {
                enabledCeids.Clear();
                enabledCeids.Add("CEID1");
                enabledCeids.Add("CEID2");
                Console.WriteLine("CEIDs enabled.");
                Console.WriteLine("CEID1, CEID2");
                Console.WriteLine();
                return
@"S2F38
<B[1] 00>";
            }
            if (message.StartsWith("S1F3") && message.Contains("'SVID1'") && message.Contains("'SVID2'") && message.Contains("'SVID3'"))
            {
                return
@"S1F4
<L[3]
  <A[6] 'VALUE1'>
  <A[6] 'VALUE2'>
  <A[6] 'VALUE3'>
>";
            }
            return
@"UNKNOWN_MESSAGE";
        }

        static void SendEvent1(StreamWriter writer, StreamReader reader)
        {
            Console.WriteLine("EVENT1 TRIGGERED");
            Console.WriteLine("CEID1 -> RPTID1");
            Console.WriteLine();
            string s6f11 =
@"S6F11 W
<L[3]

  <U4[1] 1>

  <A[5] 'CEID1'>

  <L[1]

    <L[2]

      <A[6] 'RPTID1'>

      <L[2]
        <A[6] 'VALUE1'>
        <A[6] 'VALUE2'>
      >

    >

  >

>";
            PrintSecsMessage("[SENDING MESSAGE]", s6f11);
            SendSecsMessage(writer, s6f11);
            string response = ReadSecsMessage(reader);
            if (string.IsNullOrWhiteSpace(response))
            {
                throw new IOException("Host connection closed.");
            }
            PrintSecsMessage("[RECEIVED MESSAGE]", response);
            if (response.StartsWith("S6F12"))
            {
                Console.WriteLine("EVENT1 acknowledged.");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Unexpected response to EVENT1.");
                Console.WriteLine();
            }
        }

        static void SendSecsMessage(StreamWriter writer, string message)
        {
            writer.WriteLine(message);
            writer.WriteLine("<END>");
            writer.Flush();
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
