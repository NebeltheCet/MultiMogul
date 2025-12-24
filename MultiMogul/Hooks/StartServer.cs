using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Hooks {
    [HarmonyPatch]
    public class StartServer {
        public static bool allowOverride = true; 

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
        public static void Prefix_LoadGame() {
            if (!allowOverride)
                return;

            MultiMogulBase.serverManager.StartServer();
        }
    }
}
