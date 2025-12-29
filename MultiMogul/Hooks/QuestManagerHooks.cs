using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class QuestManagerHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(QuestManager), "ActivateQuestTrigger")]
        public static void PostActivateQuestTrigger(QuestManager __instance, TriggeredQuestRequirementType type, int amount)
        {
            if (ClientManager.IsHost())
                return;

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("SV_OnActivateQuestTrigger");

                packet.Write((int)type);
                packet.Write(amount);

                packet.Send(ClientManager.Instance.connection.Connection, Steamworks.Data.SendType.Reliable);
            }
        }
    }
}
