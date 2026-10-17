using HarmonyLib;
using MultiMogul.Client;
using MultiMogul.Server;
using UnityEngine;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public static class PauseMenuHooks {
	public static bool ignoreHook = false;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PauseMenu), "OnEnable")]
	public static void PostOnEnable(PauseMenu __instance) {
		ReflectionField<float> originalTimeScale = ReflectionHelper.GetField<float>(__instance, "_originalTimeScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		Time.timeScale = originalTimeScale.Value; // prevent pausing the game
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.OnReturnToMainMenuPressed))]
	public static void PostOnReturnToMainMenuPressed(PauseMenu __instance) {
		if (ignoreHook)
			return;

		if (ServerManager.IsHosting()) {
			MultiMogulBase.serverManager.StopServer();
		}
		else if (ClientManager.IsConnected()) {
			MultiMogulBase.clientManager.Disconnect("player returned to main menu", true);
		}
	}
}
