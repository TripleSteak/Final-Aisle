using Final_Aisle_Shared.Network.Packet;

namespace Final_Aisle_Server.Network.EventArgs
{
    /// <summary>
    /// Event object related to packet/user interaction.
    /// </summary>
    public sealed class PacketEventArgs
    {
        public NetworkUser User { get; set; }
        public Packet Packet { get; set; }
    }
}
