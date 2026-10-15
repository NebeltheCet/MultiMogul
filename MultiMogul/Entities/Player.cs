using MultiMogul.Networking.Packets;
using Steamworks;
using UnityEngine;
using System.Collections.Generic;
using MultiMogul.Utilities;

namespace MultiMogul.Entities;
public class Player : MonoBehaviour {
	private static GameObject playerPrefab = null;

	public static Player LocalPlayer { get; set; }
	public static List<Player> ActivePlayers = [];

	public string DisplayName { get; set; }
	public SteamId SteamId { get; set; }

	public bool IsLocalPlayer {
		get {
			return this == LocalPlayer;
		}
	}

	private static void CreatePlayerPrefab() {
		if (playerPrefab != null) {
			return;
		}

		var player = new GameObject("PlayerPrefab", [ typeof(Player) ]);
		var playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);

		playerBody.transform.SetParent(player.transform);
		player.SetActive(false);

		playerPrefab = player;
		DontDestroyOnLoad(playerPrefab);

		MMLog.Log($"created player prefab", LogTypes.ControlFlow);
	}

	public static void CreatePlayer(PlayerDataResponse entry) {
		CreatePlayerPrefab();

		if (playerPrefab == null) {
			MMLog.LogException("invalid player prefab even after creating it, notify the developers!");
			return;
		}

		GameObject playerObject = Instantiate(playerPrefab, entry.position, entry.rotation);
		if (playerObject == null) {
			MMLog.LogException("a player object failed to instantiate, notify the developers!");
			return;
		}

		playerObject.name = $"Player_{entry.steamId}";
		playerObject.SetActive(true);

		Player playerComponent = playerObject.GetComponent<Player>();
		if (playerComponent == null) {
			MMLog.LogException("the player prefab didn't instantiate properly, notify the developers!");
			return;
		}

		Friend friend = new Friend(entry.steamId);

		playerComponent.SteamId = entry.steamId;
		playerComponent.DisplayName = friend.Name;

		if (playerComponent.SteamId == SteamClient.SteamId) {
			LocalPlayer = playerComponent;
			LocalPlayer.GetComponentInChildren<Renderer>().enabled = false; // prevent the localplayer from rendering

			MMLog.Log($"found localplayer @ \"{playerComponent.DisplayName}\" [{playerComponent.SteamId}]", LogTypes.ControlFlow);
		}

		ActivePlayers.Add(playerComponent);
		MMLog.Log($"added active player @ \"{playerComponent.DisplayName}\" [{playerComponent.SteamId}]", LogTypes.ControlFlow);

		Singleton<SoundManager>.Instance.PlaySoundAtLocation(Object.FindFirstObjectByType<PlayerController>().PlayerRespawnSound, playerComponent.transform.position, 1f, 1f, false, false);
		MMLog.Log($"played respawn sound for new active player", LogTypes.ControlFlow);
	}

	// this function is specifically called by clients to initialize the base player list.
	public static void CreatePlayerList(List<PlayerDataResponse> list) {
		if (list.Count <= 0) {
			MMLog.LogWarning("skipped player list initialization, because no entries were present.");
			return;
		}

		foreach (PlayerDataResponse entry in list) {
			CreatePlayer(entry);
		}
	}

	private void Update() {

	}
}
