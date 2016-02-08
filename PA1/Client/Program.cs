
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

namespace Client
{
    class Program
    {
        static byte[] bytes;
        static int intCursorTop, intPort;
        static string stringInput, stringPort, stringRequest, stringResponse;

        static Encoding encoding;
        static Match match;
        static Stream stream;
        static StreamReader streamReader;
        static TcpClient tcpClient;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting client on localhost. Enter two blank linkes successively to execute HTTP requests.\n");

            encoding = Encoding.UTF8;

            while (true)
            {
                // clear the console buffer
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }

                Console.Write("Enter HTTP request: ");

                intCursorTop = -1;
                stringPort = stringRequest = stringResponse = string.Empty;
                stringInput = Console.ReadLine();
                stringRequest += stringInput + "\r\n";

                while (true)
                {
                    // if the input string ends with two empty lines then break the loop
                    if ((intCursorTop == 0 && Regex.IsMatch(stringRequest, @"^\s*$")) || intCursorTop > 0)
                    {
                        break;
                    }

                    stringInput = Console.ReadLine();
                    stringRequest += stringInput + "\r\n";

                    // if the input string is empty then increment the cursor top integer
                    if (Regex.IsMatch(stringInput, @"^\s*$"))
                    {
                        intCursorTop++;
                    }
                    else
                    {
                        intCursorTop = -1;
                    }
                }

                // reset the console cursor
                Console.CursorTop -= intCursorTop;

                bytes = encoding.GetBytes(stringRequest.ToCharArray());
                match = Regex.Match(stringRequest, @"(?in)(host\s*:\s*|http://)\w([-\w]*\w)?(\.\w([-\w]*\w)?)+:(?<Port>\d+)");
                
                // if the request contains an absolute URL or a host header followed by a port number then extract and remove the port number
                if (match.Success)
                {
                    stringPort = match.Groups["Port"].Value;
                }

                // if the port number cannot be parsed or is negative then set the port number to 80 by default
                if (!int.TryParse(stringPort, out intPort) || intPort < 0)
                {
                    intPort = 80;
                }

                try
                {
                    using (tcpClient = new TcpClient())
                    {
                        tcpClient.Connect(IPAddress.Parse("127.0.0.1"), intPort);

                        using (stream = tcpClient.GetStream())
                        {
                            stream.Write(bytes, 0, bytes.Length);

                            using (streamReader = new StreamReader(stream))
                            {
                                stringResponse = streamReader.ReadToEnd();

                                Console.WriteLine(stringResponse + "\n");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Unable to connect to proxy. " + e.Message + ".\n");
                }
            }
        }
    }
}