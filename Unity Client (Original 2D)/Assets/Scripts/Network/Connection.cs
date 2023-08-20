using Newtonsoft.Json;
using FinalAisle_Shared.Networking.Packet;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using UnityEngine;
using System;

/// <summary>
/// <see cref="MonoBehaviour"/> that provides Unity GameObjects with a way of interacting with the networking system.
/// </summary>
public sealed class Connection : MonoBehaviour
{
    private AsyncClient _client;
    private DataProcessor _processor;

    public void Awake()
    {
        UnityThread.InitUnityThread();
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        _processor = GetComponent<DataProcessor>();

        _client = new AsyncClient(this);
        _client.OnPacketReceived += (object sender, PacketReceivedEventArgs e) => _processor.ParseInput(e);
        SendData("Establishing connection..."); // attempt to send a message in order to set up secure connection
    }

    /// <summary>
    /// Tells the <see cref="Connection"/>'s <see cref="AsyncClient"/> to attempt to connect to the remote server.
    /// </summary>
    public void Connect()
    {
        _client.StartClient();
    }

    /// <summary>
    /// Attempts to send the given string data to the server.
    /// </summary>
    public void SendData(string data)
    {
        try
        {
            var packet = new Packet { Type = PacketType.Message, Data = new MessagePacketData(data) };
            _client.Send(packet);
        }
        catch (Exception)
        {
            UnityEngine.Debug.Log("Data not sent to server... awaiting connection...");
        }
    }

    public void OnApplicationQuit()
    {
        _client.Dispose();
    }
}
