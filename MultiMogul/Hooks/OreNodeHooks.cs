using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class OreNodeHooks
    {
        public static bool allowOverride = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OreNode), "TakeDamage")]
        public static void PreTakeDamage(OreNode __instance, float damage, Vector3 position)
        {
            if (allowOverride)
                return;

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnOreNodeTakeDamage");

                    packet.Write(__instance.transform.position);
                    packet.Write(damage);
                    packet.Write(position);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
            }
            else
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_OnOreNodeTakeDamage");

                        packet.Write(__instance.transform.position);
                        packet.Write(damage);
                        packet.Write(position);

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OreNode), "BreakNode")]
        public static bool PreBreakNode(OreNode __instance, Vector3 position)
        {
            if (!ClientManager.IsHost())
                return false;

            OreNodes.BreakNode(__instance, position);
            return false;
        }
    }
}
