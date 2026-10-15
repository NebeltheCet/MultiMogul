using HarmonyLib;
using MultiMogul.Client;
using MultiMogul.Config;
using MultiMogul.Server;
using MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public static class MainMenuHooks {
	[HarmonyPrefix]
	[HarmonyPatch(typeof(MainMenu), nameof(MainMenu.PlayElevatorLowerAnimation))]
	public static void PrePlayElevatorLowerAnimation(MainMenu __instance) {
		ReflectionField<bool> hasStartedElevatorAnimation = ReflectionHelper.GetField<bool>(__instance, "_hasStartedElevatorAnimation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (hasStartedElevatorAnimation.Value)
			return;

		if (ClientManager.IsConnected())
			return; // prevent clients from starting a server while loading in.

		if (string.IsNullOrEmpty(ServerConfig.lobbyName) || string.IsNullOrEmpty(ServerConfig.lobbyMaxPlayers))
			return;

		string serverName = ServerConfig.lobbyName;
		string serverPassword = ServerConfig.lobbyPassword;
		if (!int.TryParse(ServerConfig.lobbyMaxPlayers, out int serverMaxPlayers)) {
			MMLog.LogException("failed to parse max players input field");
		}

		MultiMogulBase.serverManager.StartServer(serverName, serverPassword, serverMaxPlayers);
	}
}
