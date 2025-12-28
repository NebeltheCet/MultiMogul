using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class StartingElevatorHooks
    {

        // this patch allows new saves to properly initialize networking
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartingElevator), "Update")]
        public static void PostUpdate(StartingElevator __instance)
        {
            if (!SavingLoadingManager.Instance.SceneWasLoadedFromNewGame)
                return;

            FieldInfo _isLowering = typeof(StartingElevator).GetField("_isLowering", BindingFlags.NonPublic | BindingFlags.Instance);
            if ((bool)_isLowering.GetValue(__instance))
                return;

            SaveManager.SaveGame(SavingLoadingManager.Instance.ActiveSaveFileName, true);
            SaveManager.LoadGame(SavingLoadingManager.Instance, SavingLoadingManager.GetFullSaveFilePath(SavingLoadingManager.Instance.ActiveSaveFileName, true));

            MultiMogulBase.serverManager.StartServer();
            SavingLoadingManager.Instance.SceneWasLoadedFromNewGame = false;
        }
    }
}
