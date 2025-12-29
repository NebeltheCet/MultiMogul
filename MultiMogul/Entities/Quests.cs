using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Entities
{
    public class Quests
    {
        [Networkable("SV_OnActivateQuestTrigger")]
        public static void OnActivateQuestTriggerServer(Connection connection, Packet receivedPacket)
        {
            TriggeredQuestRequirementType type = (TriggeredQuestRequirementType)receivedPacket.ReadInt32();
            int amount = receivedPacket.ReadInt32();

            QuestManager.Instance.ActivateQuestTrigger(type, amount);

            receivedPacket.Dispose();
        }
    }
}
