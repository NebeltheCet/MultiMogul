using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.Networking.Packets;

// this is a basic copy of "OrePieceEntry", with the added bonus of the guid for each object.
// NOTE: this has to be adjusted if the OrePieceEntry is ever modified
public class CustomOrePieceEntry {
	public ResourceType ResourceType;
	public PieceType PieceType;
	public float PolishedPercent;

	public Vector3 Position;
	public Vector3 Rotation;
	public Vector3 Scale;

	public int MeshID;


	public string guid;

	public static CustomOrePieceEntry Read(RawPacket packet) {
		return new CustomOrePieceEntry {
			ResourceType = packet.Read<ResourceType>(),
			PieceType = packet.Read<PieceType>(),
			PolishedPercent = packet.Read<float>(),

			Position = packet.Read<Vector3>(),
			Rotation = packet.Read<Vector3>(),
			Scale = packet.Read<Vector3>(),

			MeshID = packet.Read<int>(),


			guid = packet.Read<string>()
		};
	}

	public void Write(RawPacket packet) {
		packet.Write(this.ResourceType);
		packet.Write(this.PieceType);
		packet.Write(this.PolishedPercent);

		packet.Write(this.Position);
		packet.Write(this.Rotation);
		packet.Write(this.Scale);

		packet.Write(this.MeshID);


		packet.Write(this.guid);
	}
}

public class CustomOreNodeEntry {
	public Vector3 Position;

	public string guid;

	public static CustomOreNodeEntry Read(RawPacket packet) {
		return new CustomOreNodeEntry {
			Position = packet.Read<Vector3>(),

			guid = packet.Read<string>()
		};
	}

	public void Write(RawPacket packet) {
		packet.Write(this.Position);

		packet.Write(this.guid);
	}
}

public class OnOreReplication : RawPacket {
	private const PacketType customType = PacketType.OnOreReplication;

	public OnOreReplication() : base(customType, nameof(OnOreReplication)) { }

	// packet members
	public List<CustomOrePieceEntry> orePieceEntries = [];
	public List<CustomOreNodeEntry> oreNodeEntries = [];
	public List<Vector3> destroyedStaticBreakablePositions = [];

	// packet type handler register
	static OnOreReplication() {
		RegisterHandler<OnOreReplication>((p, v) => {
			var q = (OnOreReplication)v;

			p.Write(q.orePieceEntries);
			p.Write(q.oreNodeEntries);
			p.Write(q.destroyedStaticBreakablePositions);

		}, p => new OnOreReplication {
			orePieceEntries = p.Read<List<CustomOrePieceEntry>>(),
			oreNodeEntries = p.Read<List<CustomOreNodeEntry>>(),
			destroyedStaticBreakablePositions = p.Read<List<Vector3>>()
		});
	}

	// packet conversion serializer
	public override RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnOreReplication));
		packet.Write<OnOreReplication>(this);

		return packet;
	}
}
