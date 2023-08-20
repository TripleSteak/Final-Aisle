namespace Final_Aisle_Shared.Network.Packet
{
    public sealed class IntegerPacketData : PacketData
    {
        internal int Value { get; set; }
        public IntegerPacketData(string key, int value) : base(key) => Value = value;
    }
}
