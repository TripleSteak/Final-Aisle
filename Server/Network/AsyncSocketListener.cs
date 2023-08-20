using Final_Aisle_Server.Network.EventArgs;
using Final_Aisle_Shared.Network.Packet;
using Final_Aisle_Shared.Network;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Final_Aisle_Server.Network
{
    /// <summary>
    /// TCP server socket endpoint that listens for messages on the network.
    /// </summary>
    public sealed class AsyncSocketListener
    {
        private ManualResetEvent _allDone = new ManualResetEvent(false);

        public event UserConnectedEvent OnUserConnected;
        public event UserDisconnectedEvent OnUserDisconnected;
        public event PacketReceivedEvent OnPacketReceived;
        public event PacketSentEvent OnPacketSent;

        /// <summary>
        /// Accumulator tracking quantity of network uploads.
        /// </summary>
        public long TotalBytesSent;
        
        /// <summary>
        /// Accumulator tracking quantity of network downloads.
        /// </summary>
        public long TotalBytesReceived;

        public const int Port = 8031;

        public AsyncSocketListener() { }

        /// <summary>
        /// Initializes the <see cref="Socket"/> listener and begins listening for connection attempts.
        /// </summary>
        public void StartListening()
        {
            var ip = IPAddress.Any;
            var localEndpoint = new IPEndPoint(ip, Port);
            var listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndpoint);
                listener.Listen(100);
                ConsoleLog.WriteBigStatus("Final Aisle listening server started on port " + Port + ".");

                while (true)
                {
                    _allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    _allDone.WaitOne();
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.WriteSmallError(ex.ToString());
            }
        }

        /// <summary>
        /// Attempts to send the given <see cref="Packet"/> to the specified <see cref="NetworkUser"/>.
        /// </summary>
        public void Send(NetworkUser user, Packet obj)
        {
            byte[] byteData = obj.Serialize(user.UserAES);

            try
            {
                var socket = user.Connection.Socket;
                socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback),
                    new SendCallbackArgs { User = user, Packet = obj });
            }
            catch (SocketException)
            {
                // Socket closed
            }
        }
        
        /// <summary>
        /// Invoked once we found a client trying to connect.
        /// </summary>
        private void AcceptCallback(IAsyncResult ar)
        {
            _allDone.Set();
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            var user = new NetworkUser();
            user.Connection.Socket = handler;
            handler.BeginReceive(user.Connection.Buffer, 0, Connection.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), user);
            OnUserConnected?.Invoke(this, new UserEventArgs(user));
        }

        /// <summary>
        /// Accepts and reads data sent from a client.
        /// </summary>
        private void ReadCallback(IAsyncResult ar)
        {
            var user = (NetworkUser)ar.AsyncState;
            var handler = user.Connection.Socket;

            var bytesRead = 0;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnUserDisconnected?.Invoke(this, new UserEventArgs(user));
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }

            TotalBytesReceived += bytesRead;

            if (bytesRead > 0)
            { 
                user.Connection.Message.AddRange(user.Connection.Buffer.Take(bytesRead));

                var byteCount = BitConverter.ToInt32(user.Connection.Message.Take(sizeof(Int32)).ToArray(), 0);
                while (user.Connection.Message.Count >= byteCount + sizeof(Int32))
                { 
                    // Read as many docked packets as possible (e.g. if 2 are available, read both)
                    if (user.AESKeySent)
                    { 
                        // Receive message as normal
                        try
                        {
                            var p = PacketSerializer.Deserialize(user.Connection.Message.Take(byteCount + sizeof(Int32)), user.UserAES); // don't overshoot the amount of bytes sent to the packet
                            OnPacketReceived?.Invoke(this, new PacketEventArgs { Packet = p, User = user });
                        }
                        catch (InvalidDataException)
                        {
                            ConsoleLog.WriteSmallError("Could not parse message sent by " + user.UserAccount.Username + ": invalid data exception (message length: " + byteCount + ")"); // unknown cause
                        }
                    }
                    else
                    { 
                        // If AES key has not been sent to client yet, send AES key to client
                        var byteData = user.Connection.Message.Skip(sizeof(Int32)).Decompress().ToArray();
                        var publicKeyString = Encoding.ASCII.GetString(byteData);

                        // Convert string into RSA public key
                        var sr = new StringReader(publicKeyString);
                        var xs = new XmlSerializer(typeof(RSAParameters));
                        var publicKey = (RSAParameters)xs.Deserialize(sr);

                        // Load public key parameters
                        var csp = new RSACryptoServiceProvider(2048);
                        csp.ImportParameters(publicKey);

                        var aesKey = user.UserAES.Key;
                        var cipherText = csp.Encrypt(aesKey, false).Compress().PrependLength().ToArray();

                        // Send to client
                        user.Connection.Socket.BeginSend(cipherText, 0, cipherText.Length, SocketFlags.None, new AsyncCallback(SendCallback), new SendCallbackArgs { User = user, Packet = null });

                        user.AESKeySent = true;
                    }
                    
                    // Skip over already-read bytes
                    user.Connection.Message = user.Connection.Message.Skip(byteCount + sizeof(Int32)).ToList(); 

                    if (user.Connection.Message.Count >= sizeof(Int32)) 
                    {
                        // Only reset byte count if the byte buffer hasn't been cleared
                        byteCount = BitConverter.ToInt32(user.Connection.Message.Take(sizeof(Int32)).ToArray(), 0);
                    }
                }

                try
                {
                    handler.BeginReceive(user.Connection.Buffer, 0, Connection.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), user);
                }
                catch (SocketException)
                {
                    ConsoleLog.WriteSmallError("Error occurred while communicating with user #" + user.ID + " from " + user.Connection.Socket.RemoteEndPoint + ".");
                }
            }
            else
            {
                OnUserDisconnected?.Invoke(this, new UserEventArgs(user));
                handler.Close();
            }
        }

        /// <summary>
        /// Callback method once <see cref="Send"/> has been invoked.
        /// </summary>
        private void SendCallback(IAsyncResult ar)
        {
            var args = (SendCallbackArgs)ar.AsyncState;

            var user = args.User;
            var socket = user.Connection.Socket;
            var bytesSent = 0;

            try
            {
                bytesSent = socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                OnUserDisconnected?.Invoke(this, new UserEventArgs(user));
            }

            TotalBytesSent += bytesSent;
            OnPacketSent?.Invoke(this, new PacketEventArgs { User = user, Packet = args.Packet });
        }
        private class SendCallbackArgs
        {
            public NetworkUser User { get; internal set; }
            public Packet Packet { get; internal set; }
        }

        public delegate void UserConnectedEvent(object sender, UserEventArgs user);
        public delegate void UserDisconnectedEvent(object seender, UserEventArgs user);
        public delegate void PacketReceivedEvent(object sender, PacketEventArgs args);
        public delegate void PacketSentEvent(object sender, PacketEventArgs args);
    }
}
