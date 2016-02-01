using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA1
{
    class Program
    {
        /*
        static void Main(string[] args)
        {
        }
        */
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