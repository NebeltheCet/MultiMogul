using HarmonyLib;
using MultiMogul.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.TMP_InputField;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public class NewGameMenuHooks {
	public static TMP_InputField nameInput = null;
	public static TMP_InputField passwordInput = null;
	public static TMP_InputField maxPlayerInput = null;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "OnEnable")]
	public static void PostOnEnable(NewGameMenu __instance) {
		ReflectionField<TMP_InputField> newSaveFileNameInputField = ReflectionHelper.GetField<TMP_InputField>(__instance, "_newSaveFileNameInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		if (!nameInput) {
			nameInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyNameTextInput", new Vector2(-300, -30), "", "Lobby name: 'MMTestLobby'");
		}

		if (!passwordInput) {
			passwordInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyPasswordTextInput", new Vector2(-300, -70), "", "Lobby password: '123'", ContentType.Password);
		}

		if (!maxPlayerInput) {
			maxPlayerInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyMaxPlayersTextInput", new Vector2(-300, -110), "", "Lobby max players: '8'", ContentType.IntegerNumber);
		}

		nameInput.text = "";
		passwordInput.text = "";
		maxPlayerInput.text = "";
	}


	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewGameMenu), "Update")]
	public static void PostUpdate(NewGameMenu __instance) {
		ReflectionField<Button> confirmNewGameButton = ReflectionHelper.GetField<Button>(__instance, "_confirmNewGameButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (string.IsNullOrEmpty(nameInput.text) || string.IsNullOrEmpty(maxPlayerInput.text)) {
			confirmNewGameButton.Value.interactable = false;
			confirmNewGameButton.Value.enabled = false;
			return;
		}

		confirmNewGameButton.Value.enabled = true;
	}
}
