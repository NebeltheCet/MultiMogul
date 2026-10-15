using HarmonyLib;
using UnityEngine;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public static class PauseMenuHooks {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(PauseMenu), "OnEnable")]
	public static void PostOnEnable(PauseMenu __instance) {
		ReflectionField<float> originalTimeScale = ReflectionHelper.GetField<float>(__instance, "_originalTimeScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		Time.timeScale = originalTimeScale.Value; // prevent pausing the game
	}
}
