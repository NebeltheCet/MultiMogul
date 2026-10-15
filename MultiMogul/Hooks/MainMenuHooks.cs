using HarmonyLib;
using MultiMogul.Config;
using MultiMogul.Utilities;
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

		if (!ServerConfig.nameInput || !ServerConfig.maxPlayerInput)
			return;

		if (string.IsNullOrEmpty(ServerConfig.nameInput.text) || string.IsNullOrEmpty(ServerConfig.maxPlayerInput.text))
			return;

		string serverName = ServerConfig.nameInput.text;
		string serverPassword = ServerConfig.passwordInput.text;
		if (!int.TryParse(ServerConfig.maxPlayerInput.text, out int serverMaxPlayers)) {
			MMLog.LogException("failed to parse max players input field");
		}

		MultiMogulBase.serverManager.StartServer(serverName, serverPassword, serverMaxPlayers);
	}
}
