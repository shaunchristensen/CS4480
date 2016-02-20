/*
*   Author:  Shaun Christensen
*   Course:  CS 4480 - Computer Networks
*   Created: 2016.01.31
*   Edited:  2016.02.09
*   Project: PA1
*   Summary: Build a web proxy capable of accepting HTTP requests, forwarding requests to remote (origin) servers, and returning response data to a client. The proxy must handle concurrent requests.
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Proxy
{
    class Program
    {
        static byte[] b;
        static int i, intPort;
        static string s, stringBody, stringHeader, stringHost, stringMethod, stringPath;

        static Match match;
        static MemoryStream memoryStream;
        static SHA1 sha1;
        static Socket socket;
        static Stream stream;
        static StreamReader streamReader;
        static TcpClient tcpClient;
        static TcpListener tcpListener;
        static WebClient webClient;

        static void Main(string[] args)
        {
            Console.Write("Starting proxy on localhost. Enter desired port number: ");

            // if the port number cannot be parsed or is negative then set the port number to 80 by default
            if (!int.TryParse(Console.ReadLine(), out intPort) || intPort < 0)
            {
                intPort = 80;
            }

            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), intPort);

            try
            {
                tcpListener.Start();

                Console.WriteLine("\nProxy is listening for connections on port " + intPort + ".\n");

                while (true)
                {
                    using (socket = tcpListener.AcceptSocket())
                    {
                        b = new byte[1024];
                        s = stringHeader = stringHost = string.Empty;

                        while (socket.Available > 0)
                        {
                            i = socket.Receive(b);
                            s += Encoding.UTF8.GetString(b, 0, i);
                        }

                        Console.Write("Request received. _" + s + "_");

                        match = Regex.Match(s, @"(?n)^\s*(?<Method>[A-Z]+)\s+(?<Path>\S+)\s+HTTP/1\.[01]\s*(?i)(?<Header>(\r\n\s*[a-z]+(-[a-z]+)*\s*:\s*\S(.*\S)?\s*)*)?((\r\n){2}(?<Body>\S(.*\S)?))?(\r\n){2}$");

                        // if the request syntax is valid then parse the request
                        if (match.Success)
                        {
                            // if the request specifies the get method then parse the request
                            if (match.Groups["Method"].Value == "GET")
                            {
                                stringMethod = match.Groups["Method"].Value;
                                stringBody = match.Groups["Body"].Value;

                                // if the request contains an absolute URL followed then extract the host and path
                                if (Regex.IsMatch(match.Groups["Path"].Value, @"(?i)^http://"))
                                {
                                    stringHost = Regex.Match(match.Groups["Path"].Value, @"(?in)^http://(\S+:\S*@)?(?<Host>\w([-\w]*\w)?(\.\w([-\w]*\w)?)+)(:\d+)?").Groups["Host"].Value;
                                    stringPath = Regex.Match(match.Groups["Path"].Value, stringHost + @"(?n)(:\d+)?(?<Path>/.*)$").Groups["Path"].Value;
                                }
                                // otherwise set the path
                                else
                                {
                                    stringPath = match.Groups["Path"].Value;
                                }

                                foreach (Match m in Regex.Matches(Regex.Replace(match.Groups["Header"].Value.Trim(), @"\r", ""), @"(?imn)^\s*(?<Name>[a-z]+(-[a-z]+)*)\s*:\s*(?<Value>\S(.*\S)?)\s*$"))
                                {
                                    // if the header is the connection then skip the header
                                    if (Regex.IsMatch(m.Groups["Name"].Value, @"(?i)^connection$"))// || (Regex.IsMatch(m.Groups["Name"].Value, @"(?i)encoding") && Regex.IsMatch(m.Groups["Value"].Value, @"(?i)gzip")))
                                    {
                                        continue;
                                    }
                                    // otherwise if the header is the host then check the host
                                    else if (Regex.IsMatch(m.Groups["Name"].Value, @"(?i)^host$"))
                                    {
                                        // if the host is empty then set the host
                                        if (stringHost.Length == 0)
                                        {
                                            stringHost = Regex.Replace(m.Groups["Value"].Value, @":\d+$", "");
                                        }
                                    }
                                    // otherwise append the header
                                    else
                                    {
                                        stringHeader += m.Groups["Name"].Value + ": " + m.Groups["Value"].Value + "\r\n";
                                    }
                                }

                                // 
                                if (stringHost.Length > 0)
                                {
                                    try
                                    {
                                        using (sha1 = new SHA1Managed())
                                        {
                                            using (webClient = new WebClient())
                                            {
                                                b = Encoding.UTF8.GetBytes(("whois -h hash.cymru.com " + BitConverter.ToString(sha1.ComputeHash(webClient.DownloadData("http://" + stringHost + stringPath))).Replace("-", "") + "\r\n").ToCharArray());
                                            }
                                        }

                                        using (tcpClient = new TcpClient())
                                        {
                                            tcpClient.Connect("hash.cymru.com", 43);

                                            using (stream = tcpClient.GetStream())
                                            {
                                                stream.Write(b, 0, b.Length);

                                                using (streamReader = new StreamReader(stream))
                                                {
                                                    s = streamReader.ReadToEnd();
                                                }
                                            }
                                        }
                                    }
                                    catch (ArgumentNullException e)
                                    {
                                        Console.WriteLine("Error: Unable to compute the hash. " + e.Message);
                                    }
                                    catch (OutOfMemoryException e)
                                    {
                                        Console.WriteLine("Error: Unable to receive the response. " + e.Message);
                                    }
                                    catch (SocketException e)
                                    {
                                        Console.WriteLine("Error: Unable to connect to the host. " + e.Message);
                                    }
                                    catch (WebException e)
                                    {
                                        Console.WriteLine("Error: Unable to download the data. " + e.Message);
                                    }
                                    catch (Exception e) when (e is ArgumentException || e is ArgumentOutOfRangeException)
                                    {
                                        Console.WriteLine("Error: Unable to send the request. " + e.Message);
                                    }
                                    catch (Exception e) when (e is IOException || e is NotSupportedException)
                                    {
                                        Console.WriteLine("Error: Unable to access the stream. " + e.Message);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Error: " + e.Message);
                                    }

                                    // fix me
                                    if (Regex.IsMatch(s, @"(?i)^\w+\s+\d+\s+\d+"))
                                    {
                                        b = Encoding.UTF8.GetBytes(("<!doctype html>\n<html>\n<head>\n\t<meta charset=\"utf-8\">\n\t<title>Error - Malware Detected</title>\n</head>\n<body>\n\t<h1>Error - Malware Detected</h1><hr><br /><br />\n\n\tUnable to fulfill the request. The file appears to contain malicious content.\n</body>\n</html>").ToCharArray());
                                    }
                                    // fix me
                                    else
                                    {
                                        b = Encoding.UTF8.GetBytes(("GET " + (stringPath.Length > 0 ? stringPath : "/") + " HTTP/1.0\r\nHost: " + stringHost + "\r\n" + stringHeader + "Connection: close\r\n\r\n" + stringBody).ToCharArray());

                                        try
                                        {
                                            using (tcpClient = new TcpClient())
                                            {
                                                tcpClient.Connect(stringHost, 80);

                                                using (stream = tcpClient.GetStream())
                                                {
                                                    stream.Write(b, 0, b.Length);

                                                    using (memoryStream = new MemoryStream())
                                                    {
                                                        while ((i = stream.Read(b, 0, b.Length)) > 0)
                                                        {
                                                            memoryStream.Write(b, 0, i);
                                                        }

                                                        b = memoryStream.ToArray();
                                                    }
                                                }
                                            }
                                        }
                                        catch (SocketException e)
                                        {
                                            b = Encoding.UTF8.GetBytes(("Error: Unable to connect to the host. " + e.Message).ToCharArray());
                                        }
                                        catch (Exception e) when (e is ArgumentException || e is ArgumentOutOfRangeException || e is ArgumentNullException || e is IOException || e is NotSupportedException)
                                        {
                                            b = Encoding.UTF8.GetBytes(("Error: Unable to access the stream. " + e.Message).ToCharArray());
                                        }
                                        catch (Exception e)
                                        {
                                            b = Encoding.UTF8.GetBytes(("Error: " + e.Message).ToCharArray());
                                        }
                                    }
                                }
                                // otherwise send a bad request response
                                else
                                {
                                    b = Encoding.UTF8.GetBytes(("Error: Unable to send the request. The host is required.").ToCharArray());
                                }
                            }
                            // otherwise send a not implemented response
                            else
                            {
                                b = Encoding.UTF8.GetBytes(("Error: Unable to send the request. 501 Not Implemented - The " + stringMethod + " method is unsupported.").ToCharArray());
                            }
                        }
                        // otherwise send a bad request response
                        else
                        {
                            b = Encoding.UTF8.GetBytes(("Error: Unable to send the request. 400 Bad Request - The request syntax is invalid.").ToCharArray());
                        }

                        socket.Send(b);

                        Console.WriteLine(" Response sent.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                tcpListener.Stop();
            }
        }
    }
}















































/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Network_Controller
{
    public class NetworkController
    {
        public delegate void CallbackFunction(StateObject state);

        private const int port = 11000;

        // ManualResetEvent instances signal completion, we need them so we can see if something is finished
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        private static String response = String.Empty;


        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
            public string outgoing = null;
            public CallbackFunction doWork;
            public CallbackFunction connectionFailed;
            public string returnVal;
            public TcpListener listener;
        }

        /// <summary>
        /// ---------------------------------------------------MAIN-----------------------------------------------------------------
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(String[] args)
        {
            Socket useMe = ConnectToServer(PrintItForMe, "localhost", FAIL);
            StateObject state = new StateObject();
            state.workSocket = useMe;

            Send(useMe, "$kill$\n");
            I_want_more_data(useMe, JustPrintIt);
            while (true)
            {
                string test = Console.ReadLine();
                if (test.ToUpper() == "END")
                    break;

                I_want_more_data(useMe, JustPrintIt);
                Send(useMe, "(move, 100, 100)");
            }
            useMe.Close();
            Console.Beep();
            Console.WriteLine("Connection terminated");
            Console.ReadLine();
            return 0;
        }

        private static void FAIL(StateObject state)
        {
            MessageBox.Show("Failure to connect");
            Environment.Exit(1);
        }

        private static void JustPrintIt(StateObject state)
        {
            string printMe = state.returnVal;
            Console.WriteLine(printMe);
        }

        private static void PrintItForMe(StateObject state)
        {
            Console.WriteLine("I'm in!");
        }







        public static Socket ConnectToServer(CallbackFunction callMe, string hostname, CallbackFunction ifIFail)
        {

            IPHostEntry ipHostInfo = Dns.Resolve(hostname);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            StateObject state = new StateObject();
            state.workSocket = client;
            state.doWork += callMe;
            state.connectionFailed += ifIFail;
            //Connect to the remote endpoint
            client.BeginConnect(remoteEP, new AsyncCallback(Connected_to_Server), state);
            return client;
        }

        public static void Connected_to_Server(IAsyncResult state_in_an_ar_object)
        {
            // Retrieve the socket from the state object
            StateObject state = (StateObject)state_in_an_ar_object.AsyncState;
            Socket client = state.workSocket;
            lock (client)
            {
                try
                {
                    // Complete the connection.
                    client.EndConnect(state_in_an_ar_object);
                }
                catch (SocketException)
                {
                    state.connectionFailed(state);
                    connectDone.Set();
                    return;
                }


                state.doWork(state);
                //Signal that the connection has been made.
                connectDone.Set();
            }
        }

        /// <summary>
        /// Request data from the server. This function will call the function stored in the passed in state's "doWork" CallbackFunction
        /// </summary>
        /// <param name="state">A StateObject containing the correct socket for working, and a "doWork" function.</param>
        public static void I_want_more_data(Socket client, CallbackFunction stringWork)
        {
            StateObject state = new StateObject();
            state.workSocket = client;
            state.buffer = new byte[StateObject.BufferSize];
            state.doWork = stringWork;
            client.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
        }

        public static void ReceiveCallback(IAsyncResult state_in_an_ar_object)
        {
            // Get the buffer to which the data was written.
            StateObject state = (StateObject)state_in_an_ar_object.AsyncState;
            Socket client = state.workSocket;
            // Figure out how many bytes have come in
            int bytes = client.EndReceive(state_in_an_ar_object);
            // Report that to the console and close our socket.
            if (bytes == 0)
            {
                Console.WriteLine("Socket closed");
                client.Close();
            }
            else
            {
                state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytes));
                //Console.WriteLine(state.sb);
                state.returnVal = state.sb.ToString();
                state.doWork(state);
                state.sb.Clear();
            }
        }

        public static void Send(Socket client, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            StateObject state = new StateObject();
            state.workSocket = client;
            state.doWork = (StateObject) => state.GetType(); //This does literally nothing.
            try
            {
                lock (client)
                {
                    client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), state);
                }
            }
            catch (SocketException)
            {
                Thread.Sleep(1);
                Send(client, data);
            }
            catch (ArgumentNullException)
            {
                Thread.Sleep(1);
                Send(client, data);
            }

        }

        public static void Send(Socket client, String data, CallbackFunction callback)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            StateObject state = new StateObject();
            state.doWork = callback;
            state.workSocket = client;
            lock (client)
            {
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), state);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            try
            {
                int bytesSent = client.EndSend(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Houston, we have a problem");
            }
            state.doWork(state);
        }




        // ----------------------------------------- STUFF FOR SERVER -------------------------------------//

        /// <summary>
        /// Opens a listener on the passed port.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="port"></param>
        public static void Server_awaiting_client_loop(CallbackFunction callback, int port)
        {
            // A TcpListener listens for incoming connection requests
            TcpListener server = new TcpListener(IPAddress.Any, port);

            // Start the TcpListener
            server.Start();

            // Store everything the callback will need in a state
            StateObject state = new StateObject();
            state.doWork = callback;
            state.listener = server;

            // Ask the server to call ConnectionRequested at some point in the future when 
            // a connection request arrives.  It could be a very long time until this happens.
            // The waiting and the calling will happen on another thread.  BeginAcceptSocket 
            // returns immediately, and the constructor returns to Main.
            server.BeginAcceptSocket(new AsyncCallback(Accept_a_new_client), state);
        }

        private static void Accept_a_new_client(IAsyncResult state_in_an_ar_object)
        {
            // Grab the state, and then tell the listener to STOP.
            StateObject state = (StateObject)state_in_an_ar_object.AsyncState;
            Socket client = state.listener.EndAcceptSocket(state_in_an_ar_object);

            // Store the socket in the state, we'll need it in the callback (likely)
            state.workSocket = client;

            // Create a new state object so that we can return the older one (with the socket in it)
            StateObject newState = new StateObject();
            newState.listener = state.listener;
            newState.doWork = state.doWork;

            // Start listeneing again (This happens on a different thread)
            newState.listener.BeginAcceptSocket(new AsyncCallback(Accept_a_new_client), newState);

            // Call our callback function assigned earlier.
            state.doWork(state);
        }
    }
}
*/