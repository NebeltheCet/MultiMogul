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
	public GameModeType gameMode = GameModeType.Standard;
	public string sceneName = string.Empty;
	public string worldData = string.Empty;

	// packet type handler register
	static OnConnectionResponse() {
		RegisterHandler<OnConnectionResponse>((p, v) => {
			var q = (OnConnectionResponse)v;

			p.Write(q.responseCode);
			p.Write(q.gameMode);
			p.Write(q.sceneName);
			p.Write(q.worldData);

		}, p => new OnConnectionResponse {
			responseCode = p.Read<ConnectionResponseCode>(),
			gameMode = p.Read<GameModeType>(),
			sceneName = p.Read<string>(),
			worldData = p.Read<string>()
		});
	}

	// packet conversion serializer
	public override RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnConnectionResponse));
		packet.Write<OnConnectionResponse>(this);

		return packet;
	}
}
