using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class AutoMinerToggle
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AutoMiner), "Toggle")]
        public static void Toggle(bool on)
        {
            //Debug.Log("Autominer toggled");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AutoMiner), "TurnOff")]
        public static void TurnOff()
        {
            //Debug.Log("Autominer off");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(AutoMiner), "TurnOn")]
        public static void TurnOn()
        {
            //Debug.Log("Autominer on");
        }
    }
}
