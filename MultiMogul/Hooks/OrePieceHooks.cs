using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class OrePieceHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OrePiece), "Delete")]
        public static void PreDelete(OrePiece __instance)
        {
            NetworkedObjectRegistry.RemoveAllMatching(__instance.gameObject);
        }
    }
}
