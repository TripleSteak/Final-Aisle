using System;

namespace Final_Aisle_Shared.Network.Packet
{
    /// <summary>
    /// Object class for packets, which are bundles of data sent over the web.
    /// Contains retrieval methods for reading <see cref="Packet"/>s.
    ///
    /// Note that serialization and deserialization methods have been moved out of the shared library, as the client and server applications require different JSON dependencies.
    /// </summary>
    [Serializable]
    public sealed class Packet
    {
        /// <summary>
        /// The type of data contained in this <see cref="Packet"/>.
        /// </summary>
        private PacketType Type { get; set; }

        /// <summary>
        /// The contents of this <see cref="Packet"/>.
        /// </summary>
        private PacketData Data { get; set; }

        public Packet(PacketType type, PacketData data)
        {
            Type = type;
            Data = data;
        }

        /// <summary>
        /// Returns the key of the <see cref="Packet"/>, which is used to uniquely identify the typing of its contents.
        /// </summary>
        public string GetKey() => Data.Key;

        public bool GetBool() => Type == PacketType.Boolean ? ((BooleanPacketData)Data).Value : false;

        public double GetDouble() => Type == PacketType.Double ? ((DoublePacketData)Data).Value : 0;

        public object GetEnum(Type enumType) =>
            Type == PacketType.Enum ? Enum.Parse(enumType, ((EnumPacketData)Data).Value) : null;

        public float GetFloat() => Type == PacketType.Float ? ((FloatPacketData)Data).Value : 0;

        public int GetInt() => Type == PacketType.Integer ? ((IntegerPacketData)Data).Value : 0;

        public string GetString() => Type == PacketType.String ? ((StringPacketData)Data).Value : "";

        /// <summary>
        /// Allows public access to compound packet methods.
        /// </summary>
        public CompositePacketData GetComposite() => Type == PacketType.Composite ? (CompositePacketData)Data : null;
    }
}