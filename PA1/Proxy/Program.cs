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
        static byte[] bytes;
        static int intBytes, intPort;
        static string stringBody, stringHeader, stringHost, stringMethod, stringPath, stringRequest;

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
                        bytes = new byte[1024];
                        stringHeader = stringHost = stringRequest = string.Empty;

                        while (socket.Available > 0)
                        {
                            intBytes = socket.Receive(bytes);
                            stringRequest += Encoding.UTF8.GetString(bytes, 0, intBytes);
                        }

//                        Console.Write("Request received.");

                        match = Regex.Match(stringRequest, @"(?n)^\s*(?<Method>[A-Z]+)\s+(?<Path>\S+)\s+HTTP/1\.[01]\s*(?i)(?<Header>(\r\n\s*[a-z]+(-[a-z]+)*\s*:\s*\S(.*\S)?\s*)*)?((\r\n){2}(?<Body>\S(.*\S)?))?(\r\n){2}$");

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
                                                bytes = Encoding.UTF8.GetBytes(("whois -h hash.cymru.com " + BitConverter.ToString(sha1.ComputeHash(webClient.DownloadData("http://" + stringHost + stringPath))).Replace("-", "") + "\n").ToCharArray());
                                            }
                                        }

                                        try
                                        {
                                            using (tcpClient = new TcpClient())
                                            {
                                                tcpClient.Connect("hash.cymru.com", 43);

                                                using (stream = tcpClient.GetStream())
                                                {
                                                    stream.Write(bytes, 0, bytes.Length);

                                                    using (streamReader = new StreamReader(stream))
                                                    {
                                                        // fix me
                                                        if (Regex.IsMatch(streamReader.ReadToEnd(), @"(?i)^\w+\s+\d+\s+\d+"))
                                                        {
                                                            bytes = Encoding.UTF8.GetBytes(("<!doctype html>\n<html>\n<head>\n\t<meta charset=\"utf-8\">\n\t<title>Error - Malware Detected</title>\n</head>\n<body>\n\t<h1>Error - Malware Detected</h1><hr><br /><br />\n\n\tUnable to fulfill the request. The file appears to contain malicious content.\n</body>\n</html>").ToCharArray());
                                                        }
                                                        // fix me
                                                        else
                                                        {
                                                            try
                                                            {
                                                                bytes = Encoding.UTF8.GetBytes(("GET " + (stringPath.Length > 0 ? stringPath : "/") + " HTTP/1.0\r\nHost: " + stringHost + "\r\n" + stringHeader + "Connection: close\r\n\r\n" + stringBody).ToCharArray());

                                                                tcpClient = new TcpClient();
                                                                tcpClient.Connect(stringHost, 80);

                                                                stream = tcpClient.GetStream();
                                                                stream.Write(bytes, 0, bytes.Length);

                                                                try
                                                                {
                                                                    using (memoryStream = new MemoryStream())
                                                                    {
                                                                        while ((intBytes = stream.Read(bytes, 0, bytes.Length)) > 0)
                                                                        {
                                                                            memoryStream.Write(bytes, 0, intBytes);
                                                                        }

                                                                        bytes = memoryStream.ToArray();
                                                                    }
                                                                }
                                                                catch (Exception e)
                                                                {
                                                                    bytes = Encoding.UTF8.GetBytes(("Error: Unable to read the response. " + e.Message).ToCharArray());
                                                                }
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                bytes = Encoding.UTF8.GetBytes(("Error: Unable to send the request. " + e.Message).ToCharArray());
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Error: Unable to compute the hash. " + e.Message);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Error: Unable to download the data. " + e.Message);
                                    }
                                }
                                // otherwise send a bad request response
                                else
                                {
                                    bytes = Encoding.UTF8.GetBytes(("Unable to send the request. The host is required.").ToCharArray());
                                }
                            }
                            // otherwise send a not implemented response
                            else
                            {
                                bytes = Encoding.UTF8.GetBytes(("Unable to send the request. 501 Not Implemented - The " + stringMethod + " method is unsupported.").ToCharArray());
                            }
                        }
                        // otherwise send a bad request response
                        else
                        {
                            bytes = Encoding.UTF8.GetBytes(("Unable to send the request. 400 Bad Request - The request syntax is invalid.").ToCharArray());
                        }

                        socket.Send(bytes);

//                        Console.WriteLine(" Response sent.");
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