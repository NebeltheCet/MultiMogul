using MultiMogul.Server;
using MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine;

namespace MultiMogul.Client;

public class ClientManager : MonoBehaviour {
	// initialize the ClientManager object
	public static void Init() {
		if (MultiMogulBase.clientManager != null) {
			MMLog.LogWarning($"multiple instances of {nameof(ClientManager)} detected, possibly undefined behaviour?");
			return;
		}

		GameObject gameObject = new GameObject("MM_ClientManager", [typeof(ClientManager)]);
		DontDestroyOnLoad(gameObject);

		MMLog.Log("finished initializing");
	}

	public static void Shutdown() {
		if (MultiMogulBase.clientManager == null) {
			MMLog.LogWarning("called without valid instance");
			return;
		}

		MultiMogulBase.clientManager.Disconnect();
		SteamFriends.OnGameLobbyJoinRequested -= MultiMogulBase.clientManager.OnGameLobbyJoinRequested;
		MultiMogulBase.clientManager = null;
	}

	private void Awake() {
		MultiMogulBase.clientManager = this;

		SteamFriends.OnGameLobbyJoinRequested += this.OnGameLobbyJoinRequested;
	}

	private void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId) {
		Debug.Log($"Received lobby invite from {friendId}, joining lobby {lobby.Id}");
		connectedServerId = friendId;
		ConnectToServer();
	}

	public void Disconnect() {

	}
}