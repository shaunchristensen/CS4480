/*
*   Author:  Shaun Christensen
*   Course:  CS 4480 - Computer Networks
*   Created: 2016.01.31
*   Edited:  2016.02.02
*   Project: PA1
*   Summary: Build a web proxy capable of accepting HTTP requests, forwarding requests to remote (origin) servers, and returning response data to a client. The proxy must handle concurrent requests.
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Proxy
{
    class Program
    {
        static byte[] bytes;
        static int intBytes, intPort;
        static string stringBadRequest, stringHeader, stringHost, stringMethod, stringName, stringPath, stringRequest, stringResponse, stringValue, stringVersion;
        static string[] s1, s2;

        static Encoding encoding;
        static Socket socket;
        static Stream stream;
        static StreamReader streamReader;
        static TcpListener tcpListener;

        static void Main(string[] args)
        {
            Console.Write("Starting proxy on localhost. Enter desired port number: ");

            // if the port number cannot be parsed or is negative then set the port number to 80 by default
            if (!int.TryParse(Console.ReadLine(), out intPort) || intPort < 0)
            {
                intPort = 80;
            }

            encoding = Encoding.UTF8;
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), intPort);
            stringBadRequest = "Unable to send request. 400 Bad Request - The request syntax is invalid.";

            try
            {
                tcpListener.Start();

                Console.WriteLine("Proxy is listening for connections on port " + intPort + ".");

                while (true)
                {
                    bytes = new byte[1024];
                    stringHeader = stringHost = stringRequest = stringResponse = string.Empty;

                    using (socket = tcpListener.AcceptSocket())
                    {
                        while (socket.Available > 0)
                        {
                            intBytes = socket.Receive(bytes);
                            stringRequest = encoding.GetString(bytes, 0, intBytes);
                        }

                        Console.Write("Request received.");

                        // if the request syntax is valid then parse the request
                        if (Regex.IsMatch(stringRequest, @"^\s*[A-Z]+\s*\S+\s*HTTP/1\.[01]\s*(?i)(\r\n\s*[a-z]+(-[a-z]+)*\s*:\s*\S+\s*)*((\r\n){1,2}.*)?\s*$"))
                        {
                            s1 = Regex.Split(stringRequest.Trim(), Environment.NewLine);
                            s2 = Regex.Split(s1[0].Trim(), @"\s+");

                            stringMethod = s2[0];

                            // if the request contains an absolute URL followed then extract the host and path
                            if (Regex.IsMatch(s2[1], @"(?i)^http://"))
                            {
                                stringPath = Regex.Replace(s2[1], @"(?i)^http://\w([-\w]*\w)?(\.\w([-\w]*\w)?)+", "");
                                stringHost = Regex.Replace(Regex.Replace(s2[1], @"(?i)^http://", ""), stringPath, "");
                            }
                            // otherwise set the path
                            else
                            {
                                stringPath = s2[1];
                            }

                            stringPath = stringPath.Length > 0 ? stringPath : "/";
                            stringVersion = s2[2];

                            // if the request specifies the get method then parse the request
                            if (stringMethod == "GET")
                            {
                                int intLines = s1.Length - 1;
                                string stringBody = s1[intLines].Trim();

                                // if the last line is a header then process the header
                                if (Regex.IsMatch(stringBody, @"(?i)^[a-z]+(-[a-z]+)*\s*:\s*\S+?$"))
                                {
                                    intLines = s1.Length;
                                    stringBody = string.Empty;
                                }

                                for (int i = 1; i < intLines; i++)
                                {
                                    // if the line is blank then skip the line
                                    if (s1[i].Length == 0)
                                    {
                                        continue;
                                    }

                                    s2 = Regex.Split(s1[i].Trim(), @":");

                                    stringName = s2[0].Trim();
                                    stringValue = s2[1].Trim();

                                    // if the header is the connection then skip the header
                                    if (Regex.IsMatch(stringName, @"(?i)^connection$"))
                                    {
                                        continue;
                                    }
                                    // otherwise if the header is the host then set the host
                                    else if (Regex.IsMatch(stringName, @"(?i)^host$") && stringHost.Length == 0)
                                    {
                                        stringHost = stringValue;
                                    }
                                    // otherwise parse the header
                                    else
                                    {
                                        stringHeader += stringName + ": " + stringValue + "\r\n";
                                    }
                                }

                                // if the host, path, and version are all not blank then send the request
                                if (stringHost.Length > 0 && stringPath.Length > 0 && stringVersion.Length > 0)
                                {
                                    stringRequest = stringMethod + " " + stringPath + " " + stringVersion + "\r\nHost: " + stringHost + "\r\nConnection: close\r\n" + stringHeader + "\r\n" + stringBody;
                                    bytes = encoding.GetBytes(stringRequest.ToCharArray());

                                    using (TcpClient tcpClient = new TcpClient())
                                    {
                                        tcpClient.Connect(stringHost, 80);

                                        using (stream = tcpClient.GetStream())
                                        {
                                            stream.Write(bytes, 0, bytes.Length);

                                            using (streamReader = new StreamReader(stream))
                                            {
                                                stringResponse = streamReader.ReadToEnd();
                                            }
                                        }
                                    }
                                }
                                // otherwise send a bad request response
                                else
                                {
                                    stringResponse = stringBadRequest;
                                }
                            }
                            // otherwise send a not implemented response
                            else
                            {
                                stringResponse = "Unable to send request. 501 Not Implemented - The " + stringMethod + " method is unsupported.";
                            }
                        }
                        // otherwise send a bad request response
                        else
                        {
                            stringResponse = stringBadRequest;
                        }

                        socket.Send(encoding.GetBytes(stringResponse));
                    }

                    Console.WriteLine(" Response sent.");
                }
            }
            catch (FormatException e)
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