using HarmonyLib;
using MultiMogul.Config;
using MultiMogul.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public class LoadingMenuHooks {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(LoadingMenu), "OnEnable")]
	public static void PostOnEnable(LoadingMenu __instance) {
		ServerConfig.InitializeUI();
		ServerConfig.SetUIParent(__instance.transform);
		ServerConfig.SetInputSize(new Vector2(220, 35));

		RectTransform rectTransform = ServerConfig.nameInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-525, -600);
		}

		rectTransform = ServerConfig.passwordInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-525, -640);
		}

		rectTransform = ServerConfig.maxPlayerInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-525, -680);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LoadingMenu), nameof(LoadingMenu.OnConfirmLoadGamePressed))]
	[HarmonyPatch(typeof(LoadingMenu), nameof(LoadingMenu.OnLoadGamePressed))]
	[HarmonyPatch(typeof(LoadingMenu), nameof(LoadingMenu.OnQuickLoadPressed))]
	public static bool PreOnConfirmLoadGamePressed(LoadingMenu __instance) {
		if (string.IsNullOrEmpty(ServerConfig.lobbyName) || string.IsNullOrEmpty(ServerConfig.lobbyMaxPlayers)) {
			MMLog.LogWarning("preventing load game because either lobby name or max players is empty!");
			return false;
		}

		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(LoadingMenu), "OnDisable")]
	public static void PostOnDisable(LoadingMenu __instance) {
		ServerConfig.Reset();
	}
}
