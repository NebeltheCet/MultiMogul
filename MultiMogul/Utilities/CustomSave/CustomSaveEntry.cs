using System;
using UnityEngine;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomSaveEntry
    {
        public SavableObjectID SavableObjectID;
        public SerializableVector3 Position;
        public SerializableVector3 Rotation;

        public string CustomDataJson;

        public string GUID;
    }
}
