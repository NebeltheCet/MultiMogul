using HarmonyLib;
using MultiMogul.Config;
using MultiMogul.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.TMP_InputField;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public class NewGameMenuHooks {
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

		ServerConfig.EnableUI();
	}


	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "Update")]
	public static void PostUpdate(NewGameMenu __instance) {
		ReflectionField<Button> confirmNewGameButton = ReflectionHelper.GetField<Button>(__instance, "_confirmNewGameButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (string.IsNullOrEmpty(ServerConfig.nameInput.text) || string.IsNullOrEmpty(ServerConfig.maxPlayerInput.text)) {
			confirmNewGameButton.Value.interactable = false;
			confirmNewGameButton.Value.enabled = false;
			return;
		}

		confirmNewGameButton.Value.enabled = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "OnDisable")]
	public static void PostOnDisable(NewGameMenu __instance) {
		ServerConfig.Reset();
		ServerConfig.DisableUI();
	}
}
