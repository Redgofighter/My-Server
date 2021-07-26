using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;

    //Ip list
    //127.0.0.1 is the local host
    //ipconfig for local network 
    //Google "My ip" for public one
    public string ip = "197.0.0.1";
    public int Port = 1645;
    public int myid = 0;
    public TCP tcp;
    public UDP udp;

    private delegate void PacketHandler(Packet _packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Object already exists delete");
            Destroy(this);
        }
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    public void ConnetToServer()
    {
        InitializeClientData();
        tcp.Connet();
        Debug.Log("Connecting to server");
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] reciveBuffer;

        public void Connet()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            reciveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.Port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(reciveBuffer, 0, dataBufferSize, ReceiveCallBack, null);
        }

        public void SendData(Packet _packet)
        {
            try
            {
                if(socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch(Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        private void ReceiveCallBack(IAsyncResult _result)
        {
            try
            {
                int _byteLengh = stream.EndRead(_result);
                if (_byteLengh <= 0)
                {
                    //Todo:Discconet
                    return;
                }

                byte[] _data = new byte[_byteLengh];
                Array.Copy(reciveBuffer, _data, _byteLengh);

                receivedData.Reset(HandleData(_data));
                stream.BeginRead(reciveBuffer, 0, dataBufferSize, ReceiveCallBack, null);


            }
            catch
            {
                //Add discconnet
            }
        }

        private bool HandleData(byte[] _data)
        {
            int _packetLenght = 0;

            receivedData.SetBytes(_data);

            if(receivedData.UnreadLength() >= 4)
            {
                _packetLenght = receivedData.ReadInt();

                if(_packetLenght <= 0)
                {
                    return true;
                }
            }

            while(_packetLenght > 0 && _packetLenght <= receivedData.UnreadLength())
            {
                byte[] _packetByte = receivedData.ReadBytes(_packetLenght);
                ThreadManger.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetByte))
                    {
                        int _packetid = _packet.ReadInt();
                        packetHandlers[_packetid](_packet);
                    }
                });

                _packetLenght = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLenght = receivedData.ReadInt();

                    if (_packetLenght <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLenght <= 1)
            {
                return true;
            }

            return false;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.Port);
        }

        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallBack, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet)
        {
            try
            {
                _packet.InsertInt(instance.myid);
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch(Exception _ex)
            {
                Debug.Log($"Error when sending UDP data {_ex} in Client.cs/UDP/SendData");
            }
        }

        private void ReceiveCallBack(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallBack, null);
                //Debug.Log(_data);

                if(_data.Length < 4)
                {
                    // TODO: Disconnect
                    return;
                }
            }
            catch
            { 
                // Todo Disconnet
            }
        }

        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLengh = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLengh);
            }

            ThreadManger.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    packetHandlers[_packetId](_packet);
                }
            });
        }
    }

    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandler.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandler.SpawnPlayer },
            { (int)ServerPackets.playerPosition, ClientHandler.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandler.PlayerRotation }

        };
        Debug.Log("Initializing Packets");
    }
}
