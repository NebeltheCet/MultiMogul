using HarmonyLib;
using MultiMogul.Config;
using UnityEngine;
using UnityEngine.UI;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public static class NewGameMenuHooks {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "OnEnable")]
	public static void PostOnEnable(NewGameMenu __instance) {
		ServerConfig.InitializeUI();
		ServerConfig.SetUIParent(__instance.transform);

		ServerConfig.SetInputSize(new Vector2(220, 35));

		RectTransform rectTransform = ServerConfig.nameInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-300, -135);
		}

		rectTransform = ServerConfig.passwordInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-300, -175);
		}

		rectTransform = ServerConfig.maxPlayerInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-300, -215);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "Update")]
	public static void PostUpdate(NewGameMenu __instance) {
		ReflectionField<Button> confirmNewGameButton = ReflectionHelper.GetField<Button>(__instance, "_confirmNewGameButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (string.IsNullOrEmpty(ServerConfig.lobbyName) || string.IsNullOrEmpty(ServerConfig.lobbyMaxPlayers)) {
			confirmNewGameButton.Value.interactable = false;
			return;
		}

		confirmNewGameButton.Value.enabled = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "OnDisable")]
	public static void PostOnDisable(NewGameMenu __instance) {
		ServerConfig.Reset();
	}
}
