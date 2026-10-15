using HarmonyLib;
using MultiMogul.Entities;
using MultiMogul.Networking.Packets;
using MultiMogul.Server;
using Steamworks;
using UnityEngine;

namespace MultiMogul.Hooks;
using SaveManager = SaveManager.SaveManager;


[HarmonyPatch]
public class SavingLoadingManagerHooks {
	public static string lastSaveJsonData = string.Empty;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.IsSaveFileCompatible))]
	public static void PostIsSaveFileCompatible(SavingLoadingManager __instance, int version, ref bool __result) {
		if (version < SaveManager.MULTIPLAYER_VERSION) {
			__result = false;
			return;
		}

		__result = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), "WriteSaveAtomically")]
	public static void PostWriteSaveAtomically(SavingLoadingManager __instance, string fullFilePath, string json) {
		if (!ServerManager.IsHosting())
			return; // prevent clients from running this code.

		lastSaveJsonData = json;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
	public static void PostLoadGame(SavingLoadingManager __instance, string fullFilePath) {
		PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();

		// create the host local player
		Player.CreatePlayer(new PlayerDataResponse {
			steamId = SteamClient.SteamId.Value,
			position = playerController.transform.position,
			rotation = playerController.transform.rotation
		});
	}
}
