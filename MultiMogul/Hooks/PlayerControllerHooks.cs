using HarmonyLib;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class PlayerControllerHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "Update")]
        public static void PostUpdate(PlayerController __instance)
        {
            FieldInfo _grabJoint = typeof(PlayerController).GetField("_grabJoint", BindingFlags.NonPublic | BindingFlags.Instance);
            SpringJoint grabJoint = (SpringJoint)_grabJoint.GetValue(__instance);

            if (grabJoint != null && grabJoint.connectedBody != null)
            {
                GameObject grabbedObject = __instance.HeldObject;

                Grabbables.SendGrabbableUpdate(grabbedObject);
            }
        }

        [Networkable("SV_OnOreGrabbed")]
        public static void OnOreGrabbedServer(Connection connection, Packet receivedPacket)
        {
            int listSize = receivedPacket.ReadInt32();
            bool isList = listSize != -1;

            if (!isList)
            {
                int objectId = receivedPacket.ReadInt32();
                Vector3 newPosition = receivedPacket.ReadVector3();
                Vector3 newVelocity = receivedPacket.ReadVector3();

                UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromGUID(objectId);
                if (orePiece != null)
                {
                    orePiece.transform.position = newPosition;
                    orePiece.GetComponent<Rigidbody>().linearVelocity = newVelocity;
                }

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnOreGrabbed");

                    packet.Write(objectId);
                    packet.Write(newPosition);
                    packet.Write(newVelocity);

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        if (kvp.Key == connection)
                            continue;

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }
            else
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnOreGrabbed");

                    packet.Write(listSize);
                    for (int index = 0; index < listSize; index++)
                    {
                        int objectId = receivedPacket.ReadInt32();
                        Vector3 newPosition = receivedPacket.ReadVector3();
                        Vector3 newVelocity = receivedPacket.ReadVector3();

                        UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromGUID(objectId);
                        if (orePiece != null)
                        {
                            orePiece.transform.position = newPosition;
                            orePiece.GetComponent<Rigidbody>().linearVelocity = newVelocity;
                        }

                        packet.Write(objectId);
                        packet.Write(newPosition);
                        packet.Write(newVelocity);
                    }

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        if (kvp.Key == connection)
                            continue;
                        
                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }

            //Debug.Log($"sent \"CL_OnOreGrabbed\" to all clients[{objectId}]");

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnOreGrabbed")]
        public static void OnOreGrabbed(Packet receivedPacket)
        {
            int listSize = receivedPacket.ReadInt32();
            bool isList = listSize != -1;

            //Debug.Log($"received \"CL_OnOreGrabbed\" from server[{objectId}]");

            if (!isList)
            {
                int objectId = receivedPacket.ReadInt32();
                Vector3 newPosition = receivedPacket.ReadVector3();
                Vector3 newVelocity = receivedPacket.ReadVector3();

                UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromGUID(objectId);
                orePiece.transform.position = newPosition;
                orePiece.GetComponent<Rigidbody>().linearVelocity = newVelocity;
            }
            else
            {
                for (int index = 0; index < listSize; index++)
                {
                    int objectId = receivedPacket.ReadInt32();
                    Vector3 newPosition = receivedPacket.ReadVector3();
                    Vector3 newVelocity = receivedPacket.ReadVector3();

                    UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromGUID(objectId);
                    orePiece.transform.position = newPosition;
                    orePiece.GetComponent<Rigidbody>().linearVelocity = newVelocity;
                }
            }

            receivedPacket.Dispose();
        }

        public static int lastInteractedObjectHash = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerController), "TryInteract")]
        public static bool PreTryInteract(PlayerController __instance)
        {
            FieldInfo _grabJoint = typeof(PlayerController).GetField("_grabJoint", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _interactRange = typeof(PlayerController).GetField("_interactRange", BindingFlags.NonPublic | BindingFlags.Instance);

            if (Singleton<UIManager>.Instance.IsInAnyMenu() || __instance.HeldObject != null || _grabJoint.GetValue(__instance) != null)
            {
                return false;
            }
            RaycastHit raycastHit;
            if (Physics.Raycast(__instance.PlayerCamera.transform.position, __instance.PlayerCamera.transform.forward, out raycastHit, (float)_interactRange.GetValue(__instance), __instance.InteractLayerMask))
            {
                lastInteractedObjectHash = NetworkedObjectRegistry.GetGUIDHashFromInstance(raycastHit.collider.transform.root.gameObject);
                if (lastInteractedObjectHash == -1)
                    return true;

                __instance.InteractionWheelUI.ClearInteractionWheel();
                List<IInteractable> list = new List<IInteractable>();
                list.AddRange(raycastHit.collider.GetComponentsInParent<IInteractable>());
                if (list.Count == 1 && !list[0].ShouldUseInteractionWheel())
                {
                    list[0].Interact(list[0].GetInteractions().FirstOrDefault<Interaction>());
                    return false;
                }
                if (list.Count > 0)
                {
                    __instance.InteractionWheelUI.gameObject.SetActive(true);
                    foreach (IInteractable interactable in list)
                    {

                        __instance.InteractionWheelUI.PopulateInteractionWheel(interactable);
                    }
                }
            }

            return false;
        }
    }
}
