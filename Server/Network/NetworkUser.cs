
using Final_Aisle_Server.Data;
using System;
using System.Security.Cryptography;

namespace Final_Aisle_Server.Network
{
    /// <summary>
    /// Represents a single client connected to the server
    /// </summary>
    public class NetworkUser
    {
        /// <summary>
        /// Static variable that accumulates to assign a new ID to each new connection
        /// </summary>
        public static int IDSequence;

        /// <summary>
        /// Identifier for each client, which is unique among all past/present connections to this server instance.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Represents the connection that connects this <see cref="NetworkUser"/>.
        /// </summary>
        public Connection Connection { get; set; }

        public SymmetricAlgorithm UserAES { get; }
        public bool AESKeySent;
        public bool SecureConnectionEstablished;
        public bool LoggedIn;

        public UserAccount UserAccount;

        public NetworkUser()
        {
            Connection = new Connection();
            ID = IDSequence++; // accumulate ID sequence by 1

            UserAES = new RijndaelManaged();
            UserAES.Key = GenerateAES();
            UserAES.Padding = PaddingMode.PKCS7;
        }

        private static byte[] GenerateAES()
        {
            var key = new byte[32];
            var rand = new Random();
            rand.NextBytes(key);

            return key;
        }
    }
}
