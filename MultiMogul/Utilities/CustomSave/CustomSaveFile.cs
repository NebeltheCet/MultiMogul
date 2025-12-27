using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomSaveFile
    {
        public int SaveVersion;

        public string GameVersion = "Unknown";
        public string SaveFileName = "Unnamed Save File";
        public string SaveTimestamp = "Unknown Time";

        public float Money;
        public SerializableVector3 PlayerPosition = Vector3.zero;
        public SerializableVector3 PlayerRotation = Vector3.zero;

        public List<CustomSaveEntry> Entries = new List<CustomSaveEntry>();
        public List<CustomStaticBreakableEntry> DestroyedStaticBreakablePositions = new List<CustomStaticBreakableEntry>();
        public List<CustomStaticBreakableEntry> ExistingStaticBreakablePositions = new List<CustomStaticBreakableEntry>();

        public List<QuestID> CompletedQuestsIDs = new List<QuestID>();
        [Obsolete]
        public List<QuestID> ActiveQuestsIDs = new List<QuestID>();
        public List<ActiveQuestEntry> ActiveQuests = new List<ActiveQuestEntry>(); // this doesn't need to be custom just yet

        public List<CustomOrePieceEntry> OrePieces = new List<CustomOrePieceEntry>();

        public List<CustomWorldEventEntry> WorldEventEntries = new List<CustomWorldEventEntry>();

        public ShopPurchases ShopPurchases = new ShopPurchases(); // this doesn't need to be custom just yet
    }
}
