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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Proxy
{
    class Program
    {
        static byte[] bytes;
        static int intBytes, intPort;
        static string stringBody, stringHeader, stringHost, stringMethod, stringPath, stringRequest, stringResponse;

        static Encoding encoding;
        static Match match;
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

            try
            {
                tcpListener.Start();

                Console.WriteLine("\nProxy is listening for connections on port " + intPort + ".\n");

                while (true)
                {
                    bytes = new byte[1024];
                    stringRequest = string.Empty;

                    using (socket = tcpListener.AcceptSocket())
                    {
                        while (socket.Available > 0)
                        {
                            intBytes = socket.Receive(bytes);
                            stringRequest += encoding.GetString(bytes, 0, intBytes);
                        }

                        Console.Write("Request received.");

                        match = Regex.Match(stringRequest, @"(?n)^\s*(?<Method>[A-Z]+)\s+(?<Path>\S+)\s+HTTP/1\.[01]\s*(?i)(?<Header>(\r\n\s*[a-z]+(-[a-z]+)*\s*:\s*\S(.*\S)?\s*)*)?((\r\n){2}(?<Body>\S(.*\S)?))?(\r\n){2}$");
                        stringHeader = stringHost = stringResponse = string.Empty;

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
                                    if (Regex.IsMatch(m.Groups["Name"].Value, @"(?i)^connection$"))
                                    {
                                        continue;
                                    }
                                    // otherwise if the header is the host then set the host
                                    else if (Regex.IsMatch(m.Groups["Name"].Value, @"(?i)^host$") && stringHost.Length == 0)
                                    {

                                        stringHost = Regex.Replace(m.Groups["Value"].Value, @":\d+$", "");
                                    }
                                    // otherwise parse the header
                                    else
                                    {
                                        stringHeader += m.Groups["Name"].Value + ": " + m.Groups["Value"].Value + "\r\n";
                                    }
                                }

                                // if ...
                                if (stringHost.Length > 0)
                                {
                                    try
                                    {
                                        using (WebClient webClient = new WebClient())
                                        {
                                            using (SHA1Managed sha1Managed = new SHA1Managed())
                                            {
                                                bytes = sha1Managed.ComputeHash(webClient.DownloadData("http://" + stringHost + stringPath));
                                            }
                                        }

                                        bytes = encoding.GetBytes(("whois -h hash.cymru.com " + Regex.Replace(BitConverter.ToString(bytes), "-", "") + "\n").ToCharArray());

                                        try
                                        {
                                            using (TcpClient tcpClient = new TcpClient())
                                            {
                                                tcpClient.Connect("hash.cymru.com", 43);

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
                                        catch (Exception e)
                                        {
                                            Console.WriteLine("Error: Unable to check the hash. " + e.Message);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("Error: Unable to download the file. " + e.Message);
                                    }

                                    // bad
                                    if (Regex.IsMatch(stringResponse, @"(?i)^\w+\s+\d+\s+\d+"))
                                    {
                                        stringResponse = "<!doctype html>\n<html>\n<head>\n	<meta charset=\"utf-8\">\n	<title>Error - Malware Detected</title>\n</head>\n<body>\n	<h1>Error - Malware Detected</h1><hr><br /><br />\n	Unable to fulfill the request. The file may contain malicious content.\n</body>\n</html>";
                                    }
                                    // otherwise good
                                    else
                                    {
                                        stringRequest = "GET " + (stringPath.Length > 0 ? stringPath : "/") + " HTTP/1.0\r\nHost: " + stringHost + "\r\n" + stringHeader + "Connection: close\r\n\r\n" + stringBody;
                                        bytes = encoding.GetBytes(stringRequest.ToCharArray());

                                        try
                                        {
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
                                        catch (SocketException e)
                                        {
                                            stringResponse = "Error: Unable to send the request. " + e.Message;
                                        }
                                    }
                                }
                                // otherwise send a bad request response
                                else
                                {
                                    stringResponse = "Unable to send the request. The host is required.";
                                }
                            }
                            // otherwise send a not implemented response
                            else
                            {
                                stringResponse = "Unable to send the request. 501 Not Implemented - The " + stringMethod + " method is unsupported.";
                            }
                        }
                        // otherwise send a bad request response
                        else
                        {
                            stringResponse = "Unable to send the request. 400 Bad Request - The request syntax is invalid.";
                        }

                        socket.Send(encoding.GetBytes(stringResponse));

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