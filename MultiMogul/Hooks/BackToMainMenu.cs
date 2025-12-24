using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks {
    [HarmonyPatch]
    public class BackToMainMenu {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenu), "OnEnable")]
        public static void PreMainMenu(MainMenu __instance)
        {
            MultiMogulBase.serverManager.StopServer();
            MultiMogulBase.clientManager.Disconnect();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "OnEnable")]
        public static void PostMainMenu(MainMenu __instance)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
