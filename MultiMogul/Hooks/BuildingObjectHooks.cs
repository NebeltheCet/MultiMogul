using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class BuildingObjectHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingObject), "Pack")]
        public static bool PrePack(BuildingObject __instance)
        {
            if (!ClientManager.IsHost())
            {
                Building.Pack(__instance, ClientManager.Instance.connection.Connection);
            }
            else
            {
                Building.Pack(__instance);
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildingObject), "TryAddToInventory")]
        public static bool PreTryAddToInventory(BuildingObject __instance, ref bool __result)
        {
            if (__instance.Definition == null)
            {
                Debug.LogWarning("Tried to pickup Crate with missing Building Definition!");
                __result = false;
                return false;
            }

            ToolBuilder toolBuilder = UnityEngine.Object.Instantiate<ToolBuilder>(Singleton<BuildingManager>.Instance.BuildingToolPrefab);
            toolBuilder.Definition = __instance.Definition;
            toolBuilder.Setup();

            if (UnityEngine.Object.FindObjectOfType<PlayerInventory>().TryAddToInventory(toolBuilder, -1))
            {
                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_DestroyObject");
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
                            packet.Write("CL_DestroyObject");
                            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance));

                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }
                }

                // network destroy
                UnityEngine.Object.Destroy(__instance.gameObject, 0f);
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }

        [Networkable("SV_CrateObject")]
        public static void CrateObjectServer(Connection connection, Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();

            GameObject gameObject = NetworkedObjectRegistry.GetFromGUID(guidHash);
            BuildingObject _instance = gameObject.GetComponent<BuildingObject>();

            Building.Pack(_instance, connection, receivedPacket);
            if (PlayerControllerHooks.lastInteractedObjectHash == guidHash)
            {
                Player.GetLocalPlayer()?.playerObject?.GetComponent<PlayerController>()?.InteractionWheelUI?.CloseWheel();
            }

            UnityEngine.Object.Destroy(gameObject, 0f);

            receivedPacket.Dispose();
        }

        [Networkable("CL_CrateObject")]
        public static void CrateObject(Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();
            GameObject gameObject = NetworkedObjectRegistry.GetFromGUID(guidHash);
            BuildingObject _instance = gameObject.GetComponent<BuildingObject>();

            Building.Pack(_instance, ClientManager.Instance.connection.Connection, receivedPacket);

            if (PlayerControllerHooks.lastInteractedObjectHash == guidHash)
            {
                Player.GetLocalPlayer()?.playerObject?.GetComponent<PlayerController>()?.InteractionWheelUI?.CloseWheel();
            }

            UnityEngine.Object.Destroy(gameObject, 0f);
            receivedPacket.Dispose();
        }


        [Networkable("SV_DestroyObject")]
        public static void DestroyObjectServer(Connection connection, Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();

            GameObject gameObject = NetworkedObjectRegistry.GetFromGUID(guidHash);
            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_DestroyObject");
                    packet.Write(guidHash);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            if (PlayerControllerHooks.lastInteractedObjectHash == guidHash)
            {
                Player.GetLocalPlayer()?.playerObject?.GetComponent<PlayerController>()?.InteractionWheelUI?.CloseWheel();
            }

            UnityEngine.Object.Destroy(gameObject, 0f);

            receivedPacket.Dispose();
        }

        [Networkable("CL_DestroyObject")]
        public static void DestroyObject(Packet receivedPacket)
        {
            int guidHash = receivedPacket.ReadInt32();

            GameObject gameObject = NetworkedObjectRegistry.GetFromGUID(guidHash);

            if (PlayerControllerHooks.lastInteractedObjectHash == guidHash)
            {
                Player.GetLocalPlayer()?.playerObject?.GetComponent<PlayerController>()?.InteractionWheelUI?.CloseWheel();
            }

            UnityEngine.Object.Destroy(gameObject, 0f);

            receivedPacket.Dispose();
        }
    }
}
