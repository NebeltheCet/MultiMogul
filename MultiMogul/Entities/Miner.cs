using MultiMogul.MultiMogul.Hooks;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul.Entities
{
    public class Miner
    {
        public static void SendOreSpawn(Vector3 objectPositionn, float randomValue)
        {
            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnMinerOreSpawn");

                    packet.Write(objectPositionn);
                    packet.Write(randomValue);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }
        }

        // this function will be called by clients to send an update to the server
        public static void SendUpdate(Vector3 objectPosition, bool value)
        {
            if (!ClientManager.IsHost()) {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnMinerToggled");

                    packet.Write(objectPosition);
                    packet.Write(value);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
            }
            else
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_OnMinerToggled");

                        packet.Write(objectPosition);
                        packet.Write(value);

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }
        }

        [Networkable("CL_OnMinerOreSpawn")]
        static public void OnMinerOreSpawn(Packet receivedPacket)
        {
            if (ClientManager.IsHost())
            {
                receivedPacket.Dispose();
                return;
            }

            Vector3 objectPosition = receivedPacket.ReadVector3();
            float randomValue = receivedPacket.ReadSingle();

            GameObject minerObject = NetworkedObjectRegistry.GetFromPosition(objectPosition);
            if (minerObject != null)
            {
                AutoMiner minerComponent = minerObject.GetComponent<AutoMiner>();
                if (minerComponent != null)
                {
                    MinerHooks.TrySpawnOre(minerComponent, randomValue);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnMinerToggled")]
        static public void OnMinerToggledServer(Connection connection, Packet receivedPacket)
        {
            Vector3 objectPosition = receivedPacket.ReadVector3();
            bool value = receivedPacket.ReadBool();

            GameObject minerObject = NetworkedObjectRegistry.GetFromPosition(objectPosition);
            if (minerObject != null)
            {
                AutoMiner minerComponent = minerObject.GetComponent<AutoMiner>();
                if (minerComponent != null)
                {
                    MinerHooks.allowOverride = true;
                    minerComponent.Toggle(value);
                    MinerHooks.allowOverride = false;
                }
            }

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnMinerToggled");

                    packet.Write(objectPosition);
                    packet.Write(value);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnMinerToggled")]
        static public void OnMinerToggled(Packet receivedPacket)
        {
            Vector3 objectPosition = receivedPacket.ReadVector3();
            bool value = receivedPacket.ReadBool();

            GameObject minerObject = NetworkedObjectRegistry.GetFromPosition(objectPosition);
            if (minerObject != null)
            {
                AutoMiner minerComponent = minerObject.GetComponent<AutoMiner>();
                if (minerComponent != null)
                {
                    MinerHooks.allowOverride = true;
                    minerComponent.Toggle(value);
                    MinerHooks.allowOverride = false;
                }
            }

            receivedPacket.Dispose();
        }
    }
}
