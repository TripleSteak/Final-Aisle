using FinalAisle_Shared.Networking.Packet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object representation for the <see cref="AsyncClient"/>.
/// </summary>
public sealed class PacketReceivedEventArgs
{
    public Packet Packet { get; set; }
}
