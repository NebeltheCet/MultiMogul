using DG.Tweening.Core.Easing;
using MultiMogul.MultiMogul.Hooks;
using System;
using System.Collections;
using System.Collections.Generic;
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
        public static string GetSaveFile()
        {
            SaveFileHeader saveFileHeader = new SaveFileHeader();
            saveFileHeader.SaveVersion = 3;
			saveFileHeader.GameVersion = global::Singleton<VersionManager>.Instance.VersionNumber;
			saveFileHeader.SaveTimestamp = DateTime.Now.ToString("o");
			saveFileHeader.Money = global::Singleton<EconomyManager>.Instance.Money;

			SaveFile saveFile = new SaveFile();
			saveFile.SaveVersion = saveFileHeader.SaveVersion;
			saveFile.GameVersion = saveFileHeader.GameVersion;
			saveFile.SaveTimestamp = saveFileHeader.SaveTimestamp;
			saveFile.Money = saveFileHeader.Money;

			HashSet<ISaveLoadableObject> hashSet = UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>().OfType<ISaveLoadableObject>().ToHashSet<ISaveLoadableObject>();
			hashSet.AddRange(from item in UnityEngine.Object.FindObjectOfType<PlayerInventory>().Items

			where item != null
			select item);
			foreach (ISaveLoadableObject saveLoadableObject in hashSet)
			{
				if (saveLoadableObject.ShouldBeSaved())
				{
					SaveEntry saveEntry = new SaveEntry
					{
						SavableObjectID = saveLoadableObject.GetSavableObjectID(),
						Position = saveLoadableObject.GetPosition(),
						Rotation = saveLoadableObject.GetRotation()
					};

					string customSaveData = saveLoadableObject.GetCustomSaveData();
					if (!string.IsNullOrEmpty(customSaveData))
					{
						saveEntry.CustomDataJson = customSaveData;
					}

					saveFile.Entries.Add(saveEntry);
				}
			}

			foreach (OrePiece orePiece in UnityEngine.Object.FindObjectsOfType<OrePiece>())
			{
				OrePieceEntry item3 = new OrePieceEntry
				{
					Position = orePiece.transform.position,
					Rotation = orePiece.transform.rotation.eulerAngles,
					Scale = orePiece.transform.localScale,
					MeshID = orePiece.MeshID,
					ResourceType = orePiece.ResourceType,
					PieceType = orePiece.PieceType,
					PolishedPercent = orePiece.PolishedPercent
				};

				saveFile.OrePieces.Add(item3);
			}

			foreach (ISaveLoadableWorldEvent saveLoadableWorldEvent in UnityEngine.Object.FindObjectsOfType<UnityEngine.MonoBehaviour>().OfType<ISaveLoadableWorldEvent>().ToList<ISaveLoadableWorldEvent>())
			{
				if (saveLoadableWorldEvent.GetHasHappened())
				{
					WorldEventEntry item2 = new WorldEventEntry
					{
						SavableWorldEventType = saveLoadableWorldEvent.GetWorldEventType(),
						WorldEventID = saveLoadableWorldEvent.GetWorldEventID(),
						CustomDataJson = saveLoadableWorldEvent.GetCustomSaveData()
					};

					saveFile.WorldEventEntries.Add(item2);
				}
			}

			SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;

            saveFile.ShopPurchases = global::Singleton<EconomyManager>.Instance.ShopPurchases;
			saveFile.DestroyedStaticBreakablePositions = (List<Vector3>)saveLoadManager.GetType().GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(saveLoadManager);
			saveFile.CompletedQuestsIDs = global::Singleton<QuestManager>.Instance.GetCompletedQuestIDs();
			saveFile.ActiveQuests = global::Singleton<QuestManager>.Instance.GetActiveQuestSaveEntries();

			PlayerController playerController = UnityEngine.Object.FindObjectOfType<PlayerController>();
			saveFile.PlayerPosition = playerController.transform.position;
			saveFile.PlayerRotation = playerController.transform.rotation.eulerAngles;

            return JsonUtility.ToJson(saveFile, true);
        }

		public static void LoadSave(string save)
		{
            NetworkedObjectRegistry.Clear();

            SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;

            PropertyInfo IsCurrentlyLoadingGame = typeof(SavingLoadingManager).GetProperty("IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _destroyedStaticBreakablePositions = typeof(SavingLoadingManager).GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance);

            IsCurrentlyLoadingGame.SetValue(saveLoadManager, true);
            if (save.Length <= 0)
            {
                IsCurrentlyLoadingGame.SetValue(saveLoadManager, false);
                return;
            }

            foreach (ISaveLoadableObject saveLoadableObject in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableObject>())
            {
                UnityEngine.Object.Destroy(((MonoBehaviour)saveLoadableObject).gameObject);
            }

            SaveFile saveFile = JsonUtility.FromJson<SaveFile>(save);
            if (saveFile.SaveVersion != 1)
            {
                OrePiece[] array = UnityEngine.Object.FindObjectsOfType<OrePiece>();
                for (int i = 0; i < array.Length; i++)
                {
                    UnityEngine.Object.Destroy(array[i].gameObject);
                }
            }

            _destroyedStaticBreakablePositions.SetValue(saveLoadManager, saveFile.DestroyedStaticBreakablePositions);
            foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableStaticBreakable>())
            {
                if (((List<Vector3>)_destroyedStaticBreakablePositions.GetValue(saveLoadManager)).Contains(saveLoadableStaticBreakable.GetPosition()))
                {
                    saveLoadableStaticBreakable.DestroyFromLoading();
                }
            }

            foreach (SaveEntry saveEntry in saveFile.Entries)
            {
                GameObject prefab = saveLoadManager.GetPrefab(saveEntry.SavableObjectID);
                ISaveLoadableObject saveLoadableObject2;
                GameObject obj = UnityEngine.Object.Instantiate<GameObject>(prefab, saveEntry.Position, Quaternion.Euler(saveEntry.Rotation));
                NetworkedObjectRegistry.Register<GameObject>(obj);
                if (prefab != null && obj.TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
                {
                    MinerHooks.allowOverride = true;
                    saveLoadableObject2.LoadFromSave(saveEntry.CustomDataJson);
                    MinerHooks.allowOverride = false;
                }
            }

            foreach (OrePieceEntry orePieceEntry in saveFile.OrePieces)
            {
                OrePiece orePiecePrefab = saveLoadManager.GetOrePiecePrefab(orePieceEntry.ResourceType, orePieceEntry.PieceType, orePieceEntry.PolishedPercent > 0.95f);
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

                    NetworkedObjectRegistry.Register<OrePiece>(orePiece);
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

            if (global::Singleton<UIManager>.Instance != null)
            {
                global::Singleton<UIManager>.Instance.PauseMenu.OnResumePressed();
            }

            saveLoadManager.LastSaveTime = Time.time;
            saveLoadManager.ActiveSaveFileName = "Multiplayer Game";
            IsCurrentlyLoadingGame.SetValue(saveLoadManager, false);
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
            saveLoadManager.StartCoroutine(LoadGameplaySceneThenRunLoadGame(save));
        }

        public static IEnumerator LoadGameplaySceneThenRunLoadGame(string save)
        {
            SavingLoadingManager saveLoadManager = SavingLoadingManager.Instance;

            MainMenu mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();
            if (mainMenu != null)
            {
                yield return saveLoadManager.StartCoroutine(mainMenu.PlayElevatorLowerAnimation());
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Gameplay");
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            LoadSave(save);
            yield break;
        }
    }
}
