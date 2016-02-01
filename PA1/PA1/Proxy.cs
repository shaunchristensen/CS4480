/*
*   Author:  Shaun Christensen & Xaiver Humberg
*   Course:  CS 4480 - Computer Networks
*   Created: 2015.11.??
*   Edited:  2015.12.12
*   Project: Problem Set Nine
*   Summary: The idea is to build a game similar to that seen on agar.io.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PA1
{
    class Proxy
    {
        public static void Main(string[] args)
        {
            // start proxy

            //            NetworkController.Server_awaiting_client_loop(newConnection, 11000);
            //            NetworkController.Server_awaiting_client_loop(webConnection, 11100);
        }
    }
}
/*
        private static void webConnection(NetworkController.StateObject state)
        {
            NetworkController.I_want_more_data(state.workSocket, receivedWebQuery);
        }

        private static void receivedWebQuery(NetworkController.StateObject state)
        {
            NetworkController.Send(state.workSocket,

                "HTTP/1.1 200 OK\r\n" +
                "Connection: close\r\n" +
                "Content-Type: text/html; charset=UTF-8\r\n"

                );
            Thread.Sleep(10);
            NetworkController.Send(state.workSocket, "\r\n");
            Thread.Sleep(10);

            NetworkController.Send(state.workSocket, db.ExportData(state.returnVal.Split('\n')[0]), closeSocket); // If I close the socket here, chrome won't display it.......

        }

        private static void closeSocket(NetworkController.StateObject state)
        {
            Console.WriteLine("Successfully sent the HTML to the browser. Closing the socket");
            state.workSocket.Close();
        }

        private static void newConnection(NetworkController.StateObject state)
        {
            Socket newPlayerSocket = state.workSocket;
        }

        private static void newPlayerReceived(NetworkController.StateObject state)
        {
            Socket newPlayerSocket = state.workSocket;
        }
}
*/