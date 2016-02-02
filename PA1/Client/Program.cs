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
        static string s1, s2, stringPort, stringRequest, stringResponse;

        static Encoding encoding;
        static Stream stream;
        static TcpClient tcpClient;
        static Timer timer;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting client on localhost. Enter two blank linkes successively to execute HTTP requests.\n");

/*            string[] ss1 = Regex.Split("GET http://google.com:11 HTTP/1.1", @"(\r\n)+");

            // delete me
            Console.WriteLine(ss1.Length);

            string[] ss2 = Regex.Split(ss1[0].Trim(), @"\s+");

            string stringMethod = ss2[0];
            Console.WriteLine("?" + ss2.Length + ss2[0] + ss2[1] + ss2[2]);
            */

            encoding = Encoding.UTF8;

            while (true)
            {
                Console.Write("Enter HTTP request: ");

                stringPort = stringRequest = stringResponse = string.Empty;

                s1 = Console.ReadLine();

                if (Regex.IsMatch(s1, @"(?i)http://\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:\d+"))
                {
                    stringPort = Regex.Replace(Regex.Replace(s1, @"(?i)^.*http://\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:", ""), @"\D+HTTP/1\.[01].*$", "");
                    s1 = Regex.Replace(s1, @":" + stringPort + @"\DHTTP/1\.[01].*$", "") + Regex.Replace(s1, @"(?i)^.*http://\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:" + stringPort, "");
                }

                stringRequest += s1 + "\r\n";

                do
                {
                    s2 = s1;
                    s1 = Console.ReadLine();

                    if (stringPort.Length == 0 && Regex.IsMatch(s1, @"(?i)host\s*:\s*\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:\d+"))
                    {
                        stringPort = Regex.Replace(Regex.Replace(s1, @"(?i)^.*host\s*:\s*\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:", ""), @"(\D+.*)?$", "");
                        s1 = Regex.Replace(s1, ":" + stringPort + @"(\D+.*)?$", "") + Regex.Replace(s1, @"(?i)^.*host\s*:\s*\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:" + stringPort, "");
                    }

                    stringRequest += s1 + "\r\n";
                } while (s1.Length > 0 || s2.Length > 0);

                if (Regex.IsMatch(stringRequest, @"\S+"))
                {
                    Console.CursorTop--;
                }

                boolRequest = true;
                bytes = encoding.GetBytes(stringRequest.ToCharArray());

                if (!int.TryParse(stringPort, out intPort))
                {
                    intPort = 80;
                }

                try
                {
                    using (tcpClient = new TcpClient())
                    {
                        tcpClient.Connect("127.0.0.1", intPort);

                        using (stream = tcpClient.GetStream())
                        {
                            stream.Write(bytes, 0, bytes.Length);
                            bytes = new byte[1024];

                            using (timer = new Timer(5000))
                            {
                                timer.Elapsed += TimerElapsed;
                                timer.Start();

                                while (boolRequest)
                                {
                                    intBytes = stream.Read(bytes, 0, 1024);

                                    if (intBytes > 0)
                                    {
                                        boolRequest = false;

                                        for (int i = 0; i < intBytes; i++)
                                        {
                                            stringResponse += Convert.ToChar(bytes[i]);
                                        }

                                        Console.WriteLine(stringResponse + "\n");

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Error: Unable to connec to proxy. " + e.Message + ".\n");
                }
            }
        }

        static void TimerElapsed(object source, ElapsedEventArgs e)
        {
            if (boolRequest)
            {
                timer.Stop();

                boolRequest = false;

                Console.WriteLine("Request timed out. Please try again.\n");
            }
        }
    }
    }