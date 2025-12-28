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
    public class Building
    {
        public static void Pack(BuildingObject _instance, Connection? connection = null, Packet receivedPacket = null)
        {
            if (_instance.IsGhost)
                return;

            Packet packet = new Packet(PacketType.OnRPCMessage);
            if (ClientManager.IsHost())
            {
                packet.Write("CL_CrateObject");
            }
            else
            {
                packet.Write("SV_CrateObject");
            }

            packet.Write(NetworkedObjectRegistry.GetGUIDHashFromInstance(_instance));

            Vector3 vector = (_instance.BuildingCrateSpawnPoint ? _instance.BuildingCrateSpawnPoint.position : (_instance.transform.position + new Vector3(0f, 0.25f, 0f)));
            Quaternion quaternion = (_instance.BuildingCrateSpawnPoint ? _instance.BuildingCrateSpawnPoint.rotation : Quaternion.identity);
            BuildingCrate buildingCrate = UnityEngine.Object.Instantiate<BuildingCrate>(_instance.Definition.PackedPrefab ? _instance.Definition.PackedPrefab : Singleton<BuildingManager>.Instance.BuildingCratePrefab, vector, quaternion);
            buildingCrate.Definition = _instance.Definition;

            if (receivedPacket == null)
            {
                NetworkedObjectRegistry.Register(buildingCrate);
                packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(buildingCrate));
            }
            else
            {
                NetworkedObjectRegistry.Register(buildingCrate, receivedPacket.ReadString());
                packet.Write(NetworkedObjectRegistry.GetGUIDFromInstance(buildingCrate));
            }

            Rigidbody component = buildingCrate.GetComponent<Rigidbody>();
            if (component != null)
            {
                if (receivedPacket == null)
                {
                    float linearRange = 0.5f;
                    Vector3 newLinearVelocity = new Vector3(UnityEngine.Random.Range(-linearRange, linearRange), UnityEngine.Random.Range(0f, linearRange) * 2f, UnityEngine.Random.Range(-linearRange, linearRange));
                    component.linearVelocity = newLinearVelocity;
                    packet.Write(component.linearVelocity);

                    float angularRange = 1f;
                    Vector3 newAngularVelocity = new Vector3(UnityEngine.Random.Range(-angularRange, angularRange), UnityEngine.Random.Range(-angularRange, angularRange), UnityEngine.Random.Range(-angularRange, angularRange));
                    component.angularVelocity = newAngularVelocity;
                    packet.Write(component.angularVelocity);
                }
                else
                {
                    component.linearVelocity = receivedPacket.ReadVector3();
                    component.angularVelocity = receivedPacket.ReadVector3();

                    packet.Write(component.linearVelocity);
                    packet.Write(component.angularVelocity);
                }
            }

            if (receivedPacket == null || ClientManager.IsHost())
            {
                if (ClientManager.IsHost())
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        if (connection.HasValue && kvp.Key == connection.Value)
                            continue;

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
                else
                {
                    if (!connection.HasValue)
                    {
                        throw new Exception("Pack was executed without connection for client.");
                    }

                    packet.Send(connection.Value, SendType.Reliable);
                }
            }

            UnityEngine.Object.Destroy(_instance.gameObject, 0f);
            packet.Dispose();
        }
    }
}
