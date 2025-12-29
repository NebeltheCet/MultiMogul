using HarmonyLib;
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
    public class BaseHeldToolHooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseHeldTool), "TryAddToInventory")]
        public static bool PreTryAddToInventory(BaseHeldTool __instance, int slotIndex = -1)
        {
            int GUID = NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance);

            if (UnityEngine.Object.FindObjectOfType<PlayerInventory>().TryAddToInventory(__instance, slotIndex))
            {
                Debug.Log("TryAddToInventory Ran");

                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_ItemRemoved");
                        packet.Write(GUID);
                        packet.Write((int)__instance.SavableObjectID);

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_ItemRemoved");
                            packet.Write(GUID);
                            packet.Write((int)__instance.SavableObjectID);

                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }
                }
            }

            return false;
        }

        [Networkable("SV_ItemRemoved")]
        public static void ItemRemovedServer(Connection connection, Packet receivedPacket)
        {
            int GUIDHash = receivedPacket.ReadInt32();
            GameObject obj = NetworkedObjectRegistry.GetFromGUID(GUIDHash) as GameObject;
            UnityEngine.Object.Destroy(obj);

            Debug.Log("SV_ItemRemoved Ran");

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_ItemRemoved");
                    packet.Write(GUIDHash);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_ItemRemoved")]
        public static void ItemRemoved(Packet receivedPacket)
        {
            int GUIDHash = receivedPacket.ReadInt32();
            GameObject obj = NetworkedObjectRegistry.GetFromGUID(GUIDHash) as GameObject;
            UnityEngine.Object.Destroy(obj);

            Debug.Log("CL_ItemRemoved Ran");

            receivedPacket.Dispose();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseHeldTool), "DropItem")]
        public static bool PreDropItem(BaseHeldTool __instance)
        {
            __instance.gameObject.SetActive(true);
            UnityEngine.Object.FindObjectOfType<PlayerInventory>().RemoveFromInventory(__instance, 1);
            __instance.HideWorldModel(false);
            __instance.HideViewModel(true);
            Rigidbody componentInChildren = __instance.GetComponentInChildren<Rigidbody>();
            if (componentInChildren != null)
            {
                __instance.transform.parent = null;
                Transform transform = UnityEngine.Object.FindObjectOfType<PlayerController>().PlayerCamera.transform;
                componentInChildren.isKinematic = false;
                componentInChildren.transform.position = transform.position + transform.forward * 0.5f;
                componentInChildren.position = transform.position + transform.forward * 0.5f;
                componentInChildren.linearVelocity = transform.forward * 5f;
                componentInChildren.rotation = transform.rotation;
            }
            __instance.Owner = null;

            NetworkedObjectRegistry.Register(__instance);

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_ItemDropped");
                    packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(__instance));
                    packet.Write((int)__instance.SavableObjectID);
                    packet.Write(componentInChildren.transform.position);
                    packet.Write(componentInChildren.rotation.eulerAngles);
                    packet.Write(componentInChildren.linearVelocity);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
            }
            else
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_ItemDropped");
                        packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(__instance));
                        packet.Write((int)__instance.SavableObjectID);
                        packet.Write(componentInChildren.transform.position);
                        packet.Write(componentInChildren.rotation.eulerAngles);
                        packet.Write(componentInChildren.linearVelocity);

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }

            Debug.Log("PreDropItem");

            return false;
        }

        [Networkable("SV_ItemDropped")]
        public static void ItemDroppedServer(Connection connection, Packet receivedPacket)
        {
            string GUID = receivedPacket.ReadString();
            int savedObjectID = receivedPacket.ReadInt32();
            Vector3 position = receivedPacket.ReadVector3();
            Vector3 rotation = receivedPacket.ReadVector3();
            Vector3 linearVelocity = receivedPacket.ReadVector3();

            UnityEngine.Object prefab = SavingLoadingManager.Instance.GetPrefab((SavableObjectID)savedObjectID);

            UnityEngine.Object __instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            NetworkedObjectRegistry.Register(__instance, GUID);
            Rigidbody componentInChildren = __instance.GetComponentInChildren<Rigidbody>();
            if (componentInChildren != null)
            {
                componentInChildren.rotation = Quaternion.Euler(rotation);
                componentInChildren.linearVelocity = linearVelocity;
            }

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_ItemDropped");
                    packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(__instance));
                    packet.Write(savedObjectID);
                    packet.Write(position);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_ItemDropped")]
        public static void ItemDropped(Packet receivedPacket)
        {
            string GUID = receivedPacket.ReadString();
            int savedObjectID = receivedPacket.ReadInt32();
            Vector3 position = receivedPacket.ReadVector3();
            Vector3 rotation = receivedPacket.ReadVector3();
            Vector3 linearVelocity = receivedPacket.ReadVector3();

            UnityEngine.Object prefab = SavingLoadingManager.Instance.GetPrefab((SavableObjectID)savedObjectID);

            UnityEngine.Object __instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            NetworkedObjectRegistry.Register(__instance, GUID);
            Rigidbody componentInChildren = __instance.GetComponentInChildren<Rigidbody>();
            if (componentInChildren != null)
            {
                componentInChildren.rotation = Quaternion.Euler(rotation);
                componentInChildren.linearVelocity = linearVelocity;
            }

            receivedPacket.Dispose();
        }
    }
}
