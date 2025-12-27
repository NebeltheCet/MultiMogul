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
        public static void SendOreSpawn(int objectId, float randomValue)
        {
            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnMinerOreSpawn");

                    packet.Write(objectId);
                    packet.Write(randomValue);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }
        }

        // this function will be called by clients to send an update to the server
        public static void SendUpdate(int objectId, bool value)
        {
            if (!ClientManager.IsHost()) {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnMinerToggled");

                    packet.Write(objectId);
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

                        packet.Write(objectId);
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

            int objectId = receivedPacket.ReadInt32();
            float randomValue = receivedPacket.ReadSingle();

            GameObject minerObject = NetworkedObjectRegistry.GetFromGUID(objectId);
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
            int objectId = receivedPacket.ReadInt32();
            bool value = receivedPacket.ReadBool();

            GameObject minerObject = NetworkedObjectRegistry.GetFromGUID(objectId);
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

                    packet.Write(objectId);
                    packet.Write(value);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnMinerToggled")]
        static public void OnMinerToggled(Packet receivedPacket)
        {
            int objectId = receivedPacket.ReadInt32();
            bool value = receivedPacket.ReadBool();

            GameObject minerObject = NetworkedObjectRegistry.GetFromGUID(objectId);
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
