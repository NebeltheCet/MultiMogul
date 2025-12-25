using MultiMogul.MultiMogul.Hooks;
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
    public class Miner
    {
        // this function will be called by clients to send an update to the server
        public static void SendUpdate(Vector3 objectPosition, bool value)
        {
            if (ServerManager.Instance?.currentLobby.Id == 0 && ClientManager.Instance != null && ClientManager.Instance.connection != null) {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnMinerToggled");

                    packet.Write(objectPosition);
                    packet.Write(value);

                    if (ServerManager.Instance?.currentLobby.Id == 0 && ClientManager.Instance != null && ClientManager.Instance.connection != null)
                    {
                        packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                    }
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
    }
}
