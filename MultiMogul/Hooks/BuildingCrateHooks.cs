using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
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
    public class BuildingCrateHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingCrate), "TryAddToInventory")]
        public static bool PreTryAddToInventory(BuildingCrate __instance)
        {
            if (__instance.Definition == null)
            {
                Debug.LogWarning("Tried to pickup Crate with missing Building Definition!");
                return false;
            }
            ToolBuilder toolBuilder = UnityEngine.Object.Instantiate<ToolBuilder>(Singleton<BuildingManager>.Instance.BuildingToolPrefab);
            toolBuilder.Definition = __instance.Definition;
            toolBuilder.Quantity = __instance.Quantity;
            toolBuilder.Setup();
            if (UnityEngine.Object.FindObjectOfType<PlayerInventory>().TryAddToInventory(toolBuilder, -1))
            {
                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_DeleteCrate");
                        packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance));

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_DeleteCrate");
                            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance));
 
                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }
                }

                UnityEngine.Object.Destroy(__instance.gameObject);
            }

            return false;
        }

        [Networkable("SV_DeleteCrate")]
        public static void DeleteCrateServer(Connection connection, Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();
            BuildingCrate __instance = NetworkedObjectRegistry.GetFromGUID(guidHash).GetComponent<BuildingCrate>();

            if (PlayerControllerHooks.lastInteractedObjectHash == guidHash)
            {
                Player.GetLocalPlayer()?.playerObject?.GetComponent<PlayerController>()?.InteractionWheelUI?.CloseWheel();
            }

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_DeleteCrate");
                    packet.Write(guidHash);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            UnityEngine.Object.Destroy(__instance.gameObject, 0);

            receivedPacket.Dispose();
        }

        [Networkable("CL_DeleteCrate")]
        public static void DeleteCrate(Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();
            BuildingCrate __instance = NetworkedObjectRegistry.GetFromGUID(guidHash).GetComponent<BuildingCrate>();

            if (PlayerControllerHooks.lastInteractedObjectHash == guidHash)
            {
                Player.GetLocalPlayer()?.playerObject?.GetComponent<PlayerController>()?.InteractionWheelUI?.CloseWheel();
            }

            UnityEngine.Object.Destroy(__instance.gameObject, 0);

            receivedPacket.Dispose();
        }
    }
}
