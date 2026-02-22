using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Server;
using MultiMogul.Steam;
using MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MultiMogul.Client;

public class ClientManager : MonoBehaviour {
	private ConnectionManager _clientConnection;

	public SteamId currentServerId = 0;
	public string currentServerPassword = "";

	// initialize the ClientManager object
	public static void Init() {
		if (MultiMogulBase.clientManager != null) {
			MMLog.LogWarning($"multiple instances of {nameof(ClientManager)} detected, possibly undefined behaviour?");
			return;
		}

		GameObject gameObject = new GameObject("MM_ClientManager", [typeof(ClientManager)]);
		DontDestroyOnLoad(gameObject);

		MMLog.Log("created ClientManager object", LogTypes.ControlFlow);

		MMLog.Log("finished initializing");
	}

	public static void Shutdown() {
		if (MultiMogulBase.clientManager == null) {
			MMLog.LogWarning("called without valid instance");
			return;
		}

		MultiMogulBase.clientManager.Disconnect("Client Shutdown");
		SteamFriends.OnGameLobbyJoinRequested -= MultiMogulBase.clientManager.OnGameLobbyJoinRequested;
		MultiMogulBase.clientManager = null;
	}

	private void Awake() {
		MultiMogulBase.clientManager = this;

		SteamFriends.OnGameLobbyJoinRequested += this.OnGameLobbyJoinRequested;
	}

	private void FixedUpdate() {
		this._clientConnection?.Receive();
	}

	[Networkable(PacketType.OnUserInformationRequest, 32, 1, false)]
	public static void OnUserInformationRequest(ConnectionManager localConnection, RawPacket receivedPacket) {
		MMLog.Log("server requested user information.", LogTypes.ControlFlow);

		RawPacket.Send(new OnUserInformationRequest {
			passwordHash = MultiMogulBase.clientManager.currentServerPassword.GetHashCode(),
			steamId = SteamClient.SteamId
		}, localConnection.Connection, SendType.Reliable);
	}

	[Networkable(PacketType.OnConnectionResponse, 8, 1, false)]
	public static void OnConnectionResponse(ConnectionManager localConnection, RawPacket receivedPacket) {
		MMLog.Log("received connection response", LogTypes.ControlFlow);

		OnConnectionResponse connectionResponse = receivedPacket.Read<OnConnectionResponse>();
		if (connectionResponse == null) {
			MMLog.LogWarning("connection response was null!");
			return;
		}

		MMLog.Log($"received response: {connectionResponse.responseCode}");
	}

	private void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId) {
		Debug.Log($"Received lobby invite from {friendId}, joining lobby {lobby.Id}");

		this.currentServerId = friendId;
		this.ConnectToServer(); // TODO: when making the lobby ui, this should call the ui method instead.
	}

	public void OnServerMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) {
		RawPacket receivedPacket = new RawPacket(data, size);
		int messageHash = receivedPacket.Read<string>().GetHashCode();

		// packet counters aren't "required" for clients, as hosts "shouldn't" be malicious.
		foreach (MethodInfo method in Networkable.packetHandlers[receivedPacket.PacketType]) {
			if (method.GetCustomAttributes(typeof(Networkable), false).FirstOrDefault() is not Networkable attribute)
				continue;

			if (!method.IsStatic)
				continue;

			if (attribute.packetHash != messageHash)
				continue;

			ThreadDispatcher.Enqueue(() => {
				method.Invoke(null, [this._clientConnection, receivedPacket]);
				receivedPacket.Dispose();
			});
			break;
		}
	}

	public void ConnectToServer() {
		if (!SteamManager.Instance.IsInitialized) {
			MMLog.LogError("cannot connect to server without Steam initialized");
			return;
		}

		try {
			Debug.Log($"connecting to server: \"{this.currentServerId}\"");
			this._clientConnection = SteamNetworkingSockets.ConnectRelay<ClientConnection>(this.currentServerId, 0);
		}
		catch (Exception ex) {
			MMLog.LogException($"failed to connect to server: {ex}");
		}
	}

	public void Disconnect(string reason) {
		this._clientConnection?.Close();
		this._clientConnection = null;

		MMLog.Log($"client disconnected[{reason}]");
	}

	private class ClientConnection : ConnectionManager {
		public override void OnDisconnected(ConnectionInfo info) {
			MultiMogulBase.clientManager.Disconnect("client was disconnected");
		}

		public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) {
			MultiMogulBase.clientManager.OnServerMessage(data, size, messageNum, recvTime, channel);
		}
	}
}