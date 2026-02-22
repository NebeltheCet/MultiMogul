using MultiMogul.Utilities;
using System;

namespace MultiMogul.Networking.Packets;

public enum ConnectionResponseCode {
	Invalid = 0,
	Success,
	InvalidPassword
}

public class OnConnectionResponse : RawPacket {
	private const PacketType customType = PacketType.Invalid;

	public OnConnectionResponse() : base(customType, "OnConnectionResponse") {
		if (this.Usage == PacketUsage.Writing) {
			this.Write<OnConnectionResponse>(this);
		}
	}

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
}
