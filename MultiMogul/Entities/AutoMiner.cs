using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Entities
{
    public class AutoMiner
    {
        // this function will be called by clients to send an update to the server
        public static void SendUpdate(int ID, bool value)
        {
            
        }

        [Networkable("CL_OnMinerToggled")]
        static public void OnMinerToggled(Packet receivedPacket)
        {
            int ID = receivedPacket.ReadInt32();
            bool value = receivedPacket.ReadBool();

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnMinerToggled")]
        static public void OnMinerToggledServer(Connection connection, Packet receivedPacket)
        {
            int ID = receivedPacket.ReadInt32();
            bool value = receivedPacket.ReadBool();

            receivedPacket.Dispose();

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection) continue;
            }
        }
    }
}
