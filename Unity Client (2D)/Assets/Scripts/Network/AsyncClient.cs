using FinalAisle_Shared.Networking.Packet;
using FinalAisle_Shared.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Asynchronous TCP socket client.
/// </summary>
public sealed class AsyncClient : IDisposable
{
    public event PacketReceivedEvent OnPacketReceived;
    private const int Port = 8032;
    
    private readonly SymmetricAlgorithm _aes;
    private readonly Connection _connection;

    private RSAParameters _privateKey;
    private byte[] _publicKeyBytes;
    private bool _aesKeyReceived;
    private bool _aesKeyRequested;
    
    private Socket _client;

    public AsyncClient(Connection connection)
    {
        _connection = connection;

        StartClient();

        _aes = new RijndaelManaged();
        _aes.Padding = PaddingMode.PKCS7;
    }
    
    /// <summary>
    /// Initializes the <see cref="Socket"/> client, and attempts to connect to the remote server endpoint.
    /// </summary>
    public void StartClient()
    {
        var ip = IPAddress.Parse("127.0.0.1"); // TODO: Change to public server IP
        var remoteEndpoint = new IPEndPoint(ip, Port);
        
        try
        {
            _client = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _client.BeginConnect(remoteEndpoint, new AsyncCallback(ConnectCallback), _client);

            // Generate RSA public-private key pair
            var csp = new RSACryptoServiceProvider(2048);
            _privateKey = csp.ExportParameters(true);
            var publicKey = csp.ExportParameters(false);

            // Translate public key into string
            var sw = new System.IO.StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, publicKey);
            string publicKeyString = sw.ToString();

            // Convert string to bytes and send RSA public key to server
            _publicKeyBytes = Encoding.ASCII.GetBytes(publicKeyString).Compress().PrependLength().ToArray();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Attempts to send the given <see cref="Packet"/> to the server.
    /// </summary>
    public void Send(Packet p)
    {
        if (_aesKeyReceived)
        { 
            // Send message as normal
            var byteData = p.Serialize(_aes);
            _client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), _client);
        }
        else if (!_aesKeyRequested)
        { 
            // If no AES key present, send public RSA key to server to request
            _client.BeginSend(_publicKeyBytes, 0, _publicKeyBytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), _client);
            UnityEngine.Debug.Log("Sending RSA key to server...");
            _aesKeyRequested = true;
        }
    }

    /// <summary>
    /// Begins listening for messages from the server.
    /// Invoke after a socket connection has been successfully established.
    /// </summary>
    public void Receive(Socket client)
    {
        try
        {
            var state = new ServerConnection();
            state.Socket = client;

            client.BeginReceive(state.Buffer, 0, ServerConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Callback method to be invoked once a successful socket connection has been established with the remote server.
    /// </summary>
    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            _client = (Socket)ar.AsyncState;
            _client.EndConnect(ar);
            UnityEngine.Debug.Log(string.Format("Socket connected to {0}", _client.RemoteEndPoint.ToString()));
            Receive(_client);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.ToString());
        }
    }

    /// <summary>
    /// Callback method to be invoked once a message from the remote server has been received.
    /// </summary>
    private void ReceiveCallback(IAsyncResult ar)
    {
        ServerConnection state = (ServerConnection)ar.AsyncState;
        Socket client = state.Socket;

        var bytesRead = 0;

        try
        {
            bytesRead = client.EndReceive(ar);
        }
        catch (Exception)
        {
            UnityEngine.Debug.Log("Cannot access a disposed socket.");
        }

        if (bytesRead > 0)
        {
            state.Message.AddRange(state.Buffer.Take(bytesRead));

            var byteCount = BitConverter.ToInt32(state.Message.Take(sizeof(Int32)).ToArray(), 0);
            if (state.Message.Count == byteCount + sizeof(int))
            {
                if (_aesKeyReceived)
                { 
                    // Receive message as normal
                    var p = Packet.Deserialize(state.Message, _aes);
                    OnPacketReceived?.Invoke(this, new PacketReceivedEventArgs { Packet = p });
                } else
                { 
                    // If AES key is not received yet, this has to be the AES key!
                    var byteData = state.Message.Skip(sizeof(int)).Decompress().ToArray();

                    var csp = new RSACryptoServiceProvider();
                    csp.ImportParameters(_privateKey);

                    _aes.Key = csp.Decrypt(byteData, false);
                    _aesKeyReceived = true;

                    // Write "Connection Established" to the server
                    _connection.SendData(PacketDataUtils.Condense(PacketDataUtils.SecureConnectionEstablished, ""));
                }
                state.Message.Clear();
            }

            client.BeginReceive(state.Buffer, 0, ServerConnection.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
        }
    }

    /// <summary>
    /// Callback method to be invoked once a message send attempt has gone through.
    /// </summary>
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            var client = (Socket)ar.AsyncState;
            var bytesSent = client.EndSend(ar);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
        }
    }

    /// <summary>
    /// Attempts to shut down the local socket client.
    /// </summary>
    public void Shutdown()
    {
        try
        {
            _client.Shutdown(SocketShutdown.Both);
            _client.Close();
        }
        catch (Exception)
        {
            // Client already closed, no additional shutdown process required
        }
    }

    public delegate void PacketReceivedEvent(object sender, PacketReceivedEventArgs e);

    #region IDisposable Support
    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        try
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                }
                
                disposedValue = true;
            }
        }
        catch (Exception)
        {
            // Object already disposed, no further action needed
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}
