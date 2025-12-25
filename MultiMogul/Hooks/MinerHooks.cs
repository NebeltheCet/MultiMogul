using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class MinerHooks
    {
        public static bool allowOverride = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AutoMiner), "TurnOn")]
        public static void PostTurnOn(AutoMiner __instance)
        {
            if (allowOverride)
                return;

            if (__instance == null)
            {
                Debug.LogWarning("AutoMiner instance is null in PostTurnOn hook.");
                return;
            }

            Miner.SendUpdate(__instance.transform.position, true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AutoMiner), "TurnOff")]
        public static void PostTurnOff(AutoMiner __instance)
        {
            if (allowOverride)
                return;

            if (__instance == null)
            {
                Debug.LogWarning("AutoMiner instance is null in PostTurnOff hook.");
                return;
            }

            Miner.SendUpdate(__instance.transform.position, false);
        }
    }
}
