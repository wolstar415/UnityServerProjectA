using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using MySqlX.XDevAPI;

namespace GameServer.Scripts
{
    public class TcpServer
    {
        public static TcpServer Instance;
        private TcpListener server;
        private bool isRunning;
        private List<TcpClient> clients;

        public static void Init(string ipAddress, int port)
        {
            Instance = new TcpServer();

            Instance.server = new TcpListener(IPAddress.Parse(ipAddress), port);
            Instance.clients = new List<TcpClient>();

            Instance.Start();
        }

        public void Start()
        {
            server.Start();
            isRunning = true;
            Form.Inst.AddLog("Server started on " + server.LocalEndpoint);

            while (isRunning)
            {
                TcpClient client = server.AcceptTcpClient();
                clients.Add(client);
                Form.Inst.AddLog("Client connected: " + client.Client.RemoteEndPoint);

                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Form.Inst.AddLog("Received: " + message);
                BroadcastMessage(message, client);
            }

            clients.Remove(client);
            client.Close();
            Form.Inst.AddLog("Client disconnected.");
        }

        private void BroadcastMessage(string message, TcpClient sender)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);

            foreach (var client in clients)
            {
                if (client != sender)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            server.Stop();
            Form.Inst.AddLog("Server stopped.");
        }
    }
    
}



