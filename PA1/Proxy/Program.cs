/*
*   Author:  Shaun Christensen
*   Course:  CS 4480 - Computer Networks
*   Created: 2016.01.31
*   Edited:  2016.02.21
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
using System.Threading.Tasks;

namespace Proxy
{
    class Program
    {
            static void Main(string[] args)
        {
            StartProxy().Wait();
        }

        private static async Task StartProxy()
        {
            int intPort;

            Console.Write("Starting the proxy on localhost. Enter the desired port number: ");

            // if the port number cannot be parsed or the port number is less than 0 then set the default port number
            if (!int.TryParse(Console.ReadLine(), out intPort) || intPort < 0)
            {
                Console.WriteLine("\r\nError: Invalid port number. The port number must be an integer greater than or equal to zero.");

                intPort = 80;
            }

            TcpListener tcpListener = new TcpListener(IPAddress.Loopback, intPort);

            try
            {
                tcpListener.Start();

                Console.WriteLine("\r\nThe proxy is listening for connections on port " + intPort + ".\r\n");

                // asynchronously wait for new connections
                while (true)
                {
                    await Task.Run(async () => ServeRequest(await tcpListener.AcceptTcpClientAsync()));
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

        private static async void ServeRequest(TcpClient t)
        {
            // force the method to execute asynchronously
            await Task.Yield();

            byte[] b;
            int i;
            string s, stringBody, stringHeader, stringHost, stringMethod, stringPath;

            Match match;
            MemoryStream memoryStream;
            SHA1 sha1;
            Stream stream;
            TcpClient tcpClient;
            WebClient webClient;

            b = new byte[1024];
            s = string.Empty;

            // asynchronously read the request
            while (t.Client.Available > 0)
            {
                s += Encoding.UTF8.GetString(b, 0, await Task<int>.Run(() => { return t.Client.EndReceive(t.Client.BeginReceive(b, 0, b.Length, SocketFlags.None, null, null)); }));
            }

            // if the request is not empty then parse the request
            if (s.Length > 0)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + "] Request received from socket " + t.Client.Handle + ":\r\n\r\n" + s.Trim() + "\r\n");

                match = Regex.Match(s, @"(?n)^\s*(?<Method>[A-Z]+)\s+(?<Path>\S+)\s+HTTP/1\.[01]\s*(?i)(?<Header>(\r\n\s*[a-z]+(-[a-z]+)*\s*:\s*\S(.*\S)?\s*)*)?((\r\n){2}(?<Body>\S(.*\S)?))?(\r\n){2}$");

                // if the request syntax is valid then check the request method
                if (match.Success)
                {
                    stringMethod = match.Groups["Method"].Value;

                    // if the request uses the get method then prepare the request
                    if (stringMethod == "GET")
                    {
                        stringHeader = stringHost = string.Empty;
                        stringBody = match.Groups["Body"].Value;

                        // if the request contains an absolute URL then set the host and path
                        if (Regex.IsMatch(match.Groups["Path"].Value, @"(?i)^http://"))
                        {
                            stringHost = Regex.Match(match.Groups["Path"].Value, @"(?in)^http://(\S+:\S*@)?(?<Host>\w([-\w]*\w)?(\.\w([-\w]*\w)?)*)(:\d+)?").Groups["Host"].Value;
                            stringPath = Regex.Match(match.Groups["Path"].Value, stringHost + @"(?n)(:\d+)?(?<Path>/.*)$").Groups["Path"].Value;
                        }
                        // otherwise set the path
                        else
                        {
                            stringPath = match.Groups["Path"].Value;
                        }

                        // process the header
                        foreach (Match m in Regex.Matches(Regex.Replace(match.Groups["Header"].Value.Trim(), @"\r", ""), @"(?imn)^\s*(?<Name>[a-z]+(-[a-z]+)*)\s*:\s*(?<Value>\S(.*\S)?)\s*$"))
                        {
                            // if the header is not the connection or the host then set the header
                            if (!Regex.IsMatch(m.Groups["Name"].Value, @"(?in)^(connection|host)$"))
                            {
                                stringHeader += m.Groups["Name"].Value + ": " + m.Groups["Value"].Value + "\r\n";
                            }
                            // otherwise if the header is the host and the host is empty then set the host
                            else if (Regex.IsMatch(m.Groups["Name"].Value, @"(?i)^host$") && stringHost.Length == 0)
                            {
                                stringHost = Regex.Replace(m.Groups["Value"].Value, @":\d+$", "");
                            }
                            // otherwise skip the header
                            else
                            {
                                continue;
                            }
                        }

                        // if the host is not empty then send the request
                        if (stringHost.Length > 0)
                        {
                            try
                            {
                                using (sha1 = new SHA1Managed())
                                {
                                    using (webClient = new WebClient())
                                    {
                                        b = Encoding.UTF8.GetBytes(("whois -h hash.cymru.com " + BitConverter.ToString(sha1.ComputeHash(await webClient.DownloadDataTaskAsync("http://" + stringHost + stringPath))).Replace("-", "") + "\r\n").ToCharArray());
                                    }
                                }

                                using (tcpClient = new TcpClient())
                                {
                                    tcpClient.Connect("hash.cymru.com", 43);

                                    using (stream = tcpClient.GetStream())
                                    {
                                        await stream.WriteAsync(b, 0, b.Length);

                                        using (memoryStream = new MemoryStream())
                                        {
                                            b = new byte[1024];

                                            // asynchronously read the response
                                            while ((i = await stream.ReadAsync(b, 0, b.Length)) > 0)
                                            {
                                                await memoryStream.WriteAsync(b, 0, i);
                                            }

                                            s = Encoding.UTF8.GetString(memoryStream.ToArray());
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

                            // if any malware is detected then send an infected file response
                            if (Regex.IsMatch(s, @"(?i)^\w+\s+\d+\s+\d+"))
                            {
                                b = Encoding.UTF8.GetBytes(("<!doctype html>\r\n<html>\r\n<head>\r\n\t<meta charset=\"utf-8\">\r\n\t<title>Error - Malware Detected</title>\r\n</head>\r\n<body>Error: Unable to serve the request. Malware Detected - The file appears to be infected.</body>\r\n</html>").ToCharArray());
                            }
                            // otherwise send the request
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
                                            await stream.WriteAsync(b, 0, b.Length);

                                            using (memoryStream = new MemoryStream())
                                            {
                                                b = new byte[1024];

                                                // asynchronously read the response
                                                while ((i = await stream.ReadAsync(b, 0, b.Length)) > 0)
                                                {
                                                    await memoryStream.WriteAsync(b, 0, i);
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
                        // otherwise send a host is required response
                        else
                        {
                            b = Encoding.UTF8.GetBytes(("Error: Unable to send the request. The host is required.").ToCharArray());
                        }
                    }
                    // otherwise send an unsupported method response
                    else
                    {
                        b = Encoding.UTF8.GetBytes(("Error: Unable to send the request. 501 Not Implemented - The " + stringMethod + " method is unsupported.").ToCharArray());
                    }
                }
                // otherwise send an invalid syntax response
                else
                {
                    b = Encoding.UTF8.GetBytes(("Error: Unable to send the request. 400 Bad Request - The request syntax is invalid.").ToCharArray());
                }

                Console.WriteLine("[" + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + "] Response sent to socket " + t.Client.Handle + ":\r\n\r\n" + Encoding.UTF8.GetString(b, 0, b.Length).Trim() + "\r\n");

                //
                using (stream = t.GetStream())
                {
                    await stream.WriteAsync(b, 0, b.Length);
                }
            }

            t.Close();
        }
    }
}