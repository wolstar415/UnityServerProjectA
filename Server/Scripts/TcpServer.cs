using MySqlX.XDevAPI;
using Org.BouncyCastle.Bcpg;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace GameServer.Scripts
{

    public class TcpServer
    {
        public static TcpServer? Instance;
        private TcpListener? server;
        private bool isRunning;
        //private Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        private Dictionary<IPEndPoint, TcpClient> clientMap = new Dictionary<IPEndPoint, TcpClient>();
        private const int IntSize = sizeof(int);

        public static void Init(string ipAddress, int port)
        {
            Instance = new TcpServer();
            Instance.server = new TcpListener(IPAddress.Parse(ipAddress), port);
            Instance.server.Start();
            Instance.isRunning = true;

            Thread serverThread = new Thread(Instance.ServerLoop);
            serverThread.Start();

            Form.Inst.AddLog("Server started on " + ipAddress + ":" + port);
        }

        private void ServerLoop()
        {
            while (isRunning)
            {
                TcpClient client = server.AcceptTcpClient();
                IPEndPoint? clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (clientEndPoint != null)
                {
                    clientMap.Add(clientEndPoint, client);
                    Form.Inst.AddLog("Client connected: " + clientEndPoint);

                }
                Thread clientThread = new Thread(() => HandleClientAsync(client).Wait());
                clientThread.Start();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            try
            {
                while (true)
                {
                    byte[] lengthBuffer = new byte[IntSize];
                    int bytesRead = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
                    if (bytesRead != IntSize)
                    {
                        throw new Exception("Failed to read data length.");
                    }

                    int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] dataBuffer = new byte[dataLength];
                    bytesRead = 0;
                    while (bytesRead < dataLength)
                    {
                        bytesRead += await stream.ReadAsync(dataBuffer, bytesRead, dataLength - bytesRead);
                    }

                    string json = Encoding.UTF8.GetString(dataBuffer);
                    //Packet packet = PacketSerializer.Deserialize(json);
                    //HandlePacket(packet, client);

                    BroadcastMessage(json, client);

                    Form.Inst.AddLog("Message : " + json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                DisconnectClient(client);
                client.Close();
                Form.Inst.AddLog("Client disconnected.");
            }
        }

        public void BroadcastMessage(string message, TcpClient sender)
        {
            IPEndPoint? senderEndpoint = sender.Client.RemoteEndPoint as IPEndPoint;

            List<Task> sendTasks = new List<Task>();


            foreach (var item in clientMap)
            {
                if (item.Key == senderEndpoint)
                    continue;

                try
                {
                    NetworkStream stream = item.Value.GetStream();
                    string json = message;
                    byte[] data = Encoding.UTF8.GetBytes(json);

                    byte[] dataLength = BitConverter.GetBytes(data.Length);
                    stream.Write(dataLength, 0, dataLength.Length);
                    stream.Write(data, 0, data.Length);
                    Form.Inst.AddLog($"Send : Client : {item.Key}  message:{message}");

                }
                catch (Exception ex)
                {
                    Form.Inst.AddLog($"Failed to send data to client: {ex.Message}");
                }
            }

        }

        //private void HandlePacket(Packet packet, TcpClient sender)
        //{
        //    switch (packet.Type)
        //    {
        //        case PacketType.Text:
        //            string roomName = ExtractRoomNameFromPacket(packet);
        //            HandleChatMessage(roomName, packet.Data, sender);
        //            break;

        //        case PacketType.Login:
        //            HandleLogin(packet.Data);
        //            break;

        //        case PacketType.JoinRoom:
        //            string joinRoomName = packet.Data;
        //            JoinRoom(joinRoomName, sender);
        //            break;

        //        case PacketType.LeaveRoom:
        //            string leaveRoomName = packet.Data;
        //            LeaveRoom(leaveRoomName, sender);
        //            break;

        //        default:
        //            Console.WriteLine("Unknown packet type: " + packet.Type);
        //            break;
        //    }
        //}

        //private void HandleChatMessage(string roomName, string message, TcpClient sender)
        //{
        //    if (rooms.ContainsKey(roomName))
        //    {
        //        rooms[roomName].BroadcastMessage(message, sender, clientMap);
        //    }
        //    else
        //    {
        //        Console.WriteLine("Room not found: " + roomName);
        //    }
        //}

        //private void JoinRoom(string roomName, TcpClient client)
        //{
        //    IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
        //    if (endpoint != null)
        //    {
        //        if (!rooms.ContainsKey(roomName))
        //        {
        //            rooms[roomName] = new Room(roomName);
        //        }

        //        rooms[roomName].AddClient(client);
        //        clientMap[endpoint] = client;
        //        Console.WriteLine($"Client joined room: {roomName}");
        //    }
        //}

        //private void LeaveRoom(string roomName, TcpClient client)
        //{
        //    IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
        //    if (endpoint != null)
        //    {
        //        if (rooms.ContainsKey(roomName))
        //        {
        //            rooms[roomName].RemoveClient(client);
        //            clientMap.Remove(endpoint);
        //            Console.WriteLine($"Client left room: {roomName}");
        //        }
        //    }
        //}

        private void DisconnectClient(TcpClient client)
        {
            //foreach (var room in rooms.Values)
            //{
            //    room.RemoveClient(client);
            //}

            IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint != null)
            {
                clientMap.Remove(endpoint);
            }
        }

        //private string ExtractRoomNameFromPacket(Packet packet)
        //{
        //    string[] parts = packet.Data.Split(new[] { ':' }, 2);
        //    return parts.Length > 1 ? parts[0] : "DefaultRoom";
        //}

        public void Stop()
        {
            isRunning = false;
            if (server != null) server.Stop();
            Console.WriteLine("Server stopped.");
        }
    }

    //public class Room
    //{
    //    public string RoomName { get; private set; }
    //    private HashSet<IPEndPoint> clientEndpoints;

    //    public Room(string roomName)
    //    {
    //        RoomName = roomName;
    //        clientEndpoints = new HashSet<IPEndPoint>();
    //    }

    //    public void AddClient(TcpClient client)
    //    {
    //        IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
    //        if (endpoint != null)
    //        {
    //            clientEndpoints.Add(endpoint);
    //        }
    //    }

    //    public void RemoveClient(TcpClient client)
    //    {
    //        IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
    //        if (endpoint != null)
    //        {
    //            clientEndpoints.Remove(endpoint);
    //        }
    //    }

    //    public void BroadcastMessage(string message, TcpClient sender, Dictionary<IPEndPoint, TcpClient> clientMap)
    //    {
    //        IPEndPoint senderEndpoint = sender.Client.RemoteEndPoint as IPEndPoint;

    //        List<Task> sendTasks = new List<Task>();

    //        foreach (var endpoint in clientEndpoints)
    //        {
    //            if (!endpoint.Equals(senderEndpoint) && clientMap.TryGetValue(endpoint, out TcpClient client))
    //            {
    //                sendTasks.Add(Task.Run(() => SendDataToClient(client, new Packet { Type = PacketType.Text, Data = message })));
    //            }
    //        }

    //        Task.WhenAll(sendTasks).Wait();
    //    }

    //    private void SendDataToClient(TcpClient client, Packet packet)
    //    {
    //        try
    //        {
    //            NetworkStream stream = client.GetStream();
    //            string json = PacketSerializer.Serialize(packet);
    //            byte[] data = Encoding.UTF8.GetBytes(json);

    //            byte[] dataLength = BitConverter.GetBytes(data.Length);
    //            stream.Write(dataLength, 0, dataLength.Length);
    //            stream.Write(data, 0, data.Length);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Failed to send data to client: {ex.Message}");
    //        }
    //    }
    //}

}



