using MultiMogul.Utilities;
using Steamworks;
using System;

namespace MultiMogul.Networking.Packets;

public class OnUserInformationRequest : RawPacket {
	private const PacketType customType = PacketType.OnUserInformationRequest;

	public OnUserInformationRequest() : base(customType, "OnUserInformationRequest") {
		if (this.Usage == PacketUsage.Writing) {
			this.Write<OnUserInformationRequest>(this);
		}
	}

	// packet members
	public int passwordHash = 0;
	public ulong steamId = 0;

	// packet type handler register
	static OnUserInformationRequest() {
		RegisterHandler<OnUserInformationRequest>((p, v) => {
			var q = (OnUserInformationRequest)v;

			p.Write(q.steamId);
			p.Write(q.passwordHash);

		}, p => new OnUserInformationRequest {
			steamId = p.Read<ulong>(),
			passwordHash = p.Read<int>()
		});
	}
}
