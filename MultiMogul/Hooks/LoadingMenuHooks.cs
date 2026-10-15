using HarmonyLib;
using MultiMogul.Config;
using MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static TMPro.TMP_InputField;

namespace MultiMogul.Hooks;

[HarmonyPatch]
public class LoadingMenuHooks {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(LoadingMenu), "OnEnable")]
	public static void PostOnEnable(LoadingMenu __instance) {
		ServerConfig.InitializeUI();
		ServerConfig.SetUIParent(__instance.transform);
		ServerConfig.SetInputSize(new Vector2(220, 35));

		RectTransform rectTransform = ServerConfig.nameInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-525, -600);
		}

		rectTransform = ServerConfig.passwordInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-525, -640);
		}

		rectTransform = ServerConfig.maxPlayerInput.GetComponent<RectTransform>();
		if (rectTransform != null) {
			rectTransform.anchoredPosition = new Vector2(-525, -680);
		}

		ServerConfig.EnableUI();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(LoadingMenu), "OnDisable")]
	public static void PostOnDisable(LoadingMenu __instance) {
		ServerConfig.Reset();
		ServerConfig.DisableUI();
	}
}
