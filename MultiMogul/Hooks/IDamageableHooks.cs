using HarmonyLib;
using MultiMogul.Entities;
using MultiMogul.Server;
using MultiMogul.Utilities;
using MultiMogul.Utilities.NetworkedObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public static class IDamageableHooks { // this doesn't have its own method name, as IDamageable only contains "TakeDamage" as of v0.4.0.8
	public static bool ignoreHook = false;
	
	static IEnumerable<MethodBase> TargetMethods() {
		return InterfaceTargets.Find<IDamageable>(nameof(IDamageable.TakeDamage), [typeof(DamageInfo)]);
	}

	[HarmonyPostfix]
	static void PostTakeDamage(MonoBehaviour __instance, DamageInfo damageInfo) {
		if (ignoreHook)
			return; // avoid accidentally causing take damage recursion

		DamageEntities.TakeDamage(__instance, damageInfo);
	}
}
