using HarmonyLib;
using MonoMod.RuntimeDetour;
using MultiMogul.Entities;
using MultiMogul.Networking.Packets;
using MultiMogul.Server;
using MultiMogul.Utilities;
using MultiMogul.Utilities.NetworkedObjects;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MultiMogul.Hooks;

public static class SaveGuidShared {
	const string Prefix = "@nid:";
	const char Separator = '|';

	public static string Wrap(string guid, string data) {
		return $"{Prefix}{guid}{Separator}{data ?? ""}";
	}

	public static bool TryUnwrap(string wrapped, out string guid, out string data) {
		guid = null;
		data = null;

		if (wrapped == null || !wrapped.StartsWith(Prefix, System.StringComparison.Ordinal))
			return false;

		int separatorIndex = wrapped.IndexOf(Separator, Prefix.Length);
		if (separatorIndex < 0)
			return false;

		guid = wrapped.Substring(Prefix.Length, separatorIndex - Prefix.Length);
		data = wrapped.Substring(separatorIndex + 1);
		return true;
	}

	public static string GetCustomSaveData(ISaveLoadableObject saveable) {
		string data = saveable.GetCustomSaveData();
		if (!ServerManager.IsHosting())
			return data;

		if (saveable is not Component component)
			return data;

		NetworkedObject networkedObject = NetworkedObjectRegistry.Register(component);
		return Wrap(networkedObject.guid, data);
	}

	public static void LoadFromSave(ISaveLoadableObject saveable, string data) {
		if (TryUnwrap(data, out string guid, out string originalData) && saveable is Component component) {
			MMLog.Log($"received custom guid entry for \"{(saveable as Component).name}\"", LogTypes.ControlFlow);
			NetworkedObjectRegistry.Register(component, guid);
			data = originalData;
		}

		saveable.LoadFromSave(data);
	}
}

[HarmonyPatch]
static class SaveCallSiteTranspiler {
	static IEnumerable<MethodBase> TargetMethods() {
		yield return AccessTools.Method(typeof(SavingLoadingManager), "SaveGame", [typeof(string), typeof(bool)]);
		yield return AccessTools.Method(typeof(SavingLoadingManager), "LoadGame", [typeof(string)]);
	}

	static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
		MethodInfo getHook = AccessTools.Method(typeof(SaveGuidShared), nameof(SaveGuidShared.GetCustomSaveData));
		MethodInfo loadHook = AccessTools.Method(typeof(SaveGuidShared), nameof(SaveGuidShared.LoadFromSave));

		int replaced = 0;
		foreach (CodeInstruction instruction in instructions) {
			if (IsSaveableCall(instruction, "GetCustomSaveData")) {
				instruction.opcode = OpCodes.Call;
				instruction.operand = getHook;

				replaced++;
			}
			else if (IsSaveableCall(instruction, "LoadFromSave")) {
				instruction.opcode = OpCodes.Call;
				instruction.operand = loadHook;

				replaced++;
			}

			yield return instruction;
		}

		if (replaced <= 0) {
			MMLog.LogException($"no saveable calls found in {original.Name}, did a game update break the mod?");
		}

		MMLog.Log($"{original.Name}: redirected {replaced} saveable calls");
	}

	static bool IsSaveableCall(CodeInstruction instruction, string methodName) =>
		(instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Call)
		&& instruction.operand is MethodInfo method
		&& method.Name == methodName
		&& method.DeclaringType != null
		&& method.DeclaringType.IsInterface
		&& typeof(ISaveLoadableObject).IsAssignableFrom(method.DeclaringType);
}

[HarmonyPatch]
public static class SavingLoadingManagerHooks {
	public static string lastSaveJsonData = string.Empty;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), "WriteSaveAtomically")]
	public static void PostWriteSaveAtomically(SavingLoadingManager __instance, string fullFilePath, string json) {
		if (!ServerManager.IsHosting())
			return; // prevent clients from running this code.

		lastSaveJsonData = json;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
	public static void PreLoadGame(SavingLoadingManager __instance, string fullFilePath) {
		NetworkedObjectRegistry.Clear();
		if (!ServerManager.IsHosting())
			return;

		// ...
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
	public static void PostLoadGame(SavingLoadingManager __instance, string fullFilePath) {
		if (!ServerManager.IsHosting())
			return;

		MMLog.Log("registering all existing ore pieces and nodes", LogTypes.ControlFlow);
		foreach (OrePiece orePiece in Object.FindObjectsByType<OrePiece>(FindObjectsSortMode.None)) {
			_ = NetworkedObjectRegistry.Register(orePiece); // make the host register all OrePiece entries when finishing loading in.
		}

		foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveLoadableStaticBreakable>()) {
			if (saveLoadableStaticBreakable is not Component breakableComponent) {
				MMLog.LogWarning("a found static breakable was not a component? notify the developers!");
				continue;
			}

			_ = NetworkedObjectRegistry.Register(breakableComponent);
		}

		PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();

		// create the host local player
		Player.CreatePlayer(new PlayerReplicationData {
			steamId = SteamClient.SteamId.Value,
			position = playerController.transform.position,
			rotation = playerController.transform.rotation
		});
	}
}
