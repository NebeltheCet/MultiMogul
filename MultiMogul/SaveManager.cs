using DG.Tweening.Core.Easing;
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

namespace MultiMogul.MultiMogul
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

			SavingLoadingManager SaveLoadManager = SavingLoadingManager.Instance;

            saveFile.ShopPurchases = global::Singleton<EconomyManager>.Instance.ShopPurchases;
			saveFile.DestroyedStaticBreakablePositions = (List<Vector3>)SaveLoadManager.GetType().GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SaveLoadManager);
			saveFile.CompletedQuestsIDs = global::Singleton<QuestManager>.Instance.GetCompletedQuestIDs();
			saveFile.ActiveQuests = global::Singleton<QuestManager>.Instance.GetActiveQuestSaveEntries();
			PlayerController playerController = UnityEngine.Object.FindObjectOfType<PlayerController>();
			saveFile.PlayerPosition = playerController.transform.position;
			saveFile.PlayerRotation = playerController.transform.rotation.eulerAngles;

            string contents = JsonUtility.ToJson(saveFile, true);
            return contents;
        }

		public static void LoadSave(string save)
		{
            SavingLoadingManager SaveLoadManager = SavingLoadingManager.Instance;

            PropertyInfo IsCurrentlyLoadingGame = typeof(SavingLoadingManager).GetProperty("IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo DestroyedStaticBreakablePositions = typeof(SavingLoadingManager).GetField("_destroyedStaticBreakablePositions", BindingFlags.NonPublic | BindingFlags.Instance);

            IsCurrentlyLoadingGame.SetValue(SaveLoadManager, true);
            if (save.Length <= 0)
            {
                IsCurrentlyLoadingGame.SetValue(SaveLoadManager, false);
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

            DestroyedStaticBreakablePositions.SetValue(SaveLoadManager, saveFile.DestroyedStaticBreakablePositions);
            foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoadableStaticBreakable>())
            {
                if (((List<Vector3>)DestroyedStaticBreakablePositions.GetValue(SaveLoadManager)).Contains(saveLoadableStaticBreakable.GetPosition()))
                {
                    saveLoadableStaticBreakable.DestroyFromLoading();
                }
            }
            foreach (SaveEntry saveEntry in saveFile.Entries)
            {
                GameObject prefab = SaveLoadManager.GetPrefab(saveEntry.SavableObjectID);
                ISaveLoadableObject saveLoadableObject2;
                if (prefab != null && UnityEngine.Object.Instantiate<GameObject>(prefab, saveEntry.Position, Quaternion.Euler(saveEntry.Rotation)).TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
                {
                    saveLoadableObject2.LoadFromSave(saveEntry.CustomDataJson);
                }
            }
            foreach (OrePieceEntry orePieceEntry in saveFile.OrePieces)
            {
                OrePiece orePiecePrefab = SaveLoadManager.GetOrePiecePrefab(orePieceEntry.ResourceType, orePieceEntry.PieceType, orePieceEntry.PolishedPercent > 0.95f);
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
            global::Singleton<EconomyManager>.Instance.ShopPurchases = saveFile.ShopPurchases;
            global::Singleton<EconomyManager>.Instance.SetMoney(saveFile.Money);
            global::Singleton<QuestManager>.Instance.LoadFromSaveFile(saveFile);
            UnityEngine.Object.FindObjectOfType<PlayerInventory>().ClearInventory();
            UnityEngine.Object.FindObjectOfType<PlayerController>().TeleportPlayer(saveFile.PlayerPosition, saveFile.PlayerRotation);
            if (global::Singleton<UIManager>.Instance != null)
            {
                global::Singleton<UIManager>.Instance.PauseMenu.OnResumePressed();
            }
            SaveLoadManager.LastSaveTime = Time.time;
            SaveLoadManager.ActiveSaveFileName = "Multiplayer Game";
            IsCurrentlyLoadingGame.SetValue(SaveLoadManager, false);
        }

        public static void LoadGameplaySceneThenLoadSave(string save)
        {
            SavingLoadingManager SaveLoadManager = SavingLoadingManager.Instance;
            PropertyInfo IsCurrentlyLoadingGame = typeof(SavingLoadingManager).GetProperty("IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if ((bool)IsCurrentlyLoadingGame.GetValue(SaveLoadManager))
            {
                return;
            }
            IsCurrentlyLoadingGame.SetValue(SaveLoadManager, true);
            SaveLoadManager.SceneWasLoadedFromNewGame = false;
            SaveLoadManager.StartCoroutine(LoadGameplaySceneThenRunLoadGame(save));
        }

        public static IEnumerator LoadGameplaySceneThenRunLoadGame(string save)
        {
            SavingLoadingManager SaveLoadManager = SavingLoadingManager.Instance;

            MainMenu mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();
            if (mainMenu != null)
            {
                yield return SaveLoadManager.StartCoroutine(mainMenu.PlayElevatorLowerAnimation());
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
