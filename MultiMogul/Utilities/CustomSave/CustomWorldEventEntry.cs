using System;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomWorldEventEntry
    {
        public SavableWorldEventType SavableWorldEventType;
        public int WorldEventID;

        public string CustomDataJson;
    }
}
