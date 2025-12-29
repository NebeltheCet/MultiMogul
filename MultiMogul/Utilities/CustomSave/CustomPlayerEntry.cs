using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomPlayerEntry
    {
        public SerializableVector3 Position = Vector3.zero;
        public SerializableVector3 Rotation = Vector3.zero;

        public List<CustomInventoryEntry> InventoryEntries = new List<CustomInventoryEntry>();

        public ulong SteamID = 0;
    }
}
