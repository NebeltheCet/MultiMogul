using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class StartServer
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
        public static bool PreLoadGame(SavingLoadingManager __instance, string fullFilePath)
        {
            NetworkedObject.nextNetworkId = 0;
            SaveManager.networkedObjects.Clear();

            PropertyInfo IsCurrentlyLoadingGame = typeof(SavingLoadingManager).GetProperty("IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _destroyedStaticBreakablePositions = typeof(SavingLoadingManager).GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance);

            IsCurrentlyLoadingGame.SetValue(__instance, true);
            if (!File.Exists(fullFilePath))
            {
                IsCurrentlyLoadingGame.SetValue(__instance, false);
                return false;
            }

            foreach (ISaveLoadableObject saveLoadableObject in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableObject>())
            {
                UnityEngine.Object.Destroy(((MonoBehaviour)saveLoadableObject).gameObject);
            }

            SaveFile saveFile = JsonUtility.FromJson<SaveFile>(File.ReadAllText(fullFilePath));
            if (saveFile.SaveVersion != 1)
            {
                OrePiece[] array = UnityEngine.Object.FindObjectsOfType<OrePiece>();
                for (int i = 0; i < array.Length; i++)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
            }

            _destroyedStaticBreakablePositions.SetValue(__instance, saveFile.DestroyedStaticBreakablePositions);
            foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableStaticBreakable>())
            {
                if (((List<Vector3>)_destroyedStaticBreakablePositions.GetValue(__instance)).Contains(saveLoadableStaticBreakable.GetPosition()))
                {
                    saveLoadableStaticBreakable.DestroyFromLoading();
                }
            }

            foreach (SaveEntry saveEntry in saveFile.Entries)
            {
                GameObject prefab = __instance.GetPrefab(saveEntry.SavableObjectID);
                ISaveLoadableObject saveLoadableObject2;
                GameObject obj = UnityEngine.Object.Instantiate<GameObject>(prefab, saveEntry.Position, Quaternion.Euler(saveEntry.Rotation));
                SaveManager.AddNetworkComponent<GameObject>(obj);
                if (prefab != null && obj.TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
                {
                    saveLoadableObject2.LoadFromSave(saveEntry.CustomDataJson);
                }
            }

            foreach (OrePieceEntry orePieceEntry in saveFile.OrePieces)
            {
                OrePiece orePiecePrefab = __instance.GetOrePiecePrefab(orePieceEntry.ResourceType, orePieceEntry.PieceType, orePieceEntry.PolishedPercent > 0.95f);
                if (orePiecePrefab != null)
                {
                    OrePiece orePiece = UnityEngine.Object.Instantiate<OrePiece>(orePiecePrefab, orePieceEntry.Position, Quaternion.Euler(orePieceEntry.Rotation));
                    orePiece.UseRandomScale = false;
                    orePiece.transform.localScale = orePieceEntry.Scale;
                    orePiece.UseRandomMesh = false;
                    orePiece.MeshID = orePieceEntry.MeshID;
                    if (orePiece.PolishedPercent != 1f)
                    {
                        orePiece.PolishedPercent = orePieceEntry.PolishedPercent;
                    }

                    SaveManager.AddNetworkComponent<OrePiece>(orePiece);
                }
            }

            List<ISaveLoadableWorldEvent> list = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableWorldEvent>().ToList<ISaveLoadableWorldEvent>();
            foreach (WorldEventEntry worldEventEntry in saveFile.WorldEventEntries)
            {
                foreach (ISaveLoadableWorldEvent saveLoadableWorldEvent in list)
                {
                    if (saveLoadableWorldEvent.GetWorldEventID() == worldEventEntry.WorldEventID)
                    {
                        saveLoadableWorldEvent.LoadFromSave(worldEventEntry.CustomDataJson);
                    }
                }
            }

            Singleton<EconomyManager>.Instance.ShopPurchases = saveFile.ShopPurchases;
            Singleton<EconomyManager>.Instance.SetMoney(saveFile.Money);
            Singleton<QuestManager>.Instance.LoadFromSaveFile(saveFile);
            UnityEngine.Object.FindObjectOfType<PlayerInventory>().ClearInventory();
            UnityEngine.Object.FindObjectOfType<PlayerController>().TeleportPlayer(saveFile.PlayerPosition, saveFile.PlayerRotation);
            if (Singleton<UIManager>.Instance != null)
            {
                Singleton<UIManager>.Instance.PauseMenu.OnResumePressed();
            }

            __instance.LastSaveTime = Time.time;
            __instance.ActiveSaveFileName = Path.GetFileNameWithoutExtension(fullFilePath);
            IsCurrentlyLoadingGame.SetValue(__instance, false);

            MultiMogulBase.serverManager.StartServer();
            return false;
        }
    }
}
