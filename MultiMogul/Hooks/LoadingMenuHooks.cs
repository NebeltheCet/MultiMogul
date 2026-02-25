using HarmonyLib;
using MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static TMPro.TMP_InputField;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public class LoadingMenuHooks {
	public static TMP_InputField nameInput = null;
	public static TMP_InputField passwordInput = null;
	public static TMP_InputField maxPlayerInput = null;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(LoadingMenu), "OnEnable")]
	public static void PostOnEnable(LoadingMenu __instance) {
		NewGameMenu newGameMenu = UnityEngine.Object.FindFirstObjectByType<NewGameMenu>(FindObjectsInactive.Include);
		if (!newGameMenu)
			return;

		ReflectionField<TMP_InputField> newSaveFileNameInputField = ReflectionHelper.GetField<TMP_InputField>(newGameMenu, "_newSaveFileNameInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		if (!nameInput) {
			nameInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, __instance.transform, "LobbyNameTextInput", new Vector2(-525, -600), new Vector2(220, 35), "", "Lobby name: 'MMTestLobby'");
		}

		if (!passwordInput) {
			passwordInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, __instance.transform, "LobbyPasswordTextInput", new Vector2(-525, -640), new Vector2(220, 35), "", "Lobby password: '123'", ContentType.Password);
		}

		if (!maxPlayerInput) {
			maxPlayerInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, __instance.transform, "LobbyMaxPlayersTextInput", new Vector2(-525, -680), new Vector2(220, 35), "", "Lobby max players: '8'", ContentType.IntegerNumber);
		}

		nameInput.text = "";
		passwordInput.text = "";
		maxPlayerInput.text = "";
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(LoadingMenu), "OnDisable")]
	public static void PostOnDisable(LoadingMenu __instance) {
		nameInput.text = "";
		passwordInput.text = "";
		maxPlayerInput.text = "";
	}
}
