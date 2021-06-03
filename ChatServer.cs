using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net.NetworkInformation;

namespace Server
{
    class ChatServer
    {
        static Dictionary<string, TcpClient> Clients = new Dictionary<string, TcpClient>();
        static TcpListener ServerSock = new TcpListener(IPAddress.Parse("127.0.0.1"), 2222);
        static ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();
        static void Main(string[] args)
        {
            ServerSock.Start();
            Console.WriteLine("Chat server stared");

            Thread ConnectionThread = new Thread(HandleConnections);
            ConnectionThread.Start();
            
            Thread RefreshThread = new Thread(HandleIncomingMessages);
            RefreshThread.Start();
        }

        static private void Multicast(string ClientName, ref string message, ref List<TcpClient> Clients)
        {
            int i = 0;
            foreach (TcpClient Client in Clients)
            {
                i++;
                if (message != null && message != "")
                {
                    byte[] data = Encoding.ASCII.GetBytes(message);
                    NetworkStream stream = Client.GetStream();
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                }
            }
            Console.WriteLine("Multicasting to {0} clients",  i);
        }

        static void HandleIncomingMessages()
        {
            Console.WriteLine("Refresh thread started - searching for incoming messages in backgorund...");
            List<TcpClient> CachedClients = new List<TcpClient>();
            List<string> CachedClientNames = new List<string>();
            while (true)
            {
                rwl.EnterReadLock();
                try
                {
                    foreach (KeyValuePair<string, TcpClient> Client in Clients)
                    {
                        if (!CachedClients.Contains(Client.Value))
                        {
                            CachedClients.Add(Client.Value);
                            CachedClientNames.Add(Client.Key);
                            Console.WriteLine("\nCached new client: " + Client.Key);
                        }
                    }
                }
                finally
                {
                    rwl.ExitReadLock();
                    int i = 0;
                    foreach (TcpClient client in CachedClients)
                    {
                        byte[] bytes = new byte[1024];
                        NetworkStream stream = client.GetStream();
                        int bytesCount = 0;
                        while (stream.DataAvailable)
                        {
                            bytesCount = stream.Read(bytes, 0, bytes.Length);
                        }
                        string data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytesCount);
                        if (data != null && data != "")
                        {
                            Multicast(CachedClientNames[i], ref data, ref CachedClients);

                            Console.WriteLine("Received " + data + " from " + CachedClientNames[i] + ".");
                            i++;
                        }
                    }
                }
            }
        }

        static void HandleConnections()
        {
            int ClientIndex = 0;
            while (true)
            {
                if (ServerSock.Pending())
                {
                    byte[] bytes = new byte[1024];
                    TcpClient ClientSock = ServerSock.AcceptTcpClient();
                    Console.WriteLine("\nEstablishing new connection...");

                    ClientIndex++;
                    rwl.EnterWriteLock();
                    try
                    {
                        Console.WriteLine("Locked Clients");
                        Clients.Add("Member " + ClientIndex, ClientSock);
                        Console.WriteLine("Connection establieshed with " + "Member " + ClientIndex);
                    }
                    finally
                    {
                        rwl.ExitWriteLock();
                        Console.WriteLine("Lock released");
                    }
                    bytes = Encoding.ASCII.GetBytes("Your name in chat is: %" + "Member " + ClientIndex);
                    ClientSock.GetStream().Write(bytes, 0, bytes.Length);
                    ClientSock.GetStream().Flush();
                }
                Thread.Sleep(500);
            }
        }
    }
}
