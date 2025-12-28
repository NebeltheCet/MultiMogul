using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
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
    public class ToolMagnetHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ToolMagnet), "FixedUpdate")]
        public static void PostFixedUpdate(ToolMagnet __instance)
        {
            FieldInfo _heldBodies = typeof(ToolMagnet).GetField("_heldBodies", BindingFlags.NonPublic | BindingFlags.Instance);
            List<Rigidbody> heldBodies = (List<Rigidbody>)_heldBodies.GetValue(__instance);
            if (heldBodies == null || heldBodies.Count == 0)
                return;

            List<GameObject> grabbableObjects = heldBodies.Select(rb => rb.gameObject)
                .Where(go => go != null && go.TryGetComponent<NetworkedObject>(out _))
                .ToList();

            Grabbables.SendGrabbableUpdate(grabbableObjects, false);
            Grabbables.UpdateSentTime();
        }
    }
}
