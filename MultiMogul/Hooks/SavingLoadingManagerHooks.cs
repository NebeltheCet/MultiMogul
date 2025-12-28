using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class SavingLoadingManagerHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.SaveGame))]
        public static bool PreSaveGame(SavingLoadingManager __instance, string saveFileName, bool shouldTakeScreenshot)
        {
            SaveManager.SaveGame(saveFileName, shouldTakeScreenshot);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
        public static bool PreLoadGame(SavingLoadingManager __instance, string fullFilePath)
        {
            MethodInfo ClearCart = typeof(ComputerShopUI).GetMethod("ClearCart", BindingFlags.NonPublic | BindingFlags.Instance);

            ClearCart.Invoke(UIManager.Instance?.ComputerShopUI, null);
            SaveManager.LoadGame(__instance, fullFilePath, false);

            MultiMogulBase.serverManager.StartServer();
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.IsSaveFileCompatible))]
        public static void PostIsSaveFileCompatible(SavingLoadingManager __instance, int version, ref bool __result)
        {
            if (version < SaveManager.multiplayerBaseSaveVersion)
            {
                __result = false;
                return;
            }

            __result = true;
        }
    }
}