using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class BuildingManagerHooks
    {
        public static float lastSentUpdateTime = 0f;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildingManager), "CleanUpGhostObject")]
        public static void PostCleanUpGhostObject(BuildingManager __instance)
        {
            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnCleanUpGhostObject");
                    packet.Write(SteamClient.SteamId.Value);

                    packet.Send(ClientManager.Instance.connection.Connection, Steamworks.Data.SendType.Reliable);
                }
            }
            else
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnCleanUpGhostObject");
                    packet.Write(SteamClient.SteamId.Value);

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildingManager), "SetupGhostObject")]
        public static void PostSetupGhostObject(BuildingManager __instance, Vector3Int position, BuildingObject prefab, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[PostSetupGhostObject] - prefab was null!");
                return;
            }

            if ((Time.realtimeSinceStartup - BuildingManagerHooks.lastSentUpdateTime) <= 0.1f)
                return;

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnSetupGhostObject");
                    packet.Write(SteamClient.SteamId.Value);

                    packet.Write(position);
                    packet.Write((int)prefab.GetSavableObjectID());
                    packet.Write(rotation);

                    packet.Send(ClientManager.Instance.connection.Connection, Steamworks.Data.SendType.Reliable);
                }
            }
            else
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnSetupGhostObject");
                    packet.Write(SteamClient.SteamId.Value);

                    packet.Write(position);
                    packet.Write((int)prefab.GetSavableObjectID());
                    packet.Write(rotation);

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingManager), "UpdateGhostObject")]
        public static bool PreUpdateGhostObject(BuildingManager __instance, Vector3Int position, BuildingObject prefab, Quaternion rotation, ToolBuilder activeTool)
        {
            Building.UpdateGhostObject(__instance, position, prefab, rotation, activeTool);
            return false;
        }
    }
}
