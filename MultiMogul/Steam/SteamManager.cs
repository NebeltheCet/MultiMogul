using MultiMogul.Config;
using MultiMogul.Utilities;
using Steamworks;
using System;
using UnityEngine;

namespace MultiMogul.Steam;

// a GameObject that handles any Steam Messages
public class SteamManager : MonoBehaviour {
	public static SteamManager Instance = null;

	public bool IsInitialized => SteamClient.IsValid;

	public static void Init() {
		if (Instance != null) {
			MMLog.LogWarning($"multiple instances of {nameof(SteamManager)} detected, possibly undefined behaviour?");
			return;
		}

		GameObject gameObject = new GameObject("MM_SteamManager", [typeof(SteamManager)]);
		DontDestroyOnLoad(gameObject);
	}

	public static void Shutdown() {
		if (Instance == null)
			return;

		if (!Instance.IsInitialized)
			return;

		SteamClient.Shutdown();
	}

	private void Start() {
		Instance = this;

		try {
			if (!SteamClient.IsValid) {
				SteamClient.Init(Configuration.AppId, true);
				MMLog.Log($"Steamworks successfully initialized: {SteamClient.SteamId}");
			}
		}
		catch (System.Exception e) {
			MMLog.LogError($"Steamworks failed to initialize: {e.Message}");
		}
	}
}