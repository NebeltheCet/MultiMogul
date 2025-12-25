using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class MinerHooks
    {
        public static bool allowOverride = false;
        public class WeightedOre
        {
            public OrePiece OrePrefab;
            public float Weight = 100f;
        }

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

        public static OrePiece GetOrePrefab(AutoMinerResourceDefinition autoMinerInstance, ref float value)
        {
            if (float.IsNaN(value))
            {
                value = UnityEngine.Random.value;
            }

            if (autoMinerInstance == null)
                return null;

            Type autoMinerType = autoMinerInstance.GetType();
            FieldInfo prefabsField = autoMinerType.GetField("_possibleOrePrefabs", BindingFlags.NonPublic | BindingFlags.Instance);
            if (prefabsField == null)
                return null;

            var prefabsList = prefabsField.GetValue(autoMinerInstance) as IEnumerable;
            if (prefabsList == null)
                return null;

            List<object> weightedOreList = [.. prefabsList];
            if (weightedOreList.Count == 0)
                return null;

            Type weightedOreType = weightedOreList[0].GetType();
            FieldInfo weightField = weightedOreType.GetField("Weight", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo prefabField = weightedOreType.GetField("OrePrefab", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (weightField == null || prefabField == null)
                return null;

            float totalWeight = 0f;
            foreach (var weightedOre in weightedOreList)
            {
                totalWeight += (float)weightField.GetValue(weightedOre);
            }

            float randomValue = value * totalWeight;
            float accumulated = 0f;

            foreach (var weightedOre in weightedOreList)
            {
                float weight = (float)weightField.GetValue(weightedOre);
                accumulated += weight;

                if (randomValue > accumulated)
                    continue;

                return (OrePiece)prefabField.GetValue(weightedOre);
            }

            return (OrePiece)prefabField.GetValue(weightedOreList[weightedOreList.Count - 1]);
        }

        public static void TrySpawnOre(AutoMiner __instance, float value)
        {
            OrePiece createdOre = null;
            float oreRandomValue = float.NaN;

            bool probabilityPass = true;
            if (ClientManager.IsHost())
            {
                probabilityPass = UnityEngine.Random.Range(0f, 100f) <= __instance.SpawnProbability;
            }

            if (probabilityPass)
            {
                OrePiece orePiece = null;
                if (ClientManager.IsHost())
                {
                    orePiece = GetOrePrefab(__instance.ResourceDefinition, ref oreRandomValue);
                }
                else
                {
                    orePiece = GetOrePrefab(__instance.ResourceDefinition, ref value);
                }

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
                    Miner.SendOreSpawn(__instance.transform.position, oreRandomValue);
                }

                NetworkedObjectRegistry.Register<GameObject>(createdOre.gameObject);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AutoMiner), "TrySpawnOre")]
        public static bool PreTrySpawnOre(AutoMiner __instance)
        {
            TrySpawnOre(__instance, float.NaN);
            return false;
        }
    }
}
