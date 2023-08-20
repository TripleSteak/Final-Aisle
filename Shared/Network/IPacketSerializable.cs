using Final_Aisle_Shared.Network.Packet;
using System.Collections.Generic;

namespace Final_Aisle_Shared.Network
{
    /// <summary>
    /// Interface to be inherited by all object classes that require delivery across the network.
    /// </summary>
    public interface IPacketSerializable
    {
        /// <summary>
        /// Returns a list of all objects that are to serialized into a packet
        /// </summary>
        List<object> GetSerializableComponents();

        /// <summary>
        /// Returns an object containing properties deserialized from the compound packet, given a starting index.
        /// </summary>
        object Deserialize(CompositePacketData data, int startIndex);
    }
}