
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MySqlX.XDevAPI;
using System.Numerics;
using System.Windows.Forms;

namespace GameServer.Scripts
{
    public enum PacketType
    {
        Login,
        JoinRoom,
        LeaveRoom,
        Text
    }
    public class Packet
    {
        public PacketType Type { get; set; }
        public string Data { get; set; }
    }

    public class TcpServer
    {
        public static TcpServer? Instance;
        private TcpListener? server;
        private bool isRunning;
        private Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        private Dictionary<IPEndPoint, TcpClient> clientMap = new Dictionary<IPEndPoint, TcpClient>();
        private Dictionary<IPEndPoint, string> clientNames = new ();
        private const int IntSize = sizeof(int);
        public string ipAddress;
        public int port;
        private static readonly object lockObj = new object();
        private List<IPEndPoint> waitingPlayers = new List<IPEndPoint>();
        public void AddPlayerToMatchmaking(IPEndPoint point)
        {
            lock (lockObj)
            {
                waitingPlayers.Add(point);
            }
        }
        public void StartMatchmaking()
        {
            while (true)
            {
                Thread.Sleep(1000); // 매치메이킹 주기를 1초마다 수행
                lock (lockObj)
                {
                    var matchedPlayers = new List<(IPEndPoint, IPEndPoint)>();
                    foreach (var player in waitingPlayers.ToList()) // 대기 중인 플레이어 목록 복사
                    {
                        IPEndPoint? opponent = FindBestMatch(player);

                        if (opponent!=null)
                        {
                            matchedPlayers.Add((player, opponent));
                            waitingPlayers.Remove(player);
                            waitingPlayers.Remove(opponent);
                        }
          
                    }

                    // 매칭된 플레이어들을 처리
                    foreach (var match in matchedPlayers)
                    {
                        var dd = clientMap[match.Item1];
                        var dd_name = clientNames[match.Item1];
                        var dd2 = clientMap[match.Item2];
                        var dd2_name = clientNames[match.Item2];

                        JoinRoom($"{dd_name}_{dd2_name}", dd);
                        JoinRoom($"{dd_name}_{dd2_name}", dd2);

                        SendDataToClient(dd, new Packet { Type = PacketType.JoinRoom, Data = $"{dd_name}_{dd2_name}" });
                        SendDataToClient(dd2, new Packet { Type = PacketType.JoinRoom, Data = $"{dd_name}_{dd2_name}" });

                        Form.Inst.AddLog($"Matching  : {dd_name} / {dd2_name}");
                    }
                }
            }
        }

        private IPEndPoint? FindBestMatch(IPEndPoint player)
        {
            //int scoreRange = 50; // 초기 매칭 범위
            //int maxScoreDifference = 300; // 최대 허용 매칭 범위
            //TimeSpan maxWaitTime = TimeSpan.FromSeconds(30); // 대기 시간이 30초가 넘으면 매칭 범위 확장

            var potentialMatches = waitingPlayers
                    .Where(p => p != player)
                    .ToList();

            return potentialMatches.FirstOrDefault();
        }

        //private static void CreateMatch(Player player1, Player player2)
        //{
        //    Console.WriteLine($"Match created between {player1.PlayerId} (Score: {player1.Score}) and {player2.PlayerId} (Score: {player2.Score})");
        //    // 매치 생성 후, 게임 시작 등의 로직을 추가할 수 있습니다.
        //}
        public static void Init(string ipAddress, int port)
        {
            Instance = new TcpServer();
            Instance.server = new TcpListener(IPAddress.Parse(ipAddress), port);
            Instance.isRunning = false;
            Instance.ipAddress = ipAddress;
            Instance.port = port;
            Instance.Start();

            Task.Run(() => Instance.StartMatchmaking()); // 비동기로 매치메이킹 시작

        }

        private void ServerLoop()
        {
            try
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
            catch (Exception)
            {

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
                    Packet packet = PacketSerializer.Deserialize(json);
                    HandlePacket(packet, client);

                    //BroadcastMessage(json, client);

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
        private void HandleLogin(string data, TcpClient sender)
        {
            Database.AccountUpdate(data, "Login");

            var e = sender.Client.RemoteEndPoint as IPEndPoint;
            AddPlayerToMatchmaking(e);
            // 예시로, 클라이언트의 이름을 로그인 데이터로 설정
            clientNames[e] = data;
            Console.WriteLine($"Client logged Id: {data}");
            //SendMessage(clientSocket, "Login successful");
        }
        private void HandlePacket(Packet packet, TcpClient sender)
        {
            switch (packet.Type)
            {
                case PacketType.Text:
                    string roomName = ExtractRoomNameFromPacket(packet);
                    HandleChatMessage(roomName, packet.Data, sender);
                    break;

                case PacketType.Login:
                    HandleLogin(packet.Data, sender);
                    break;

                case PacketType.JoinRoom:
                    string joinRoomName = packet.Data;
                    JoinRoom(joinRoomName, sender);
                    break;

                case PacketType.LeaveRoom:
                    string leaveRoomName = packet.Data;
                    LeaveRoom(leaveRoomName, sender);
                    break;

                default:
                    Console.WriteLine("Unknown packet type: " + packet.Type);
                    break;
            }
        }

        private string ExtractRoomNameFromPacket(Packet packet)
        {
            string[] parts = packet.Data.Split(new[] { ':' }, 2);
            return parts.Length > 1 ? parts[0] : "DefaultRoom";
        }

        private void HandleChatMessage(string roomName, string message, TcpClient sender)
        {
            if (rooms.ContainsKey(roomName))
            {
                rooms[roomName].BroadcastMessage(message, sender, clientMap);
            }
            else
            {
                Console.WriteLine("Room not found: " + roomName);
            }
        }

        private void JoinRoom(string roomName, TcpClient client)
        {
            IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint != null)
            {
                if (!rooms.ContainsKey(roomName))
                {
                    rooms[roomName] = new Room(roomName);
                }

                rooms[roomName].AddClient(client);
                clientMap[endpoint] = client;
                Form.Inst.AddLog($"Client joined room: {roomName}//{endpoint}");
            }
        }

        private void LeaveRoom(string roomName, TcpClient client)
        {
            IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint != null)
            {
                if (rooms.ContainsKey(roomName))
                {
                    rooms[roomName].RemoveClient(client);
                    clientMap.Remove(endpoint);
                    Console.WriteLine($"Client left room: {roomName}");
                }
            }
        }

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

        public void Start()
        {
            if (isRunning)
                return;
            isRunning = true;
            
            if (server != null) server.Start();
            Form.Inst.AddLog("Server started on " + ipAddress + ":" + port);
            Thread serverThread = new Thread(ServerLoop);
            serverThread.Start();
        }

        public void Stop()
        {
            if (isRunning==false)
                return;
            isRunning = false;
            if (server != null) server.Stop();
            Form.Inst.AddLog("Server stopped.");
        }

        public static void SendDataToClient(TcpClient client, Packet packet)
    {
        try
        {
                IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;

                NetworkStream stream = client.GetStream();
            string json = PacketSerializer.Serialize(packet);
            byte[] data = Encoding.UTF8.GetBytes(json);

            byte[] dataLength = BitConverter.GetBytes(data.Length);
            stream.Write(dataLength, 0, dataLength.Length);
            stream.Write(data, 0, data.Length);

                Form.Inst.AddLog($"Server SendData {endpoint}//{packet.Type} // {packet.Data}");
            }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send data to client: {ex.Message}");
        }
    }
    }
    
    public class Room
    {
        public string RoomName { get; private set; }
        private HashSet<IPEndPoint> clientEndpoints;

        public Room(string roomName)
        {
            RoomName = roomName;
            clientEndpoints = new HashSet<IPEndPoint>();
        }

        public void AddClient(TcpClient client)
        {
            IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint != null)
            {
                clientEndpoints.Add(endpoint);
            }
        }

        public void RemoveClient(TcpClient client)
        {
            IPEndPoint endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            if (endpoint != null)
            {
                clientEndpoints.Remove(endpoint);
            }
        }

        public void BroadcastMessage(string message, TcpClient sender, Dictionary<IPEndPoint, TcpClient> clientMap)
        {
            IPEndPoint senderEndpoint = sender.Client.RemoteEndPoint as IPEndPoint;

            List<Task> sendTasks = new List<Task>();

            foreach (var endpoint in clientEndpoints)
            {
                if (!endpoint.Equals(senderEndpoint) && clientMap.TryGetValue(endpoint, out TcpClient client))
                {
                    sendTasks.Add(Task.Run(() => TcpServer.SendDataToClient(client, new Packet { Type = PacketType.Text, Data = message })));
                }
            }

            Task.WhenAll(sendTasks).Wait();
        }

        
    }
    public static class PacketSerializer
    {
        public static string Serialize(Packet packet)
        {
            return JsonConvert.SerializeObject(packet);
        }

        public static Packet Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<Packet>(json);
        }
    }
}



