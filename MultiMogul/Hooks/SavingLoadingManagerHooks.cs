using HarmonyLib;
using MultiMogul.Server;

namespace MultiMogul.Hooks;
using SaveManager = SaveManager.SaveManager;


[HarmonyPatch]
public class SavingLoadingManagerHooks {
	public static string lastSaveJsonData = string.Empty;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.IsSaveFileCompatible))]
	public static void PostIsSaveFileCompatible(SavingLoadingManager __instance, int version, ref bool __result) {
		if (version < SaveManager.MULTIPLAYER_VERSION) {
			__result = false;
			return;
		}

		__result = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), "WriteSaveAtomically")]
	public static void PostWriteSaveAtomically(SavingLoadingManager __instance, string fullFilePath, string json) {
		if (!ServerManager.IsHosting())
			return; // prevent clients from running this code.

		lastSaveJsonData = json;
	}
}
