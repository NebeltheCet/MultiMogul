using HarmonyLib;
using MultiMogul.Client;
using MultiMogul.Server;
using MultiMogul.Utilities;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.Core.Parsing;
using UnityEngine.UI;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public static class PauseMenuHooks {
	public static bool ignoreHook = false;

	public static GameObject multiplayerButtonObject = null;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PauseMenu), "OnEnable")]
	public static void PostOnEnable(PauseMenu __instance) {
		ReflectionField<float> originalTimeScale = ReflectionHelper.GetField<float>(__instance, "_originalTimeScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		Time.timeScale = originalTimeScale.Value; // prevent pausing the game

		if (multiplayerButtonObject == null) {
			var resumeButtonObject = __instance.gameObject.transform.Find("BackgroundCanvas/PauseMenu/ButtonsContainer/ResumeButton/").gameObject;
			multiplayerButtonObject = GameObject.Instantiate(resumeButtonObject, resumeButtonObject.transform.parent);
			multiplayerButtonObject.transform.SetSiblingIndex(1);
			multiplayerButtonObject.name = "MultiplayerButton";
			var textObject = multiplayerButtonObject.transform.Find("Text (TMP)");
			TextMeshProUGUI tmpText = textObject.GetComponent<TMPro.TextMeshProUGUI>();
			tmpText.text = "Multiplayer";	

			LocalizeStringEvent localizeEvent = textObject.GetComponent<LocalizeStringEvent>();
			if (localizeEvent != null) {
				localizeEvent.enabled = false;
			}

			Button multiplayerButton = multiplayerButtonObject.GetComponent<UnityEngine.UI.Button>();
			if (multiplayerButton != null) {
				var persistentCalls = ReflectionHelper.GetField<object>(multiplayerButton.onClick, "m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance, typeof(UnityEventBase)).Value;

				if (persistentCalls != null) {
					ReflectionHelper.GetMethod<object>(persistentCalls, "Clear", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke();
				}
			}

			/* Fix for when we add our button everything moving a bit */
			{
				var feedBackButtonObject = __instance.gameObject.transform.Find("BackgroundCanvas/PauseMenu/SteamDiscussionsButton").gameObject;

				var layoutElement = feedBackButtonObject.GetComponent<LayoutElement>();
				if (layoutElement == null) {
					layoutElement = feedBackButtonObject.AddComponent<LayoutElement>();
				}

				layoutElement.ignoreLayout = true;

				var rectTransform = feedBackButtonObject.GetComponent<RectTransform>();
				if (rectTransform != null) {
					rectTransform.anchoredPosition += new Vector2(0f, -20f);
				}
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.OnReturnToMainMenuPressed))]
	public static void PostOnReturnToMainMenuPressed(PauseMenu __instance) {
		if (ignoreHook)
			return;

		if (ServerManager.IsHosting()) {
			MultiMogulBase.serverManager.StopServer();
		}
		else if (ClientManager.IsConnected()) {
			MultiMogulBase.clientManager.Disconnect("player returned to main menu", true);
		}
	}
}
