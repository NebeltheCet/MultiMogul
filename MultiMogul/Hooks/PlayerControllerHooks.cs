using HarmonyLib;
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
    /*
    [HarmonyPatch]
    public class PlayerControllerHooks
    {
        public static Vector3 lastGrabbedOrePosition = Vector3.zero;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "GrabObject")]
        public static void PostGrabObject(PlayerController __instance, RaycastHit hit)
        {
            lastGrabbedOrePosition = __instance.HeldObject.transform.position;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "Update")]
        public static void PostUpdate(PlayerController __instance)
        {
            FieldInfo _grabJoint = typeof(PlayerController).GetField("_grabJoint", BindingFlags.NonPublic | BindingFlags.Instance);
            SpringJoint grabJoint = (SpringJoint)_grabJoint.GetValue(__instance);

            if (grabJoint != null && grabJoint.connectedBody != null)
            {
                GameObject grabbedObject = grabJoint.connectedBody.gameObject;
                OrePiece orePiece = grabbedObject?.GetComponent<OrePiece>();
                if (orePiece == null)
                    return;

                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_OnOreGrabbed");

                        packet.Write(lastGrabbedOrePosition);
                        packet.Write(__instance.HeldObject.transform.position);

                        lastGrabbedOrePosition = __instance.HeldObject.transform.position;

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_OnOreGrabbed");

                            packet.Write(lastGrabbedOrePosition);
                            packet.Write(__instance.HeldObject.transform.position);

                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }

                    lastGrabbedOrePosition = __instance.HeldObject.transform.position;
                }
            }
        }

        [Networkable("SV_OnOreGrabbed")]
        public static void OnOreGrabbedServer(Connection connection, Packet receivedPacket)
        {
            Vector3 oldPos = receivedPacket.ReadVector3();
            Vector3 newPos = receivedPacket.ReadVector3();

            UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromPosition(oldPos);

            orePiece.transform.position = newPos;

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnOreGrabbed");

                    packet.Write(oldPos);
                    packet.Write(newPos);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnOreGrabbed")]
        public static void OnOreGrabbed(Packet receivedPacket)
        {
            Vector3 oldPos = receivedPacket.ReadVector3();
            Vector3 newPos = receivedPacket.ReadVector3();

            UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromPosition(oldPos);

            orePiece.transform.position = newPos;

            receivedPacket.Dispose();
        }

    }
     */
}
