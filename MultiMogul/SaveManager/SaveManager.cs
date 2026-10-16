using MultiMogul.Hooks;
using MultiMogul.Utilities;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiMogul.SaveManager;

public static class SaveManager {
	public static string GetCurrentSave() {
		Singleton<SavingLoadingManager>.Instance.SaveGame(Singleton<SavingLoadingManager>.Instance.ActiveSaveFileName, false);
		return SavingLoadingManagerHooks.lastSaveJsonData;
	}

	public static string CleanOreEntriesFromSave(string saveData) {
		SaveFile saveFile = JsonUtility.FromJson<SaveFile>(saveData);
		if (saveFile == null) {
			MMLog.LogException("failed to parse save data, assuming fatal error in save parsing, notify the developers!");
		}

		if (saveFile.OrePieces != null) {
			saveFile.OrePieces.Clear(); // clear the list, we'll network this later manually.
		}

		return JsonUtility.ToJson(saveFile, true);
	}

	public static string GetCurrentSceneName() {
		Scene currentScene = SceneManager.GetActiveScene();
		return currentScene.name;
	}

	public static IEnumerator LoadSaveFromJson(string sceneName, string jsonData, GameModeType gameMode) {
		ReflectionProperty<bool> IsCurrentlyLoadingGame = ReflectionHelper.GetProperty<bool>(Singleton<SavingLoadingManager>.Instance, "IsCurrentlyLoadingGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

		MMLog.Log($"loading save from json with data:", LogTypes.ControlFlow);
		MMLog.Log($"sceneName:\t{sceneName}", LogTypes.ControlFlow);
		MMLog.Log($"gameMode:\t{gameMode}", LogTypes.ControlFlow);

		string tempFile = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString() + ".tmp");
		MMLog.Log($"temp file generated at \"{tempFile}\"", LogTypes.ControlFlow);
		try {
			File.WriteAllText(tempFile, jsonData);

			Singleton<SavingLoadingManager>.Instance.LoadSceneThenLoadSave(tempFile, sceneName, gameMode);
			while (IsCurrentlyLoadingGame.Value) {
				yield return null;
			}

			MMLog.Log($"finished loading save data!", LogTypes.ControlFlow);
		}
		finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}
}
