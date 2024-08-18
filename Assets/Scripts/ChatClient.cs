using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ChatClient : MonoBehaviour
{
    [SerializeField]
    private Button _button1;
    [SerializeField]
    private Button _button2;
    [SerializeField]
    private Button _button3;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

    void Start()
    {
        ConnectToServer("127.0.0.1", 8888);

        _button1.onClick.AddListener(() =>
        {
            SendMessageToServer("1");
        });
        _button2.onClick.AddListener(() =>
        {
            SendMessageToServer("2");
        });
        _button3.onClick.AddListener(() =>
        {
            SendMessageToServer("3");
        });
    }

    void ConnectToServer(string ipAddress, int port)
    {
        try
        {
            client = new TcpClient(ipAddress, port); // 이 객체가 null인지 확인
            stream = client.GetStream();
            isConnected = true;

            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

        }
        catch (Exception ex)
        {
            Debug.LogError("Connection error: " + ex.Message);
        }
    }

    void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (isConnected)
        {
            try
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    continue;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Debug.Log("Received: " + message);
                AppendMessageToChat(message);
            }
            catch (Exception ex)
            {
                Debug.LogError("Receive error: " + ex.Message);
                isConnected = false;
            }
        }
    }

    public void SendMessageToServer(string s)
    {
        if (!isConnected)
            return;

        if (string.IsNullOrEmpty(s))
            return;

        byte[] buffer = Encoding.ASCII.GetBytes(s);
        stream.Write(buffer, 0, buffer.Length);
    }

    public void AppendMessageToChat(string message)
    {
        Debug.Log($"Message : {message}");
    }


    void OnApplicationQuit()
    {
        isConnected = false;
        receiveThread.Abort();
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }
}
