using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class PauseMenuEnable
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PauseMenu), "OnEnable")]
        public static bool PreOnEnable(PauseMenu __instance)
        {
            MethodInfo RefreshToggleHudText = __instance.GetType().GetMethod("RefreshToggleHudText", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _originalTimeScale = __instance.GetType().GetField("_originalTimeScale", BindingFlags.NonPublic | BindingFlags.Instance);

            __instance.QuitMenu.SetActive(false);
            __instance.ReturnToMainMenuMenu.SetActive(false);
            __instance.SettingsMenu.SetActive(false);
            __instance.MainUIPanel.SetActive(true);
            __instance.SaveGameMenu.SetActive(false);
            __instance.VersionNumberText.text = Singleton<VersionManager>.Instance.GetFormattedVersionText();
            RefreshToggleHudText.Invoke(__instance, null);
            _originalTimeScale.SetValue(__instance, Time.timeScale);
            if ((float)_originalTimeScale.GetValue(__instance) <= 0f)
            {
                _originalTimeScale.SetValue(__instance, 1f);
            }
            
            if (Singleton<SavingLoadingManager>.Instance.LastSaveTime != 0f)
            {
                __instance.LastSaveTimeText.text = "Last Saved: " + Singleton<SavingLoadingManager>.Instance.GetFormattedLastSaveTime();
            }

            return false;
        }
    }
}
