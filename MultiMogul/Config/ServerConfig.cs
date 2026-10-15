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

	public static string lobbyName = string.Empty;
	public static string lobbyPassword = string.Empty;
	public static string lobbyMaxPlayers = string.Empty;

	private static bool isUIInitialized = false;

	public static void InitializeUI() {
		if (isUIInitialized)
			return;

		NewGameMenu newGameMenu = GameObject.FindFirstObjectByType<NewGameMenu>(FindObjectsInactive.Include);
		if (!newGameMenu) {
			MMLog.LogException("failed to retrieve NewGameMenu instance");
		}

		ReflectionField<TMP_InputField> newSaveFileNameInputField = ReflectionHelper.GetField<TMP_InputField>(newGameMenu, "_newSaveFileNameInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (nameInput == null) {
			nameInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyNameTextInput", string.Empty, "lobby name");
			nameInput.onValueChanged.AddListener(value => lobbyName = value);
		}

		if (passwordInput == null) {
			passwordInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyPasswordTextInput", string.Empty, "lobby password", ContentType.Password);
			passwordInput.onValueChanged.AddListener(value => lobbyPassword = value);
		}

		if (maxPlayerInput == null) {
			maxPlayerInput = UIHelper.CreateInputFieldFromInstance(newSaveFileNameInputField, "LobbyMaxPlayersTextInput", string.Empty, "max players", ContentType.IntegerNumber);
			maxPlayerInput.onValueChanged.AddListener(value => lobbyMaxPlayers = value);
		}

		isUIInitialized = true;
	}

	public static void SetInputSize(Vector2 size) {
		RectTransform rectTransform = nameInput?.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.sizeDelta = size;
		}

		rectTransform = passwordInput?.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.sizeDelta = size;
		}

		rectTransform = maxPlayerInput?.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.sizeDelta = size;
		}
	}

	public static void SetUIParent(Transform parent) {
		if (!isUIInitialized)
			return;

		nameInput?.transform?.SetParent(parent);
		passwordInput?.transform?.SetParent(parent);
		maxPlayerInput?.transform?.SetParent(parent);
	}

	public static void Reset() {
		if (nameInput != null) {
			nameInput.text = string.Empty; // trigger our OnValueChanged callback

			Object.Destroy(nameInput.gameObject);
			nameInput = null;
		}

		if (passwordInput != null) {
			passwordInput.text = string.Empty; // trigger our OnValueChanged callback

			Object.Destroy(passwordInput.gameObject);
			passwordInput = null;
		}

		if (maxPlayerInput != null) {
			maxPlayerInput.text = string.Empty; // trigger our OnValueChanged callback

			Object.Destroy(maxPlayerInput.gameObject);
			maxPlayerInput = null;
		}
	}
}
