using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading;


namespace SocketChat
{
    class ChatClient
    {
        static string NickName = "";
        static TcpClient ClientSock = new TcpClient();
        static NetworkStream ServerStream;
        static void Main(string[] args)
        {
            Console.WriteLine("Started chat client");
            try
            {
                Console.WriteLine("Attempting to connect (127.0.0.1)...");
                ClientSock.Connect(IPAddress.Parse("127.0.0.1"), 2222);
                ServerStream = ClientSock.GetStream();
            }catch(Exception e)
            {
                Console.WriteLine("\nConnection to 127.0.0.1 failed: " + e.ToString());
                Console.WriteLine("\nPress any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("Connection established");
            Console.WriteLine("Getting nickname...");

            string msg;
            do
            {
                int Size = ClientSock.ReceiveBufferSize;
                byte[] bytes = new byte[1024];
                ServerStream.Read(bytes, 0, bytes.Length);
                msg = Encoding.ASCII.GetString(bytes);
            } while (!InitNickname(ref msg, ref NickName));

            Thread RefreshThread = new Thread(WaitForMessage);
            RefreshThread.Start();
            Console.WriteLine("Refresh thread started - searching for incoming messages in backgorund...");

            while (true)
            {
                Console.Write(">> ");
                string data = Console.ReadLine();
                if (data == "" || data == null) break;
                try
                {
                    byte[] bytes = new byte[1024];
                    bytes = Encoding.ASCII.GetBytes(NickName + ": " + data);
                    ClientSock.GetStream().Write(bytes, 0, bytes.Length);
                }catch(Exception e)
                {
                    Console.Write("Failed to send message: " + e.ToString());
                }
            }
        }

        static void WaitForMessage()
        {
            while (true)
            {
                int Size = ClientSock.ReceiveBufferSize;
                byte[] bytes = new byte[1024];
                ServerStream.Read(bytes, 0, bytes.Length);
                string msg = Encoding.ASCII.GetString(bytes);
                Console.WriteLine("\n>> " + msg);
            }
        }

        static bool InitNickname(ref string DataIn, ref string NickNameOut)
        {
            int length = DataIn.ToCharArray().Length;
            if (DataIn.Contains("%"))
            {
                List<char> temp = new List<char>();
                for (int i = DataIn.IndexOf("%") + 1; i < length; i++)
                {
                    temp.Add(DataIn[i]);
                }
                NickNameOut = new string(temp.ToArray());
                Console.WriteLine("Your nickname: " + NickNameOut);
                return true;
            }
            return false;
        }
    }
}
