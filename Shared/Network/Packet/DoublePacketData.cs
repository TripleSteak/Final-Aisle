namespace Final_Aisle_Shared.Network.Packet
{
    public sealed class DoublePacketData : PacketData
    {
        internal double Value { get; set; }
        public DoublePacketData(string key, double value) : base(key) => Value = value;
    }
}
