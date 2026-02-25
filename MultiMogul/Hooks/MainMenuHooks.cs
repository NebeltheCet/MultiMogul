using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public class MainMenuHooks {
	[HarmonyPrefix]
	[HarmonyPatch(typeof(MainMenu), nameof(MainMenu.PlayElevatorLowerAnimation))]
	public static void PrePlayElevatorLowerAnimation(MainMenu __instance) {
		ReflectionField<bool> hasStartedElevatorAnimation = ReflectionHelper.GetField<bool>(__instance, "_hasStartedElevatorAnimation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (hasStartedElevatorAnimation.Value)
			return;

		string serverName = "";
		string serverPassword = "";
		int serverMaxPlayers = 0;

		if(!string.IsNullOrEmpty(LoadingMenuHooks.nameInput.text) && !string.IsNullOrEmpty(LoadingMenuHooks.maxPlayerInput.text)) {
			serverName = LoadingMenuHooks.nameInput.text;
			serverPassword = LoadingMenuHooks.passwordInput.text;
			int.TryParse(LoadingMenuHooks.maxPlayerInput.text, out serverMaxPlayers);
		}
		else if (!string.IsNullOrEmpty(NewGameMenuHooks.nameInput.text) && !string.IsNullOrEmpty(NewGameMenuHooks.maxPlayerInput.text)) {
			serverName = NewGameMenuHooks.nameInput.text;
			serverPassword = NewGameMenuHooks.passwordInput.text;
			int.TryParse(NewGameMenuHooks.maxPlayerInput.text, out serverMaxPlayers);
		}

		MultiMogulBase.serverManager.StartServer(serverName, serverPassword, serverMaxPlayers);
	}
}
