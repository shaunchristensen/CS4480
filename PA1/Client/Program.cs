using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] bytes;
            int intBytes, intPort;
            string s1, s2, stringRequest, stringResponse;

            Encoding encoding = Encoding.UTF8;
            Stream stream;

            stringRequest = stringResponse = string.Empty;

            Console.WriteLine("Starting client on localhost. Enter two blank linkes successively to execute HTTP requests.\n");

            while (true)
            {
                Console.Write("Enter HTTP request: ");

                s1 = Console.ReadLine();

                if (Regex.IsMatch(s1, "(?i)exit"))
                {
                    return;
                }

                stringRequest += s1 + "\r\n";

                do
                {
                    s2 = s1;
                    s1 = Console.ReadLine();
                    stringRequest += s1 + "\r\n";
                } while (s1.Length > 0 || s2.Length > 0);

                if (Regex.IsMatch(stringRequest, @"(?i)(http://|host:\s*)\w:(\d+)"))
                {
                    // capture?
                    intPort = 80;
                }
                else
                {
                    intPort = 80;
                }

                using (TcpClient tcpClient = new TcpClient())
                {
                    tcpClient.Connect("127.0.0.1", intPort);
                    stream = tcpClient.GetStream();

                    bytes = encoding.GetBytes(stringRequest.ToCharArray());
                    stream.Write(bytes, 0, bytes.Length);

                    bytes = new byte[1024];
                    intBytes = stream.Read(bytes, 0, 1024);

                    for (int i = 0; i < intBytes; i++)
                    {
                        stringResponse += Convert.ToChar(bytes[i]);
                    }

                    Console.WriteLine(stringResponse + "\n");

                    tcpClient.Close();
                }
            }
        }
    }
}