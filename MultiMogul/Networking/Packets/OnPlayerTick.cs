using System;
using UnityEngine;

namespace MultiMogul.Networking.Packets;
// an example class for implementing packets
public class OnPlayerTick : RawPacket {
	private const PacketType customType = PacketType.OnRPCMessage;

	public OnPlayerTick() : base(customType, nameof(OnPlayerTick), true) { }

	// packet members
	public ulong steamId = 0;
	public Vector3 position = Vector3.zero;
	public Quaternion rotation = Quaternion.identity;

	// packet type handler register
	static OnPlayerTick() {
		RegisterHandler<OnPlayerTick>((p, v) => {
			var q = (OnPlayerTick)v;

			p.Write(q.steamId);
			p.Write(q.position);
			p.Write(q.rotation);
		}, p => new OnPlayerTick {
			steamId = p.Read<ulong>(),
			position = p.Read<Vector3>(),
			rotation = p.Read<Quaternion>()
		});
	}

	// packet conversion serializer
	public override RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnPlayerTick), true);
		packet.Write<OnPlayerTick>(this);

		return packet;
	}
}
