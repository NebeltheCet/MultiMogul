using MultiMogul.Utilities;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static TMPro.TMP_InputField;
using static UnityEngine.UIElements.DefaultEventSystem.LegacyInputProcessor;

namespace MultiMogul.Config;

// NOTE: this may require reworking at some point depending on what we add.
// currently this specifically targets the server configuration ui when loading/creating a save
public static class ServerConfig {
	public static TMP_InputField nameInput = null;
	public static TMP_InputField passwordInput = null;
	public static TMP_InputField maxPlayerInput = null;

	private static bool isUIInitialized = false;

	public static void InitializeUI() {
		if (isUIInitialized)
			return;

		NewGameMenu newGameMenu = GameObject.FindFirstObjectByType<NewGameMenu>(FindObjectsInactive.Include);
		if (!newGameMenu) {
			MMLog.LogException("failed to retrieve NewGameMenu instance");
		}

		ReflectionField<TMP_InputField> newSaveFileNameInputField = ReflectionHelper.GetField<TMP_InputField>(newGameMenu, "_newSaveFileNameInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (!nameInput) {
			nameInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyNameTextInput", "", "Lobby name: 'MMTestLobby'");
		}

		if (!passwordInput) {
			passwordInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyPasswordTextInput", "", "Lobby password: '123'", ContentType.Password);
		}

		if (!maxPlayerInput) {
			maxPlayerInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyMaxPlayersTextInput", "", "Lobby max players: '8'", ContentType.IntegerNumber);
		}

		isUIInitialized = true;
	}

	public static void SetInputSize(Vector2 size) {
		RectTransform rectTransform = nameInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.sizeDelta = size;
		}

		rectTransform = passwordInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.sizeDelta = size;
		}

		rectTransform = maxPlayerInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.sizeDelta = size;
		}
	}

	public static void SetUIParent(Transform parent) {
		if (!isUIInitialized)
			return;

		if (nameInput) {
			nameInput.transform.SetParent(parent);
		}

		if (passwordInput) {
			passwordInput.transform.SetParent(parent);
		}

		if (maxPlayerInput) {
			maxPlayerInput.transform.SetParent(parent);
		}
	}

	public static void EnableUI() {
		nameInput.gameObject.SetActive(true);
		passwordInput.gameObject.SetActive(true);
		maxPlayerInput.gameObject.SetActive(true);
	}

	public static void DisableUI() {
		nameInput.gameObject.SetActive(false);
		passwordInput.gameObject.SetActive(false);
		maxPlayerInput.gameObject.SetActive(false);
	}

	public static void Reset() {
		nameInput.text = "";
		passwordInput.text = "";
		maxPlayerInput.text = "";
	}
}
