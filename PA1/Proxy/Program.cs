using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    class Program
    {
        static void Main(string[] args)
        {
            int intPort;

            Console.Write("Starting proxy on localhost. Enter desired port number: ");

            if (!int.TryParse(Console.ReadLine(), out intPort))
            {
                intPort = 80;
            }

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            TcpListener tcpListener = new TcpListener(ipAddress, intPort);

            while (true)
            {
                tcpListener.Start();

                Console.WriteLine("Proxy is listening for connections on port " + intPort + ".");

                Socket socket = tcpListener.AcceptSocket();

                byte[] bytes = new byte[1024];

                int intBytes = socket.Receive(bytes);

                string stringRequest, stringResponse;
                stringRequest = stringResponse = string.Empty;

                for (int i = 0; i < intBytes; i++)
                {
                    stringRequest += Convert.ToChar(bytes[i]);
                }

                Console.WriteLine("Request received.");

                // regex input
                // prepare request
                // send request
                // receive resposne
                // relay response

                ASCIIEncoding asciiEncoding = new ASCIIEncoding();

//                socket.Send(asciiEncoding.GetBytes(stringResponse));
                socket.Close();

                tcpListener.Stop();

                Console.WriteLine("Response sent.");
            }
        }
    }
}