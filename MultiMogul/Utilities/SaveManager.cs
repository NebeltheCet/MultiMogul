using DG.Tweening.Core.Easing;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Hooks;
using MultiMogul.MultiMogul.Utilities.CustomSave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiMogul.MultiMogul.Utilities
{
    public class SaveManager
    {
        public static string SaveGame(string saveFileName = "", bool shouldTakeScreenshot = true)
        {
            SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;
            FieldInfo _destroyedStaticBreakablePositions = saveLoadManager.GetType().GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance);

            string fullSaveFilePath = "";
            if (saveFileName.Count() > 0)
            {
                fullSaveFilePath = SavingLoadingManager.GetFullSaveFilePath(saveFileName, true);
                if (shouldTakeScreenshot)
                {
                    saveLoadManager.StartCoroutine(saveLoadManager.SaveJpgScreenshot(fullSaveFilePath, 85));
                }
            }

            CustomSaveFile saveFile = new CustomSaveFile();
            saveFile.SaveVersion = 3;
            saveFile.GameVersion = Singleton<VersionManager>.Instance.VersionNumber;
            saveFile.SaveTimestamp = DateTime.Now.ToString("o");
            saveFile.Money = Singleton<EconomyManager>.Instance.Money;

            HashSet<ISaveLoadableObject> hashSet = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableObject>().ToHashSet<ISaveLoadableObject>();
            hashSet.AddRange(UnityEngine.Object.FindObjectOfType<PlayerInventory>().Items.Where((BaseHeldTool item) => item != null));

            foreach (ISaveLoadableObject saveLoadableObject in hashSet)
            {
                if (!saveLoadableObject.ShouldBeSaved())
                    continue;

                NetworkedObjectRegistry.Register<MonoBehaviour>((MonoBehaviour)saveLoadableObject);
                CustomSaveEntry saveEntry = new CustomSaveEntry
                {
                    SavableObjectID = saveLoadableObject.GetSavableObjectID(),
                    Position = saveLoadableObject.GetPosition(),
                    Rotation = saveLoadableObject.GetRotation(),
                    GUID = (saveLoadableObject as MonoBehaviour).GetComponent<NetworkedObject>()?.guid ?? ""
                };

                string customSaveData = saveLoadableObject.GetCustomSaveData();
                if (!string.IsNullOrEmpty(customSaveData))
                {
                    saveEntry.CustomDataJson = customSaveData;
                }

                saveFile.Entries.Add(saveEntry);
            }

            foreach (OrePiece orePiece in UnityEngine.Object.FindObjectsOfType<OrePiece>())
            {
                NetworkedObjectRegistry.Register<OrePiece>(orePiece);
                CustomOrePieceEntry orePieceEntry = new CustomOrePieceEntry
                {
                    Position = orePiece.transform.position,
                    Rotation = orePiece.transform.rotation.eulerAngles,
                    Scale = orePiece.transform.localScale,
                    MeshID = orePiece.MeshID,
                    ResourceType = orePiece.ResourceType,
                    PieceType = orePiece.PieceType,
                    PolishedPercent = orePiece.PolishedPercent,
                    GUID = orePiece.GetComponent<NetworkedObject>()?.guid ?? ""
                };

                saveFile.OrePieces.Add(orePieceEntry);
            }

            foreach (ISaveLoadableWorldEvent saveLoadableWorldEvent in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableWorldEvent>().ToList<ISaveLoadableWorldEvent>())
            {
                if (!saveLoadableWorldEvent.GetHasHappened())
                    continue;

                NetworkedObjectRegistry.Register<MonoBehaviour>((MonoBehaviour)saveLoadableWorldEvent);
                CustomWorldEventEntry worldEventEntry = new CustomWorldEventEntry
                {
                    SavableWorldEventType = saveLoadableWorldEvent.GetWorldEventType(),
                    WorldEventID = saveLoadableWorldEvent.GetWorldEventID(),
                    CustomDataJson = saveLoadableWorldEvent.GetCustomSaveData(),
                    GUID = ((MonoBehaviour)saveLoadableWorldEvent).GetComponent<NetworkedObject>()?.guid ?? ""
                };

                saveFile.WorldEventEntries.Add(worldEventEntry);
            }

            List<Vector3> destroyedStaticBreakablePositions = (List<Vector3>)_destroyedStaticBreakablePositions.GetValue(saveLoadManager);
            foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableStaticBreakable>())
            {
                NetworkedObjectRegistry.Register<MonoBehaviour>((MonoBehaviour)saveLoadableStaticBreakable);
                CustomStaticBreakableEntry existingEntry = new CustomStaticBreakableEntry
                {
                    Position = saveLoadableStaticBreakable.GetPosition(),
                    GUID = ((MonoBehaviour)saveLoadableStaticBreakable).GetComponent<NetworkedObject>()?.guid ?? ""
                };

                saveFile.ExistingStaticBreakablePositions.Add(existingEntry);
            }

            foreach (Vector3 pos in destroyedStaticBreakablePositions)
            {
                CustomStaticBreakableEntry existingEntry = new CustomStaticBreakableEntry
                {
                    Position = pos,
                    GUID = ""
                };

                saveFile.DestroyedStaticBreakablePositions.Add(existingEntry);
            }

            saveFile.ShopPurchases = Singleton<EconomyManager>.Instance.ShopPurchases;
            saveFile.CompletedQuestsIDs = Singleton<QuestManager>.Instance.GetCompletedQuestIDs();
            saveFile.ActiveQuests = Singleton<QuestManager>.Instance.GetActiveQuestSaveEntries();

            PlayerController playerController = UnityEngine.Object.FindObjectOfType<PlayerController>();
            saveFile.PlayerPosition = playerController.transform.position;
            saveFile.PlayerRotation = playerController.transform.rotation.eulerAngles;

            string text = Newtonsoft.Json.JsonConvert.SerializeObject(saveFile, Newtonsoft.Json.Formatting.Indented);
            if (saveFileName.Count() > 0)
            {
                File.WriteAllText(fullSaveFilePath, text);
            }

            saveLoadManager.LastSaveTime = Time.time;
            return text;
        }

        public static void LoadGame(SavingLoadingManager saveLoadManager, string fullFilePath, bool isJsonString = false)
        {
            if (fullFilePath.Length <= 0)
                return;

            FieldInfo _destroyedStaticBreakablePositions = saveLoadManager.GetType().GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo IsCurrentlyLoadingGame = typeof(SavingLoadingManager).GetProperty("IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo ClearCart = typeof(ComputerShopUI).GetMethod("ClearCart", BindingFlags.NonPublic | BindingFlags.Instance);

            ClearCart.Invoke(UIManager.Instance?.ComputerShopUI, null);
            NetworkedObjectRegistry.Clear();

            IsCurrentlyLoadingGame.SetValue(saveLoadManager, true);
            if (!isJsonString)
            {
                if (!File.Exists(fullFilePath))
                {
                    IsCurrentlyLoadingGame.SetValue(saveLoadManager, false);
                    return;
                }
            }

            Debug.Log("destroying \"ISaveLoadableObject\" objects");
            foreach (ISaveLoadableObject saveLoadableObject in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableObject>())
            {
                UnityEngine.Object.Destroy(((MonoBehaviour)saveLoadableObject).gameObject);
            }
            
            Debug.Log("destroyed \"ISaveLoadableObject\" objects");

            
            Debug.Log("deserializing save file");
            CustomSaveFile saveFile = null;
            if (!isJsonString)
            {
                saveFile = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomSaveFile>(File.ReadAllText(fullFilePath));
            }
            else
            {
                saveFile = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomSaveFile>(fullFilePath);
            }
            
            Debug.Log("deserialized save file");


            if (saveFile.SaveVersion != 1)
            {
                Debug.Log("destroying \"OrePiece\" objects");

                OrePiece[] array = UnityEngine.Object.FindObjectsOfType<OrePiece>();
                for (int i = 0; i < array.Length; i++)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
                
                Debug.Log("destroyed \"OrePiece\" objects");
            }

            List<Vector3> destroyedStaticBreakablePositionsNew = new List<Vector3>();
            foreach (CustomStaticBreakableEntry entry in saveFile.DestroyedStaticBreakablePositions)
            {
                destroyedStaticBreakablePositionsNew.Add(entry.Position.ToVector3());
            }

            Debug.Log("removing destroyed \"ISaveLoadableStaticBreakable\" objects");

            _destroyedStaticBreakablePositions.SetValue(saveLoadManager, destroyedStaticBreakablePositionsNew);
            foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableStaticBreakable>())
            {
                if (((List<Vector3>)_destroyedStaticBreakablePositions.GetValue(saveLoadManager)).Contains(saveLoadableStaticBreakable.GetPosition()))
                {
                    saveLoadableStaticBreakable.DestroyFromLoading();
                }
            }

            Debug.Log("finished removing destroyed \"ISaveLoadableStaticBreakable\" objects");

            foreach (CustomStaticBreakableEntry entry in saveFile.ExistingStaticBreakablePositions)
            {
                foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable2 in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableStaticBreakable>())
                {
                    if (saveLoadableStaticBreakable2.GetPosition() != entry.Position.ToVector3())
                        continue;

                    NetworkedObjectRegistry.Register<MonoBehaviour>((MonoBehaviour)saveLoadableStaticBreakable2, entry.GUID);
                }
            }

            Debug.Log($"loading save entries[{saveFile.Entries.Count}]");
            foreach (CustomSaveEntry saveEntry in saveFile.Entries)
            {
                GameObject prefab = saveLoadManager.GetPrefab(saveEntry.SavableObjectID);
                ISaveLoadableObject saveLoadableObject2;

                GameObject obj = UnityEngine.Object.Instantiate<GameObject>(prefab, saveEntry.Position.ToVector3(), Quaternion.Euler(saveEntry.Rotation.ToVector3()));
                NetworkedObjectRegistry.Register<GameObject>(obj, saveEntry.GUID);
                if (prefab != null && obj.TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
                {
                    MinerHooks.allowOverride = true;
                    saveLoadableObject2.LoadFromSave(saveEntry.CustomDataJson);
                    MinerHooks.allowOverride = false;
                }
            }

            Debug.Log("finished loading save entries");

            
            Debug.Log($"loading save ore piece entries[{saveFile.OrePieces.Count}]");
            foreach (CustomOrePieceEntry orePieceEntry in saveFile.OrePieces)
            {
                OrePiece orePiecePrefab = saveLoadManager.GetOrePiecePrefab(orePieceEntry.ResourceType, orePieceEntry.PieceType, orePieceEntry.PolishedPercent > 0.95f);
                if (orePiecePrefab != null)
                {
                    OrePiece orePiece = UnityEngine.Object.Instantiate<OrePiece>(orePiecePrefab, orePieceEntry.Position.ToVector3(), Quaternion.Euler(orePieceEntry.Rotation.ToVector3()));
                    orePiece.UseRandomScale = false;
                    orePiece.transform.localScale = orePieceEntry.Scale.ToVector3();
                    orePiece.UseRandomMesh = false;
                    orePiece.MeshID = orePieceEntry.MeshID;
                    if (orePiece.PolishedPercent != 1f)
                    {
                        orePiece.PolishedPercent = orePieceEntry.PolishedPercent;
                    }

                    NetworkedObjectRegistry.Register<OrePiece>(orePiece, orePieceEntry.GUID);
                }
            }
            
            Debug.Log("finished loading save ore piece entries");

            
            Debug.Log($"loading save world event entries[{saveFile.WorldEventEntries.Count}]");

            List<ISaveLoadableWorldEvent> list = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableWorldEvent>().ToList<ISaveLoadableWorldEvent>();
            foreach (CustomWorldEventEntry worldEventEntry in saveFile.WorldEventEntries)
            {
                foreach (ISaveLoadableWorldEvent saveLoadableWorldEvent in list)
                {
                    if (saveLoadableWorldEvent.GetWorldEventID() == worldEventEntry.WorldEventID)
                    {
                        saveLoadableWorldEvent.LoadFromSave(worldEventEntry.CustomDataJson);
                    }
                }
            }

            Debug.Log($"finished loading save world event entries");

            Singleton<EconomyManager>.Instance.ShopPurchases = saveFile.ShopPurchases;
            Singleton<EconomyManager>.Instance.SetMoney(saveFile.Money);
            LoadQuestsFromSaveFile(Singleton<QuestManager>.Instance, saveFile);

            UnityEngine.Object.FindObjectOfType<PlayerInventory>().ClearInventory();
            UnityEngine.Object.FindObjectOfType<PlayerController>().TeleportPlayer(saveFile.PlayerPosition.ToVector3(), saveFile.PlayerRotation.ToVector3());
            if (Singleton<UIManager>.Instance != null)
            {
                Singleton<UIManager>.Instance.PauseMenu.OnResumePressed();
            }

            saveLoadManager.LastSaveTime = Time.time;
            saveLoadManager.ActiveSaveFileName = isJsonString ? "Multiplayer Game" : Path.GetFileNameWithoutExtension(fullFilePath);
            IsCurrentlyLoadingGame.SetValue(saveLoadManager, false);
        }

        public static void LoadQuestsFromSaveFile(QuestManager _instance, CustomSaveFile saveFile)
        {
            Debug.Log($"loading quests from save file[{saveFile.CompletedQuestsIDs.Count}]");

            FieldInfo QuestCompleted = _instance.GetType().GetField("QuestCompleted", BindingFlags.Instance | BindingFlags.NonPublic);

            _instance.ActiveQuests.Clear();
            _instance.CompletedQuests.Clear();
            foreach (QuestID completedQuestsID in saveFile.CompletedQuestsIDs)
            {
                Quest quest = Singleton<SavingLoadingManager>.Instance.GetQuestDefinition(completedQuestsID).GenerateQuest();
                if (quest != null)
                {
                    _instance.CompletedQuests.Add(quest);
                    quest.UnlockFromLoadingSaveFile();
                }
                else
                {
                    Debug.LogError("Couldn't load quest for: " + completedQuestsID);
                }
            }

            Debug.Log("finished loading quests from save file");

            Debug.Log($"loading active quest ids from save file[{saveFile.ActiveQuestsIDs.Count}]");
            foreach (QuestID activeQuestsID in saveFile.ActiveQuestsIDs)
            {
                _instance.ActiveQuests.Add(Singleton<SavingLoadingManager>.Instance.GetQuestDefinition(activeQuestsID).GenerateQuest());
            }

            Debug.Log("finished loading active quest ids from save file");

            Debug.Log($"loading active quests from save file[{saveFile.ActiveQuests.Count}]");
            foreach (ActiveQuestEntry activeQuest in saveFile.ActiveQuests)
            {
                Quest quest2 = Singleton<SavingLoadingManager>.Instance.GetQuestDefinition(activeQuest.QuestID).GenerateQuest();
                foreach (ResourceQuestRequirementEntry resourceRequirement in activeQuest.ResourceRequirements)
                {
                    foreach (ResourceQuestRequirement item in quest2.QuestRequirements.OfType<ResourceQuestRequirement>())
                    {
                        if (item.ResourceType == resourceRequirement.ResourceType && item.PieceType == resourceRequirement.PieceType && item.RequirePolishedResource == resourceRequirement.RequirePolishedResource)
                        {
                            item.CurrentAmount = resourceRequirement.CurrentAmount;
                            break;
                        }
                    }
                }

                _instance.ActiveQuests.Add(quest2);
            }

            
            Debug.Log("finished loading active quests from save file");

            if (QuestCompleted != null)
            {
                var action = QuestCompleted.GetValue(_instance) as Action;
                action?.Invoke();
            }
        }

        public static void LoadGameplaySceneThenLoadSave(string save)
        {
            SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;
            PropertyInfo IsCurrentlyLoadingGame = typeof(SavingLoadingManager).GetProperty("IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if ((bool)IsCurrentlyLoadingGame.GetValue(saveLoadManager))
            {
                return;
            }

            IsCurrentlyLoadingGame.SetValue(saveLoadManager, true);
            saveLoadManager.SceneWasLoadedFromNewGame = false;
            saveLoadManager.StartCoroutine(LoadGameplaySceneThenRunLoadGame(saveLoadManager, save));
        }

        public static IEnumerator LoadGameplaySceneThenRunLoadGame(SavingLoadingManager saveLoadManager, string save)
        {
            Debug.Log($"starting elevator scene");

            MainMenu mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();
            if (mainMenu != null)
            {
                yield return saveLoadManager.StartCoroutine(mainMenu.PlayElevatorLowerAnimation());
            }
            
            Debug.Log("finished elevator scene");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Gameplay");
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            
            Debug.Log($"loading save of size: {save.Length}");

            LoadGame(saveLoadManager, save, true);
            Debug.Log($"finished loading save of size: {save.Length}");
            yield break;
        }
    }
}
