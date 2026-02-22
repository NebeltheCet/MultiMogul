using MultiMogul.Utilities;
using System;

namespace MultiMogul.Networking.Packets;

public enum ConnectionResponseCode {
	Invalid = 0,
	Success,
	InvalidPassword
}

public class OnConnectionResponse : RawPacket {
	private const PacketType customType = PacketType.OnConnectionResponse;

	public OnConnectionResponse() : base(customType, "OnConnectionResponse") {}

	// packet members
	public ConnectionResponseCode responseCode = ConnectionResponseCode.Invalid;

	// packet type handler register
	static OnConnectionResponse() {
		RegisterHandler<OnConnectionResponse>((p, v) => {
			var q = (OnConnectionResponse)v;

			p.Write(q.responseCode);

		}, p => new OnConnectionResponse {
			responseCode = p.Read<ConnectionResponseCode>()
		});
	}

	// packet conversion serializer
	public RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnConnectionResponse));
		packet.Write<OnConnectionResponse>(this);

		return packet;
	}
}
