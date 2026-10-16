using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.Networking.Packets;


public class PlayerReplicationData {
	public ulong steamId = 0;
	public Vector3 position = Vector3.zero;
	public Quaternion rotation = Quaternion.identity;

	public static PlayerReplicationData Read(RawPacket packet) {
		return new PlayerReplicationData {
			steamId = packet.Read<ulong>(),
			position = packet.Read<Vector3>(),
			rotation = packet.Read<Quaternion>()
		};
	}

	public void Write(RawPacket packet) {
		packet.Write(this.steamId);
		packet.Write(this.position);
		packet.Write(this.rotation);
	}
}

public class OnPlayerReplication : RawPacket {
	private const PacketType customType = PacketType.OnPlayerReplication;

	public OnPlayerReplication() : base(customType, nameof(OnPlayerReplication)) { }

	// packet members
	public List<PlayerReplicationData> playerReplications = [];

	// packet type handler register
	static OnPlayerReplication() {
		RegisterHandler<OnPlayerReplication>((p, v) => {
			var q = (OnPlayerReplication)v;

			p.Write(q.playerReplications);

		}, p => new OnPlayerReplication {
			playerReplications = p.Read<List<PlayerReplicationData>>(),
		});
	}

	// packet conversion serializer
	public override RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnPlayerReplication));
		packet.Write<OnPlayerReplication>(this);

		return packet;
	}
}
