using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;
using UnityEngine;

namespace MultiMogul.MultiMogul.Entities {
    public class WorldEvents {

        [Networkable("CL_OnDetonatorInteract")]
        public static void OnDetonatorInteract(Packet receivedPacket) {
            int detonatorId = receivedPacket.ReadInt32();

            DetonatorTrigger[] detonatorTriggers = UnityEngine.Object.FindObjectsByType<DetonatorTrigger>(FindObjectsSortMode.InstanceID);
            if (detonatorTriggers != null && detonatorTriggers.Length > 0) {
                foreach (DetonatorTrigger detonatorTrigger in detonatorTriggers) {
                    if (detonatorTrigger.DetonatorID != detonatorId)
                        continue;

                    detonatorTrigger.Interact(null); // detonators dont have interactions, they just trigger the fuse.
                    break;
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnDetonatorInteract")]
        public static void OnDetonatorInteractServer(Connection connection, Packet receivedPacket) {
            int detonatorId = receivedPacket.ReadInt32();

            DetonatorTrigger[] detonatorTriggers = UnityEngine.Object.FindObjectsByType<DetonatorTrigger>(FindObjectsSortMode.InstanceID);
            if (detonatorTriggers != null && detonatorTriggers.Length > 0) {
                foreach (DetonatorTrigger detonatorTrigger in detonatorTriggers) {
                    if (detonatorTrigger.DetonatorID != detonatorId)
                        continue;

                    detonatorTrigger.Interact(null); // detonators dont have interactions, they just trigger the fuse.
                    break;
                }
            }

            using (Packet packet = new Packet(PacketType.OnRPCMessage)) {
                packet.Write("CL_OnDetonatorInteract");
                packet.Write(detonatorId);

                foreach (var kvp in MultiMogulBase.serverManager.connectedClients) {
                    if (kvp.Key == connection)
                        continue;

                    packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }
    }
}
