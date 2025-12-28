using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Entities
{
    public class Grabbables
    {
        private static float lastSentTime = 0f;

        private const int updateRate = 30;
        private const float intervalPerTick = (1f / updateRate);

        public static void UpdateSentTime()
        {
            lastSentTime = Time.realtimeSinceStartup;
        }

        public static void SendGrabbableUpdate(List<GameObject> grabbedObjects, bool updateTime = true)
        {
            if (grabbedObjects.Count == 0)
                return;

            if ((Time.realtimeSinceStartup - lastSentTime) < intervalPerTick)
                return;

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnOreGrabbed");

                    packet.Write(grabbedObjects.Count);
                    foreach (GameObject grabbedObject in grabbedObjects)
                    {
                        packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(grabbedObject));
                        packet.Write(grabbedObject.transform.position);
                        packet.Write(grabbedObject.GetComponent<Rigidbody>().linearVelocity);
                    }

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }

                //Debug.Log($"sent \"CL_OnOreGrabbed\" to the server[{NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject)}]");
            }
            else
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnOreGrabbed");

                    packet.Write(grabbedObjects.Count);
                    foreach (GameObject grabbedObject in grabbedObjects)
                    {
                        packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(grabbedObject));
                        packet.Write(grabbedObject.transform.position);
                        packet.Write(grabbedObject.GetComponent<Rigidbody>().linearVelocity);
                    }

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }

                //Debug.Log($"sent \"CL_OnOreGrabbed\" to all clients[{NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject)}]");
            }

            if (updateTime)
            {
                UpdateSentTime();
            }
        }

        public static void SendGrabbableUpdate(GameObject grabbedObject, bool updateTime = true)
        {
            if (grabbedObject == null)
                return;

            if ((Time.realtimeSinceStartup - lastSentTime) < intervalPerTick)
                return;

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnOreGrabbed");

                    packet.Write(-1);
                    packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(grabbedObject));
                    packet.Write(grabbedObject.transform.position);
                    packet.Write(grabbedObject.GetComponent<Rigidbody>().linearVelocity);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }

                //Debug.Log($"sent \"CL_OnOreGrabbed\" to the server[{NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject)}]");
            }
            else
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnOreGrabbed");

                    packet.Write(-1);
                    packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(grabbedObject));
                    packet.Write(grabbedObject.transform.position);
                    packet.Write(grabbedObject.GetComponent<Rigidbody>().linearVelocity);

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }

                //Debug.Log($"sent \"CL_OnOreGrabbed\" to all clients[{NetworkedObjectRegistry.GetGUIDHashFromInstance(__instance.HeldObject)}]");
            }

            if (updateTime)
            {
                UpdateSentTime();
            }
        }
    }
}
