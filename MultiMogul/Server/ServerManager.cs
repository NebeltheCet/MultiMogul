using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Steam;
using MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;

namespace MultiMogul.Server;

public class ServerManager : MonoBehaviour {
	public Dictionary<Connection, ConnectionData> connectedClients = new Dictionary<Connection, ConnectionData>();
	public Lobby currentLobby = new Lobby(0);

	private SocketManager _serverSocket;

	[Header("Server Defaults")]
	public int maxPlayers = 8;

	public enum ServerLobbyType : int { // straight copy from Facepunch.Steamworks
		Private = 0,
		FriendsOnly = 1,
		Public = 2,
		Invisible = 3,
		PrivateUnique = 4,
	}

	// initialize the ServerManager object
	public static void Init() {
		if (MultiMogulBase.serverManager != null) {
			MMLog.LogWarning($"multiple instances of {nameof(ServerManager)} detected, possibly undefined behaviour?");
			return;
		}

		GameObject gameObject = new GameObject("MM_ServerManager", [typeof(ServerManager)]);
		DontDestroyOnLoad(gameObject);

		MMLog.Log("created ServerManager object", LogTypes.ControlFlow);

		MMLog.Log("finished initializing");
	}

	public static void Shutdown() {
		if (MultiMogulBase.serverManager == null) {
			MMLog.LogWarning("called without valid instance");
			return;
		}

		MultiMogulBase.serverManager.StopServer();
		MultiMogulBase.serverManager = null;
	}

	private void Awake() {
		MultiMogulBase.serverManager = this;
	}

	private void FixedUpdate() {
		// disconnect any non tracked players, and update player count
		if (this._serverSocket != null && this._serverSocket.Connected.Count > 0) {
			foreach (var currentConnection in this._serverSocket.Connected) {
				if (this.connectedClients.ContainsKey(currentConnection))
					continue; // only close connections that aren't "tracked"

				currentConnection.Close(true);
			}

			this.currentLobby.SetData("playerCount", (this.connectedClients.Count + 1).ToString()); // update player count, add the host aswell.
		}
	}

	private void Update() {
		this._serverSocket?.Receive();
	}

	public void StartServer() {
		if (this._serverSocket != null) {
			MMLog.LogWarning("server is already running");
			return;
		}

		this._serverSocket = SteamNetworkingSockets.CreateRelaySocket<ServerSocketManager>();
		if (this._serverSocket == null) {
			MMLog.LogError("failed to create server socket");
			return;
		}

		MMLog.Log($"server socket created: {SteamClient.SteamId}");
		this.CreateLobbyAsync(this.maxPlayers, ServerLobbyType.Public);
	}

	public void StopServer() {
		if (this._serverSocket != null && this._serverSocket.Connected.Count > 0) {
			foreach (var currentConnection in this._serverSocket.Connected) { // disconnect all players
				currentConnection.Close(true, 0, "Server Closed");
			}
		}

		this._serverSocket?.Close();
		this._serverSocket = null;

		if (this.currentLobby.Id != 0) {
			this.currentLobby.Leave();
		}
		this.currentLobby = new Lobby(0);

		MMLog.Log($"server socket stopped.");
	}

	private async void CreateLobbyAsync(int maxPlayers = 8, ServerLobbyType lobbyType = ServerLobbyType.Private) {
		var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
		if (!lobbyResult.HasValue) {
			MMLog.LogError("failed to create steam lobby");
			return;
		}

		this.currentLobby = lobbyResult.Value;
		this.currentLobby.SetJoinable(true); // allow people to join

		// change the publicity type
		switch (lobbyType) {
			case ServerLobbyType.Private:
				this.currentLobby.SetPrivate();
				break;
			case ServerLobbyType.FriendsOnly:
				this.currentLobby.SetFriendsOnly();
				break;
			case ServerLobbyType.Public:
				this.currentLobby.SetPublic();
				break;
			case ServerLobbyType.Invisible:
				this.currentLobby.SetInvisible();
				break;
			default:
				break;
		}

		// Set custom key/value data that clients can filter on, incase we want to make a server browser
		this.currentLobby.SetData("gameName", Application.productName);
		this.currentLobby.SetData("gameBuild", Application.version + " | " + Application.unityVersion);

		// game data
		this.currentLobby.SetData("serverID", SteamClient.SteamId.ToString());
		this.currentLobby.SetData("serverName", $"{SteamClient.SteamId.Value}'s Lobby");
		this.currentLobby.SetData("playerCount", "1");
		this.currentLobby.SetData("maxPlayers", maxPlayers.ToString());

		MMLog.Log($"created steam lobby: {SteamClient.SteamId}[{lobbyType.ToString().ToLower()}]", LogTypes.ControlFlow);
	}

	public void OnClientMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel) {
		if (!this.connectedClients.ContainsKey(connection)) {
			MMLog.LogWarning($"client with id [{connection.Id}] is not tracked as connected. ignoring message");
			return;
		}

		RawPacket receivedPacket = new RawPacket(data, size);

		int messageHash = receivedPacket.Read<string>().GetHashCode();
		foreach (MethodInfo method in Networkable.packetHandlers[receivedPacket.PacketType]) {
			if (method.GetCustomAttributes(typeof(Networkable), false).FirstOrDefault() is not Networkable attribute)
				continue;

			if (!method.IsStatic)
				continue;

			if (attribute.packetHash != messageHash)
				continue;

			ThreadDispatcher.Enqueue(() => {
				method.Invoke(null, [this.connectedClients[connection], receivedPacket]);
				receivedPacket.Dispose();
			});
			break;
		}
	}

	public void OnClientConnected(Connection connection, ConnectionState connectionState) {
		if (this.connectedClients.ContainsKey(connection)) {
			MMLog.LogWarning($"client with id [{connection.Id}] is already tracked as connected. ignoring connect");
			return;
		}

		// accept client connection
		connection.Accept();

		//TODO: send connection approval packet

		// add the connection as client
		this.connectedClients.Add(connection, new ConnectionData {
			connection = connection
		});

		MMLog.Log($"client with id [{connection.Id}] connected", LogTypes.ControlFlow);
	}

	public void OnClientDisconnected(Connection connection, ConnectionState connectionState) {
		if (!this.connectedClients.ContainsKey(connection)) {
			MMLog.LogWarning($"client with id [{connection.Id}] is not tracked as connected. ignoring disconnect");
			return;
		}

		connection.Close(false);
		this.connectedClients.Remove(connection);

		MMLog.Log($"client with id [{connection.Id}] disconnected", LogTypes.ControlFlow);
	}
}