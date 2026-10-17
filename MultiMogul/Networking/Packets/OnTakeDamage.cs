using UnityEngine;

namespace MultiMogul.Networking.Packets;

public class CustomDamageInfo {
	public float Damage;
	public DamageType DamageType;
	public Vector3 Position;

	public static CustomDamageInfo Read(RawPacket packet) {
		return new CustomDamageInfo {
			Damage = packet.Read<float>(),
			DamageType = packet.Read<DamageType>(),
			Position = packet.Read<Vector3>()
		};
	}

	public void Write(RawPacket packet) {
		packet.Write(this.Damage);
		packet.Write(this.DamageType);
		packet.Write(this.Position);
	}
}

public class OnTakeDamage : RawPacket {
	private const PacketType customType = PacketType.OnRPCMessage;

	public OnTakeDamage() : base(customType, nameof(OnTakeDamage)) { }

	// packet members
	public CustomDamageInfo damageInfo = new CustomDamageInfo();
	public ulong victimGuid = 0;

	// packet type handler register
	static OnTakeDamage() {
		RegisterHandler<OnTakeDamage>((p, v) => {
			var q = (OnTakeDamage)v;

			p.Write(q.damageInfo);
			p.Write(q.victimGuid);
		}, p => new OnTakeDamage {
			damageInfo = p.Read<CustomDamageInfo>(),
			victimGuid = p.Read<ulong>()
		});
	}

	// packet conversion serializer
	public override RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnTakeDamage));
		packet.Write<OnTakeDamage>(this);

		return packet;
	}
}
