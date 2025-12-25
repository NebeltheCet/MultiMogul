using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AutoMiner), "Update")]
        public static bool PreUpdate(AutoMiner __instance)
        {
            if (!ClientManager.IsHost())
                return false; // we are a client, dont allow us to run this.

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AutoMiner), "TrySpawnOre")]
        public static void PreTrySpawnOre(AutoMiner __instance)
        {
            OrePiece createdOre = null;
            int orePrefabSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.InitState(orePrefabSeed);
            if (UnityEngine.Random.Range(0f, 100f) <= __instance.SpawnProbability)
            {
                OrePiece orePiece = __instance.ResourceDefinition.GetOrePrefab();
                if (orePiece == null)
                {
                    orePiece = __instance.FallbackOrePrefab;
                }

                if (orePiece != null)
                {
                    createdOre = UnityEngine.Object.Instantiate<OrePiece>(orePiece, __instance.OreSpawnPoint.position, __instance.OreSpawnPoint.rotation);
                }
            }

            if (createdOre != null)
            {
                if (ClientManager.IsHost())
                {
                    Miner.SendOreSpawn(__instance.transform.position, orePrefabSeed);
                }

                NetworkedObjectRegistry.Register<GameObject>(createdOre.gameObject);
            }
        }
    }
}
