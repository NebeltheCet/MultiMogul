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

		MultiMogulBase.serverManager.StartServer("Test Game");
	}
}
