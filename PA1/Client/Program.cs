using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace Client
{
    class Program
    {
        static bool boolRequest;
        static byte[] bytes;
        static int intBytes, intPort;
        static string s1, s2, stringRequest, stringResponse;

        static Encoding encoding;
        static Stream stream;
        static Timer timer;

        static void Main(string[] args)
        {
            encoding = Encoding.UTF8;
            timer = new Timer(5000);
            timer.Elapsed += TimerElapsed;

            Console.WriteLine("Starting client on localhost. Enter two blank linkes successively to execute HTTP requests.\n");

            while (true)
            {
                stringRequest = stringResponse = string.Empty;

                Console.Write("Enter HTTP request: ");

                if (ReadInput())
                {
                    return;
                }

                do
                {
                    s2 = s1;

                    if (ReadInput())
                    {
                        return;
                    }
                } while (s1.Length > 0 || s2.Length > 0);

                if (Regex.IsMatch(stringRequest, @"(?i)(http://|host:\s*)\w+(\.\w+)+:(\d+)"))
                {
                    Console.WriteLine("Port");
                    // capture?
                    intPort = 80;
                }
                else
                {
                    intPort = 80;
                }

                try
                {
                    using (TcpClient tcpClient = new TcpClient())
                    {
                        tcpClient.Connect("127.0.0.1", intPort);
                        stream = tcpClient.GetStream();

                        bytes = encoding.GetBytes(stringRequest.ToCharArray());
                        stream.Write(bytes, 0, bytes.Length);

                        boolRequest = true;
                        bytes = new byte[1024];

                        timer.Start();

                        while (boolRequest)
                        {
                            intBytes = stream.Read(bytes, 0, 1024);

                            if (intBytes > 0)
                            {
                                for (int i = 0; i < intBytes; i++)
                                {
                                    stringResponse += Convert.ToChar(bytes[i]);
                                }

                                Console.WriteLine(stringResponse + "\n");

                                break;
                            }
                        }

                        tcpClient.Close();
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Error: Unable to connec to proxy. " + e.Message + ".\n");
                }
            }
        }

        public static bool ReadInput()
        {
            s1 = Console.ReadLine();

            if (Regex.IsMatch(s1, @"^(?i)(close|disconnect|exit|log\s*out|quit)$"))
            {
                return true;
            }

            stringRequest += s1 + "\r\n";

            return false;
        }

        public static void TimerElapsed(object source, ElapsedEventArgs e)
        {
            timer.Stop();

            boolRequest = false;

            Console.WriteLine("Request timed out. Please try again.\n");
        }
    }
}