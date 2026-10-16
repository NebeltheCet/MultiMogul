using MultiMogul.Client;
using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Server;
using MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.Entities;
public class Player : MonoBehaviour {
	private static GameObject playerPrefab = null;
	private static GameObject localController = null;

	public static Player LocalPlayer { get; set; }
	public static List<Player> activePlayers = [];
	public static bool isListInitialized = false;

	public string DisplayName { get; set; }
	public SteamId SteamId { get; set; }

	public Vector3 currentPosition = Vector3.zero; // this is usually interpolated (TODO)
	public Quaternion currentRotation = Quaternion.identity; // this is usually interpolated (TODO)

	private Vector3 _lastNetworkedPosition = Vector3.zero; // this is the last received network position or the last sent network position for this player
	private Quaternion _lastNetworkedRotation = Quaternion.identity; // this is the last received network rotation or the last sent network rotation for this player

	private float _lastSentTickTime = 0f;
	public const float TICK_INTERVAL = 1f / 20;

	public bool IsLocalPlayer {
		get {
			return this == LocalPlayer;
		}
	}

	private static void CreatePlayerPrefab() {
		if (playerPrefab != null) {
			return;
		}

		localController = Object.FindFirstObjectByType<PlayerController>().gameObject;

		var player = new GameObject("PlayerPrefab", [typeof(Player)]);
		var playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);

		playerBody.transform.SetParent(player.transform);
		player.SetActive(false);

		playerPrefab = player;
		DontDestroyOnLoad(playerPrefab);

		MMLog.Log($"created player prefab", LogTypes.ControlFlow);
	}

	public static void CreatePlayer(PlayerReplicationData entry) {
		CreatePlayerPrefab();

		if (playerPrefab == null) {
			MMLog.LogException("invalid player prefab even after creating it, notify the developers!");
		}

		Player existingEntry = activePlayers.Find(x => x.SteamId.Value == entry.steamId);
		if (existingEntry != null) {
			MMLog.LogWarning("a replicated player already existed, possible bug!");
			return;
		}

		GameObject playerObject = Instantiate(playerPrefab, entry.position, entry.rotation);
		if (playerObject == null) {
			MMLog.LogException("a player object failed to instantiate, notify the developers!");
		}

		playerObject.name = $"Player_{entry.steamId}";
		playerObject.SetActive(true);

		Player playerComponent = playerObject.GetComponent<Player>();
		if (playerComponent == null) {
			MMLog.LogException("the player prefab didn't instantiate properly, notify the developers!");
		}

		Friend friend = new Friend(entry.steamId);

		playerComponent.SteamId = entry.steamId;
		playerComponent.DisplayName = friend.Name;

		playerComponent.currentPosition = entry.position;
		playerComponent.currentRotation = entry.rotation;

		playerComponent._lastNetworkedPosition = entry.position;
		playerComponent._lastNetworkedRotation = entry.rotation;
		playerComponent._lastSentTickTime = Time.realtimeSinceStartup;

		if (playerComponent.SteamId == SteamClient.SteamId) {
			LocalPlayer = playerComponent;
			LocalPlayer.GetComponentInChildren<Renderer>().enabled = false; // prevent the localplayer from rendering

			MMLog.Log($"found localplayer @ \"{playerComponent.DisplayName}\" [{playerComponent.SteamId}]", LogTypes.ControlFlow);
		}

		activePlayers.Add(playerComponent);
		MMLog.Log($"added active player @ \"{playerComponent.DisplayName}\" [{playerComponent.SteamId}]", LogTypes.ControlFlow);

		PlayerController playerController = localController.GetComponent<PlayerController>();
		Singleton<SoundManager>.Instance.PlaySoundAtLocation(playerController.PlayerRespawnSound, playerComponent.transform.position, 1f, 1f, false, false);
		MMLog.Log($"played respawn sound for new active player", LogTypes.ControlFlow);
	}

	// this function is specifically called by clients to initialize the base player list.
	public static void CreatePlayerList(List<PlayerReplicationData> list) {
		if (list.Count <= 0) {
			MMLog.LogWarning("skipped player list initialization, because no entries were present.");
			return;
		}

		foreach (PlayerReplicationData entry in list) {
			CreatePlayer(entry);
		}
	}

	private void SendClientTick() {
		if (!this.IsLocalPlayer)
			return; // only send client ticks for the localplayer

		if (!ServerManager.IsHosting()) {
			RawPacket.Send(new OnPlayerTick {
				steamId = SteamClient.SteamId,
				position = this.currentPosition,
				rotation = this.currentRotation
			}, ClientManager.GetConnection(), SendType.Unreliable, true);
		}
		else {
			foreach (var client in MultiMogulBase.serverManager.connectedClients) {
				RawPacket.Send(new OnPlayerTick {
					steamId = SteamClient.SteamId,
					position = this.currentPosition,
					rotation = this.currentRotation
				}, client.Key, SendType.Unreliable, true);
			}
		}

		this._lastNetworkedPosition = this.currentPosition;
		this._lastNetworkedRotation = this.currentRotation;
		this._lastSentTickTime = Time.realtimeSinceStartup;
	}

	private void Update() {
		float currentTime = Time.realtimeSinceStartup;
		if ((currentTime - this._lastSentTickTime) > TICK_INTERVAL) {
			this.SendClientTick();
		}

		if (!this.IsLocalPlayer) {
			// / temporary
			// this should optimally be replaced with interpolation
			this.currentPosition = this._lastNetworkedPosition;
			this.currentRotation = this._lastNetworkedRotation;
			// temporary \

			this.transform.position = this.currentPosition;
			this.transform.rotation = this.currentRotation;
		}
		else {
			// the localplayer gets special handling to support the game's movement
			this.currentPosition = localController.transform.position;
			this.currentRotation = localController.transform.rotation;
		}
	}

	#region Packets
	[Networkable(PacketType.OnPlayerReplication, uint.MaxValue, 4, false)]
	public static void OnPlayerReplication(ConnectionManager localConnection, RawPacket receivedPacket) {
		MMLog.Log("received player replication", LogTypes.ControlFlow);
		OnPlayerReplication playerReplication = receivedPacket.Read<OnPlayerReplication>();
		if (playerReplication == null) {
			MMLog.LogWarning("received invalid player replication packet");
			return;
		}

		System.Action action = () => {
			Player.CreatePlayerList(playerReplication.playerReplications);
		};

		if (!ClientManager.loadSaveTask.IsCompleted) {
			ClientManager.loadSaveTask.AddCompletionCallback(action);
		}
		else {
			action();
		}
	}

	[Networkable(PacketType.OnRPCMessage, "OnPlayerTick", 64, (uint)(1f / TICK_INTERVAL) * 2, true)]
	public static void SV_OnPlayerTick(ConnectionData connectionData, RawPacket receivedPacket) {
		//MMLog.Log("received player tick", LogTypes.ControlFlow);
		if (connectionData == null) {
			MMLog.LogWarning("received player tick from invalid connected client");
			connectionData.connection.Close(true, 0, "Invalid Connection");
			return;
		}
		
		OnPlayerTick playerTick = receivedPacket.Read<OnPlayerTick>();
		if (playerTick == null) {
			MMLog.LogWarning("received invalid player tick packet");
			return;
		}

		Player player = activePlayers.Find(x => x.SteamId.Value == playerTick.steamId);
		if (player == null) {
			MMLog.LogWarning("received invalid target player for player tick");
			return;
		}

		player._lastNetworkedPosition = playerTick.position;
		player._lastNetworkedRotation = playerTick.rotation;
		player._lastSentTickTime = Time.realtimeSinceStartup;

		foreach (var client in MultiMogulBase.serverManager.connectedClients) {
			if (client.Key == connectionData.connection)
				continue;

			RawPacket.Send(playerTick, client.Key, SendType.Unreliable); // forward the packet to each player but the sender
		}
	}

	[Networkable(PacketType.OnRPCMessage, "OnPlayerTick", 64, (uint)(1f / TICK_INTERVAL) * 2, false)]
	public static void CL_OnPlayerTick(ConnectionManager localConnection, RawPacket receivedPacket) {
		MMLog.Log("received player tick", LogTypes.ControlFlow);
		if (activePlayers.Count <= 0) { // our client is most likely still loading in.
			//MMLog.LogWarning("discarded player tick, are we fully loaded in yet? (if this spams constantly contact the developers!)");
			return;
		}

		OnPlayerTick playerTick = receivedPacket.Read<OnPlayerTick>();
		if (playerTick == null) {
			MMLog.LogWarning("received invalid player tick packet");
			return;
		}

		Player player = activePlayers.Find(x => x.SteamId.Value == playerTick.steamId);
		if (player == null) {
			MMLog.LogWarning("received invalid target player for player tick");
			return;
		}

		player._lastNetworkedPosition = playerTick.position;
		player._lastNetworkedRotation = playerTick.rotation;
		player._lastSentTickTime = Time.realtimeSinceStartup;
	}
	#endregion
}
