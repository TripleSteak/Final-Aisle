namespace Final_Aisle_Shared.Network.Packet
{
    /// <summary>
    /// Abstract class from which packet data of all types derive.
    /// </summary>
    public abstract class PacketData
    {
        /// <summary>
        /// Unique identifier of this <see cref="Packet"/>'s content signature.
        /// </summary>
        public string Key { get; set; }

        protected PacketData(string key) => Key = key;
    }
}
