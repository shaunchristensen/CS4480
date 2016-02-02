using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Proxy
{
    class Program
    {
        static byte[] bytes;
        static int intBytes, intPort;
        static string stringBadRequest, stringHost, stringMethod, stringPath, stringRequest, stringResponse, stringVersion;
        static string[] s1, s2;

        static Encoding encoding;
        static HttpWebRequest httpWebRequest;
        static HttpWebResponse httpWebResponse;
        static IPAddress ipAddress;
        static Socket socket;
        static Stream stream;
        static StreamReader streamReader;
        static TcpListener tcpListener;
        static WebHeaderCollection webHeaderCollection;

        static void Main(string[] args)
        {
            Console.Write("Starting proxy on localhost. Enter desired port number: ");

            if (!int.TryParse(Console.ReadLine(), out intPort) || intPort < 0)
            {
                intPort = 80;
            }

            encoding = Encoding.UTF8;
            stringBadRequest = "Unable to send request. 400 Bad Request - The request is malformed.";

            ipAddress = IPAddress.Parse("127.0.0.1");
            tcpListener = new TcpListener(ipAddress, intPort);

            try
            {
                tcpListener.Start();

                Console.WriteLine("Proxy is listening for connections on port " + intPort + ".");

                while (true)
                {
                    bytes = new byte[1024];
                    stringHost = stringRequest = stringResponse = string.Empty;

                    using (socket = tcpListener.AcceptSocket())
                    {
                        intBytes = socket.Receive(bytes);

                        for (int i = 0; i < intBytes; i++)
                        {
                            stringRequest += Convert.ToChar(bytes[i]);
                        }

                        Console.Write("Request received.");

                        if (Regex.IsMatch(stringRequest, @"^\s*[A-Z]+\s*\S+\s*HTTP/1\.[01]\s*(?i)(\r\n\s*[a-z]+(-[a-z]+)*\s*:\s*\S+\s*)*((\r\n){1,2}.*)?\s*$"))
                        {
                            s1 = Regex.Split(stringRequest.Trim(), @"(\r\n)+");
                            s2 = Regex.Split(s1[0].Trim(), @"\s+");

                            stringMethod = s2[0];

                            if (Regex.IsMatch(s2[1], @"(?i)^http://"))
                            {
                                stringPath = Regex.Replace(s2[1], @"(?i)^http://\w([-\w]*\w)?(\.\w([-\w]*\w)?)+", "");
                                stringHost = Regex.Replace(Regex.Replace(s2[1], @"(?i)^http://", ""), stringPath, "");
                            }
                            else
                            {
                                stringPath = s2[1];
                            }

                            stringVersion = s2[2];

                            if (stringMethod == "GET")
                            {
                                httpWebRequest = (HttpWebRequest)WebRequest.Create(s2[1]);
//                                webHeaderCollection = httpWebRequest.Headers;

                                int intLines = s1.Length - 1;
                                string stringBody = s1[intLines].Trim();

                                if (Regex.IsMatch(stringBody, @"(?i)^[a-z]+(-[a-z]+)*\s*:\s*\S+?$"))
                                {
                                    intLines = s1.Length;
                                    stringBody = string.Empty;
                                }

                                for (int i = 1; i < intLines; i++)
                                {
                                    s2 = Regex.Split(s1[i].Trim(), @":");

                                    string stringName = s2[0].Trim();
                                    string stringValue = s2[1].Trim();
                                    Console.WriteLine(stringName + " " + stringValue);

                                    if (Regex.IsMatch(stringName, @"(?i)^connection$"))
                                    {
                                        continue;
                                    }
                                    else if (Regex.IsMatch(stringName, @"(?i)^host$") && stringHost.Length == 0)
                                    {
                                        stringHost = Regex.Replace(s2[1], @":\d+$", "");
                                    }
                                    else
                                    {
//                                        webHeaderCollection.Add(s2[0], s2[1]);
                                    }
                                }

                                if (stringHost.Length > 0 && stringVersion.Length > 0)
                                {
                                    httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
                                    httpWebRequest.Connection = "Close";
                                    httpWebRequest.Method = "GET";
                                    httpWebRequest.ProtocolVersion = Regex.IsMatch(stringVersion, @"^HTTP/1.1$") ? HttpVersion.Version11 : HttpVersion.Version10;

                                    using (httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                                    {
                                        using (stream = httpWebResponse.GetResponseStream())
                                        {
                                            using (streamReader = new StreamReader(stream))
                                            {
                                                stringResponse = streamReader.ReadToEnd();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    stringResponse = stringBadRequest;
                                }
                            }
                            else
                            {
                                stringResponse = "Unable to send request. 501 Not Implemented - The " + stringMethod + " method is unsupported.";
                            }
                        }
                        else
                        {
                            stringResponse = stringBadRequest;
                        }

                        socket.Send(encoding.GetBytes(stringResponse));
                    }

                    Console.WriteLine(" Response sent.");
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