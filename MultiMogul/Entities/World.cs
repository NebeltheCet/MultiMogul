using MultiMogul.MultiMogul.Utilities;
using System;
using UnityEngine;

namespace MultiMogul.MultiMogul.Entities
{
    public class World
    {
        [Networkable("CL_OnWorldReceive")]
        public static void OnWorldReceive(Packet receivedPacket)
        {
            Debug.Log("received world data from server");

            SaveManager.LoadGameplaySceneThenLoadSave(receivedPacket.ReadString());
            if (AutoSaveManager.Instance != null)
            {
                AutoSaveManager.Instance.AutoSaveEnabled = false;
            }

            Debug.Log("world data loaded from server");
            receivedPacket.Dispose();
        }
    }
}
