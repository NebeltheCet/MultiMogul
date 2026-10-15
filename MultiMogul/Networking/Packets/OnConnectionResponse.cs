using MultiMogul.Utilities;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Mono.Security.X509.X520;

namespace MultiMogul.Networking.Packets;

public enum ConnectionResponseCode {
	Invalid = 0,
	Success,
	InvalidPassword
}

public class PlayerDataResponse {
	public ulong steamId = 0;
	public Vector3 position = Vector3.zero;
	public Quaternion rotation = Quaternion.identity;

	public static PlayerDataResponse Read(RawPacket packet) {
		return new PlayerDataResponse {
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

public class OnConnectionResponse : RawPacket {
	private const PacketType customType = PacketType.OnConnectionResponse;

	public OnConnectionResponse() : base(customType, nameof(OnConnectionResponse)) {}

	// packet members
	public ConnectionResponseCode responseCode = ConnectionResponseCode.Invalid;
	public GameModeType gameMode = GameModeType.Standard;
	public string sceneName = string.Empty;
	public string worldData = string.Empty;

	public List<PlayerDataResponse> playerData = [];

	// packet type handler register
	static OnConnectionResponse() {
		RegisterHandler<OnConnectionResponse>((p, v) => {
			var q = (OnConnectionResponse)v;

			p.Write(q.responseCode);
			p.Write(q.gameMode);
			p.Write(q.sceneName);
			p.Write(q.worldData);
			p.Write(q.playerData);

		}, p => new OnConnectionResponse {
			responseCode = p.Read<ConnectionResponseCode>(),
			gameMode = p.Read<GameModeType>(),
			sceneName = p.Read<string>(),
			worldData = p.Read<string>(),
			playerData = p.Read<List<PlayerDataResponse>>()
		});
	}

	// packet conversion serializer
	public override RawPacket ToRawPacket() {
		var packet = new RawPacket(customType, nameof(OnConnectionResponse), true);
		packet.Write<OnConnectionResponse>(this);

		return packet;
	}
}
