using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using MultiMogul.MultiMogul.Entities;

namespace MultiMogul.MultiMogul.Hooks {
    [HarmonyPatch]
    public class ToolPickaxeHooks {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ToolPickaxe), "SwingPickaxe")]
        public static bool PreSwingPickaxe(ToolPickaxe __instance) {
            Pickaxe.SwingPickaxe(__instance);
            return false;
        }
    }
}
