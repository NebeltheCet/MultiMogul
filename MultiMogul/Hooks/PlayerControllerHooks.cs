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
                GameObject grabbedObject = grabJoint.connectedBody.gameObject;
                OrePiece orePiece = grabbedObject?.GetComponent<OrePiece>();
                if (orePiece == null)
                    return;

                if (!ClientManager.IsHost())
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("SV_OnOreGrabbed");

                        packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject));
                        packet.Write(__instance.HeldObject.transform.position);
                        packet.Write(__instance.HeldObject.GetComponent<Rigidbody>().linearVelocity);

                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }

                    //Debug.Log($"sent \"CL_OnOreGrabbed\" to the server[{NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject)}]");
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        using (Packet packet = new Packet(PacketType.OnRPCMessage))
                        {
                            packet.Write("CL_OnOreGrabbed");

                            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject));
                            packet.Write(__instance.HeldObject.transform.position);
                            packet.Write(__instance.HeldObject.GetComponent<Rigidbody>().linearVelocity);

                            packet.Send(kvp.Key, SendType.Reliable);
                        }
                    }

                    //Debug.Log($"sent \"CL_OnOreGrabbed\" to all clients[{NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject)}]");
                }
            }
        }

        [Networkable("SV_OnOreGrabbed")]
        public static void OnOreGrabbedServer(Connection connection, Packet receivedPacket)
        {
            int objectId = receivedPacket.ReadInt32();
            Vector3 newPosition = receivedPacket.ReadVector3();
            Vector3 newVelocity = receivedPacket.ReadVector3();

            UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromGUID(objectId);
            orePiece.transform.position = newPosition;
            orePiece.GetComponent<Rigidbody>().linearVelocity = newVelocity;

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnOreGrabbed");

                    packet.Write(objectId);
                    packet.Write(newPosition);
                    packet.Write(newVelocity);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            //Debug.Log($"sent \"CL_OnOreGrabbed\" to all clients[{objectId}]");

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnOreGrabbed")]
        public static void OnOreGrabbed(Packet receivedPacket)
        {
            int objectId = receivedPacket.ReadInt32();
            Vector3 newPosition = receivedPacket.ReadVector3();
            Vector3 newVelocity = receivedPacket.ReadVector3();

            //Debug.Log($"received \"CL_OnOreGrabbed\" from server[{objectId}]");

            UnityEngine.GameObject orePiece = NetworkedObjectRegistry.GetFromGUID(objectId);
            orePiece.transform.position = newPosition;
            orePiece.GetComponent<Rigidbody>().linearVelocity = newVelocity;

            receivedPacket.Dispose();
        }

    }
}
