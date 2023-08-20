namespace Final_Aisle_Shared.Network.Packet
{
    public sealed class EnumPacketData : PacketData
    {
        /// <summary>
        /// Stores the string representation of the enum.
        /// </summary>
        internal string Value { get; set; }
        
        public EnumPacketData(string key, object value) : base(key) => Value = value.ToString();
    }
}
