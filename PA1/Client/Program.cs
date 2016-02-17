﻿/*
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
using System.Text;
using System.Text.RegularExpressions;

namespace Client
{
    class Program
    {
        static byte[] bytes;
        static int intInput, intPort;
        static string stringInput, stringPort, stringRequest;

        static Match match;
        static Stream stream;
        static StreamReader streamReader;
        static TcpClient tcpClient;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting client on localhost. Enter two blank linkes successively to execute HTTP requests.\n");

            while (true)
            {
                // clear the console buffer
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }

                Console.Write("Enter HTTP request: ");

                intInput = 0;
                stringPort = stringRequest = string.Empty;

                while (true)
                {
                    // remove \r, \n, and whitespace from the input string
                    stringInput = Regex.Replace(Console.ReadLine().Trim(), @"\\r|\\n", "");

                    // if the input string is empty then check for error or break conditions
                    if (stringInput == string.Empty)
                    {
                        // if the input integer equals 1 and the request string is empty then display an error and reset the input integer and request string
                        if (intInput == 1 && Regex.IsMatch(stringRequest, @"^\s*$"))
                        {
                            Console.Write("Error: HTTP request is required. Please try again.\n\nEnter HTTP request: ");

                            intInput = 0;
                            stringRequest = string.Empty;
                        }
                        // otherwise if the input integer equals 2 then reset the console cursor position and break the loop
                        else if (intInput == 2)
                        {
                            Console.CursorTop--;

                            break;
                        }
                        // otherwise increment the input integer and append a carriage return and new line to the request string
                        else
                        {
                            intInput++;
                            stringRequest += "\r\n";
                        }
                    }
                    // otherwise set the input integer and append the input string, carriage return, and a new line to the request string
                    else
                    {
                        intInput = 1;
                        stringRequest += stringInput + "\r\n";
                    }
                }

                bytes = Encoding.UTF8.GetBytes(stringRequest.ToCharArray());
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
                                Console.WriteLine(streamReader.ReadToEnd() + "\n");
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