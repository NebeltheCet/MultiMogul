using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
