using Final_Aisle_Server.Network;
using Final_Aisle_Shared.Network.Packet;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Final_Aisle_Server.Data
{
    /// <summary>
    /// Utility class that assists with file manipulation.
    /// </summary>
    public static class FileUtils
    {
        public static readonly string ParentDirectory;
        public static readonly string AccountsDirectory;

        public static SymmetricAlgorithm StorageAES = new RijndaelManaged();

        static FileUtils()
        {
            // TODO: Hardcoding an encryption key is INCREDIBLY questionable, but I didn't know better when I wrote this :/
            StorageAES.Key = new byte[] { 167, 227, 157, 141, 7, 84, 186, 226, 163, 164, 22, 52, 90, 199, 77, 88, 145, 91, 100, 46, 36, 0, 51, 19, 254, 130, 6, 217, 102, 40, 179, 105 };
            StorageAES.Padding = PaddingMode.PKCS7;

            ParentDirectory = CombineAndCreate(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, "Server Data");
            AccountsDirectory = CombineAndCreate(ParentDirectory, "Account Data");

            ConsoleLog.WriteSmallData("Storage directories loaded.");
        }

        private static void WriteToFile(string directory, string fileName, byte[] data)
        {
            if (data == null || data.Length == 0) {
                // Empty file calls will NOT override existing data
                return; 
            }

            var path = CombineAndCreateText(directory, fileName);
            File.WriteAllText(path, string.Empty);

            using (var fs = File.Create(CombineAndCreateText(directory, fileName)))
            {
                fs.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Encrypts and writes the given byte <paramref name="data"/> to the file specified by the given <paramref name="directory"/> and <paramref name="fileName"/>.
        /// </summary>
        public static void WriteToFile(string directory, string fileName, string data) => WriteToFile(directory, fileName, EncryptForStorage(data));

        private static byte[] ReadBytesFromFile(string directory, string fileName)
        {
            try
            {
                var fullPath = CombineAndCreateText(directory, fileName);
                return File.ReadAllBytes(fullPath);
            }
            catch (Exception)
            {
                return new byte[] { };
            }
        }

        /// <summary>
        /// Decrypts and reads all bytes from the file specified by the given <paramref name="directory"/> and <paramref name="fileName"/>.
        /// </summary>
        public static string ReadStringFromFile(string directory, string fileName)
        {
            try
            {
                return DecryptFromStorage(ReadBytesFromFile(directory, fileName));
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Creates a new folder using the given directory and file name.
        /// </summary>
        public static string CombineAndCreate(string parent, string fileName)
        {
            var newPath = Path.Combine(parent, fileName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }
            
            return newPath;
        }

        /// <summary>
        /// Creates a new text file using the given directory and file name.
        /// </summary>
        private static string CombineAndCreateText(string parent, string fileName)
        {
            fileName += ".txt";
            var newPath = Path.Combine(parent, fileName);
            
            if (!File.Exists(newPath))
            {
                var stream = File.Create(newPath);
                stream.Close();
            }
            
            return newPath;
        }

        private static byte[] EncryptForStorage(string message)
        {
            var packet = new Packet(PacketType.String, new EmptyPacketData(message)); // storage simply uses empty packet with data as key
            return packet.Serialize(StorageAES);
        }

        private static string DecryptFromStorage(byte[] data)
        {
            return PacketSerializer.Deserialize(data, StorageAES).GetKey();
        }
    }
}
