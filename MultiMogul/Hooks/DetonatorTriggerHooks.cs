using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace MultiMogul.MultiMogul.Hooks {
    [HarmonyPatch]
    public class DetonatorTriggerHooks {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DetonatorTrigger), "Interact")]
        public static void PostInteract(DetonatorTrigger __instance, Interaction selectedInteraction) {
            if (!ClientManager.IsHost()) {
                using (Packet packet = new Packet(PacketType.OnRPCMessage)) {
                    packet.Write("SV_OnDetonatorInteract");
                    packet.Write(__instance.DetonatorID);

                    packet.Send(ClientManager.Instance.connection.Connection, Steamworks.Data.SendType.Reliable);
                }
            }
            else {
                using (Packet packet = new Packet(PacketType.OnRPCMessage)) {
                    packet.Write("CL_OnDetonatorInteract");
                    packet.Write(__instance.DetonatorID);

                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients) {
                        packet.Send(kvp.Key, Steamworks.Data.SendType.Reliable);
                    }
                }
            }
        }
    }
}
