using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Middleware
{
    class Program
    {
        static readonly string MachineAHost = "127.0.0.1";
        static readonly int MachineAPort = 5001;
        static readonly string MachineBHost = "127.0.0.1";
        static readonly int MachineBPort = 5002;
        static readonly int ReconnectDelayMs = 3000;
        static string telemetryEndpoint = "";

        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Console.WriteLine("==============================================");
            Console.WriteLine("              SECS/GEM MIDDLEWARE");
            Console.WriteLine("==============================================");
            Console.WriteLine();
            // Run both machine connections in parallel.
            Thread machineAThread = new Thread(RunMachineA);
            Thread machineBThread = new Thread(RunMachineB);
            machineAThread.Start();
            machineBThread.Start();
            machineAThread.Join();
            machineBThread.Join();
        }

        static void RunMachineA()
        {
            bool displayConnectionFailure = true;
            while (true)
            {
                TcpClient client = null;
                try
                {
                    if (displayConnectionFailure)
                    {
                        Log("Trying to connect to Machine A at " + MachineAHost + ":" + MachineAPort + "...");
                    }
                    client = new TcpClient();
                    try
                    {
                        client.Connect(MachineAHost, MachineAPort);
                    }
                    catch (SocketException ex)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch
                        {
                        }
                        if (displayConnectionFailure)
                        {
                            Log("Machine A connection failed: " + ex.Message);
                            Console.WriteLine();
                        }
                        displayConnectionFailure = false;
                        Thread.Sleep(ReconnectDelayMs);
                        continue;
                    }
                    Log("Connected to Machine A at " + MachineAHost + ":" + MachineAPort + ".");
                    Log("Starting SECS/GEM communication setup...");
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    using (client)
                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.AutoFlush = true;
                        RunMachineASetupSequence(writer, reader);
                        Console.WriteLine();
                        Log("==============================================");
                        Log("       MACHINE A COMMUNICATION READY");
                        Log("==============================================");
                        Console.WriteLine();
                        ReceiveMachineAMessages(writer, reader);
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine();
                    Log("Connection to Machine A lost.");
                    Log("Communication error: " + ex.Message);
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    Thread.Sleep(ReconnectDelayMs);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine();
                    Log("Connection to Machine A lost.");
                    Log("Socket error: " + ex.Message);
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    Thread.Sleep(ReconnectDelayMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Log("Communication error: " + ex.Message);
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    Thread.Sleep(ReconnectDelayMs);
                }
                finally
                {
                    if (client != null)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        static void RunMachineB()
        {
            bool displayConnectionFailure = true;
            while (true)
            {
                TcpClient client = null;
                try
                {
                    if (displayConnectionFailure)
                    {
                        Log("Trying to connect to Machine B at " + MachineBHost + ":" + MachineBPort + "...");
                    }
                    client = new TcpClient();
                    try
                    {
                        client.Connect(MachineBHost, MachineBPort);
                    }
                    catch (SocketException ex)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch
                        {
                        }
                        if (displayConnectionFailure)
                        {
                            Log("Machine B connection failed: " + ex.Message);
                            Console.WriteLine();
                        }
                        displayConnectionFailure = false;
                        Thread.Sleep(ReconnectDelayMs);
                        continue;
                    }
                    Log("Connected to Machine B at " + MachineBHost + ":" + MachineBPort + ".");
                    Log("Starting Machine B SECS/GEM communication setup...");
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    using (client)
                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.AutoFlush = true;
                        RunMachineBSetupSequence(writer, reader);
                        Console.WriteLine();
                        Log("==============================================");
                        Log("       MACHINE B COMMUNICATION READY");
                        Log("==============================================");
                        Console.WriteLine();
                        ReceiveMachineBMessages(writer, reader);
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine();
                    Log("Connection to Machine B lost.");
                    Log("Communication error: " + ex.Message);
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    Thread.Sleep(ReconnectDelayMs);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine();
                    Log("Connection to Machine B lost.");
                    Log("Socket error: " + ex.Message);
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    Thread.Sleep(ReconnectDelayMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Log("Machine B communication error: " + ex.Message);
                    Console.WriteLine();
                    displayConnectionFailure = true;
                    Thread.Sleep(ReconnectDelayMs);
                }
                finally
                {
                    if (client != null)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        static void RunMachineASetupSequence(StreamWriter writer, StreamReader reader)
        {
            string s1f13 =
@"S1F13 W
<L[0]
>";
            SendAndReceiveA(writer, reader, s1f13, "S1F14");
            string s2f33Clear =
@"S2F33 W
<L[2]
  <U4[1] 0>
  <L[0]
  >
>";
            SendAndReceiveA(writer, reader, s2f33Clear, "S2F34");
            string s2f35Clear =
@"S2F35 W
<L[2]
  <U4[1] 0>
  <L[0]
  >
>";
            SendAndReceiveA(writer, reader, s2f35Clear, "S2F36");
            string s2f37Disable =
@"S2F37 W
<L[2]
  <Bool[5] false>
  <L[0]
  >
>";
            SendAndReceiveA(writer, reader, s2f37Disable, "S2F38");
            string s2f33Define =
@"S2F33 W
<L[2]
  <U4[3] 100>
  <L[3]

    <L[2]
      <U4[1] 1>
      <L[2]
        <U4[4] 1835>
        <U4[4] 1836>
      >
    >

    <L[2]
      <U4[1] 2>
      <L[2]
        <U4[4] 1830>
        <U4[4] 1831>
      >
    >

    <L[2]
      <U4[1] 3>
      <L[1]
        <U4[4] 4306>
      >
    >

  >
>";
            SendAndReceiveA(writer, reader, s2f33Define, "S2F34");
            string s2f35Link =
@"S2F35 W
<L[5]
  <U4[1] 0>

  <L[1]
    <L[2]
      <U4[1] 4>
      <L[1]
        <U4[1] 1>
      >
    >
  >

  <L[1]
    <L[2]
      <U4[1] 5>
      <L[1]
        <U4[1] 2>
      >
    >
  >

  <L[1]
    <L[2]
      <U4[3] 140>
      <L[1]
        <U4[1] 3>
      >
    >
  >

  <L[1]
    <L[2]
      <U4[3] 141>
      <L[1]
        <U4[1] 3>
      >
    >
  >

>";
            SendAndReceiveA(writer, reader, s2f35Link, "S2F36");
            string s2f37Enable =
@"S2F37 W
<L[2]
  <Bool[4] true>
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
            SendAndReceiveA(writer, reader, s2f37Enable, "S2F38");
            string s1f3Events =
@"S1F3 W
<L[1]
  <U4[2] 12>
>";
            SendAndReceiveA(writer, reader, s1f3Events, "S1F4");
            string s2f41Remote =
@"S2F41 W
<L[1]
  <A[6] 'REMOTE'>
>";
            SendAndReceiveA(writer, reader, s2f41Remote, "S2F42");
            string s1f3ControlState =
@"S1F3 W
<L[1]
  <U4[2] 11>
>";
            string controlResponse = SendAndReceiveA(writer, reader, s1f3ControlState, "S1F4");
            if (controlResponse.Contains("<U4[1] 5>"))
            {
                Log("Control State verified: ONLINE REMOTE.");
            }
            string s2f41PpSelect =
@"S2F41 W
<L[2]
  <A[8] 'PPSELECT'>
  <L[5]

    <L[2]
      <A[4] 'PPID'>
      <A[4] 'PPID'>
    >

    <L[2]
      <A[6] 'PORTID'>
      <U2[1] 1>
    >

    <L[2]
      <A[9] 'CARRIERID'>
      <A[9] 'CARRIERID'>
    >

    <L[2]
      <A[5] 'LOTID'>
      <A[5] 'LOTID'>
    >

    <L[2]
      <A[5] 'JOBID'>
      <A[5] 'JOBID'>
    >

  >
>";
            SendAndReceiveA(writer, reader, s2f41PpSelect, "S2F42");
        }

        static void RunMachineBSetupSequence(StreamWriter writer, StreamReader reader)
        {
            string s1f13 =
@"S1F13 W
<L[0]
>";
            SendAndReceiveB(writer, reader, s1f13, "S1F14");
            string s2f33Clear =
@"S2F33 W
<L[2]
  <U4[1] 0>
  <L[0]
  >
>";
            SendAndReceiveB(writer, reader, s2f33Clear, "S2F34");
            string s2f35Clear =
@"S2F35 W
<L[2]
  <U4[1] 0>
  <L[0]
  >
>";
            SendAndReceiveB(writer, reader, s2f35Clear, "S2F36");
            string s2f37Disable =
@"S2F37 W
<L[2]
  <Bool[5] false>
  <L[0]
  >
>";
            SendAndReceiveB(writer, reader, s2f37Disable, "S2F38");
            string s2f33Define =
@"S2F33 W
<L[2]

  <U4[1] 1>

  <L[2]

    <L[2]
      <A[6] 'RPTID1'>
      <L[2]
        <A[5] 'SVID1'>
        <A[5] 'SVID2'>
      >
    >

    <L[2]
      <A[6] 'RPTID2'>
      <L[1]
        <A[5] 'SVID3'>
      >
    >

  >

>";
            SendAndReceiveB(writer, reader, s2f33Define, "S2F34");
            string s2f35Link =
@"S2F35 W
<L[3]

  <U4[1] 0>

  <L[1]
    <L[2]
      <A[5] 'CEID1'>
      <L[1]
        <A[6] 'RPTID1'>
      >
    >
  >

  <L[1]
    <L[2]
      <A[5] 'CEID2'>
      <L[1]
        <A[6] 'RPTID2'>
      >
    >
  >

>";
            SendAndReceiveB(writer, reader, s2f35Link, "S2F36");
            string s2f37Enable =
@"S2F37 W
<L[2]

  <Bool[4] true>

  <L[2]
    <A[5] 'CEID1'>
    <A[5] 'CEID2'>
  >

>";
            SendAndReceiveB(writer, reader, s2f37Enable, "S2F38");
            string s1f3 =
@"S1F3 W
<L[3]
  <A[5] 'SVID1'>
  <A[5] 'SVID2'>
  <A[5] 'SVID3'>
>";
            SendAndReceiveB(writer, reader, s1f3, "S1F4");
        }

        static void ReceiveMachineAMessages(StreamWriter writer, StreamReader reader)
        {
            while (true)
            {
                string message = ReadSecsMessage(reader);
                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new IOException("Machine A connection closed.");
                }
                PrintSecsMessage("[MACHINE A RECEIVED MESSAGE]", message);
                if (message.StartsWith("S6F11"))
                {
                    ProcessMachineAS6F11(writer, message);
                    continue;
                }
                Log("Machine A unsolicited equipment message received.");
                Console.WriteLine();
            }
        }

        static void ReceiveMachineBMessages(StreamWriter writer, StreamReader reader)
        {
            while (true)
            {
                string message = ReadSecsMessage(reader);
                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new IOException("Machine B connection closed.");
                }
                PrintSecsMessage("[MACHINE B RECEIVED MESSAGE]", message);
                if (message.StartsWith("S6F11"))
                {
                    ProcessMachineBS6F11(writer, message);
                    continue;
                }
                Log("Machine B unsolicited equipment message received.");
                Console.WriteLine();
            }
        }

        static void ProcessMachineAS6F11(StreamWriter writer, string message)
        {
            string s6f12 =
@"S6F12
<B[1] 00>";
            PrintSecsMessage("[MACHINE A SENDING MESSAGE]", s6f12);
            SendSecsMessage(writer, s6f12);
            if (message.Contains("<U4[2] 13>"))
            {
                Log("Machine A CEID 13: processRecipeSelected");
                SendMachineATelemetry(13, "processRecipeSelected");
                Console.WriteLine();
                return;
            }
            Log("Machine A S6F11 Event Report received.");
            Console.WriteLine();
        }

        static void ProcessMachineBS6F11(StreamWriter writer, string message)
        {
            string s6f12 =
@"S6F12
<B[1] 00>";
            PrintSecsMessage("[MACHINE B SENDING MESSAGE]", s6f12);
            SendSecsMessage(writer, s6f12);
            if (message.Contains("'CEID1'"))
            {
                Log("Machine B CEID1: EVENT1");
                SendMachineBTelemetry("CEID1", "EVENT1");
                Console.WriteLine();
                return;
            }
            if (message.Contains("'CEID2'"))
            {
                Log("Machine B CEID2: EVENT2");
                SendMachineBTelemetry("CEID2", "EVENT2");
                Console.WriteLine();
                return;
            }
            Log("Machine B S6F11 Event Report received.");
            Console.WriteLine();
        }

        static void SendMachineATelemetry(int ceid, string eventName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            string json = "{" + "\"machine\":\"MachineA\"," + "\"timestamp\":\"" + EscapeJson(timestamp) + "\"," + "\"ceid\":" + ceid + "," + "\"event\":\"" + EscapeJson(eventName) + "\"" + "}";
            Log("[MACHINE A HTTP POST]");
            Log("Payload: " + json);
            SendTelemetry("Machine A", json);
        }

        static void SendMachineBTelemetry(string ceid, string eventName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            string json = "{" + "\"machine\":\"MachineB\"," + "\"timestamp\":\"" + EscapeJson(timestamp) + "\"," + "\"ceid\":\"" + EscapeJson(ceid) + "\"," + "\"event\":\"" + EscapeJson(eventName) + "\"" + "}";
            Log("[MACHINE B HTTP POST]");
            Log("Payload: " + json);
            SendTelemetry("Machine B", json);
        }

        static void SendTelemetry(string machineName, string json)
        {
            if (string.IsNullOrWhiteSpace(telemetryEndpoint))
            {
                Log(machineName + " HTTP POST skipped: endpoint empty.");
                return;
            }
            try
            {
                string response = PostJson(telemetryEndpoint, json);
                Log(machineName + " HTTP POST successful.");
                if (!string.IsNullOrWhiteSpace(response))
                {
                    Log("HTTP Response: " + response);
                }
            }
            catch (WebException ex)
            {
                Log(machineName + " HTTP POST failed: " + GetWebExceptionMessage(ex));
            }
            catch (Exception ex)
            {
                Log(machineName + " HTTP POST failed: " + ex.Message);
            }
        }

        static string PostJson(string url, string json)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.ContentLength = data.Length;
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        return "";
                    }
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        static string GetWebExceptionMessage(WebException ex)
        {
            if (ex.Response == null)
            {
                return ex.Message;
            }
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)ex.Response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream == null)
                        {
                            return ((int)response.StatusCode).ToString() + " " + response.StatusDescription;
                        }
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string body = reader.ReadToEnd();
                            return ((int)response.StatusCode).ToString() + " " + response.StatusDescription + " - " + body;
                        }
                    }
                }
            }
            catch
            {
                return ex.Message;
            }
        }

        static string SendAndReceiveA(StreamWriter writer, StreamReader reader, string request, string expectedResponse)
        {
            PrintSecsMessage("[MACHINE A SENDING MESSAGE]", request);
            SendSecsMessage(writer, request);
            string response = ReadSecsMessage(reader);
            if (string.IsNullOrWhiteSpace(response))
            {
                throw new IOException("Machine A connection closed.");
            }
            PrintSecsMessage("[MACHINE A RECEIVED MESSAGE]", response);
            if (!response.StartsWith(expectedResponse))
            {
                throw new Exception("Machine A unexpected response. Expected " + expectedResponse + " but received " + GetMessageName(response) + ".");
            }
            return response;
        }

        static string SendAndReceiveB(StreamWriter writer, StreamReader reader, string request, string expectedResponse)
        {
            PrintSecsMessage("[MACHINE B SENDING MESSAGE]", request);
            SendSecsMessage(writer, request);
            string response = ReadSecsMessage(reader);
            if (string.IsNullOrWhiteSpace(response))
            {
                throw new IOException("Machine B connection closed.");
            }
            PrintSecsMessage("[MACHINE B RECEIVED MESSAGE]", response);
            if (!response.StartsWith(expectedResponse))
            {
                throw new Exception("Machine B unexpected response. Expected " + expectedResponse + " but received " + GetMessageName(response) + ".");
            }
            return response;
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

        static string EscapeJson(string value)
        {
            if (value == null)
            {
                return "";
            }
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        static string GetMessageName(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "EMPTY";
            }
            string normalized = message.Replace("\r\n", "\n");
            int index = normalized.IndexOf('\n');
            if (index >= 0)
            {
                return normalized.Substring(0, index);
            }
            return normalized;
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
