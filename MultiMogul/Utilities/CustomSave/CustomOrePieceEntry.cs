using System;
using UnityEngine;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomOrePieceEntry
    {
        public ResourceType ResourceType;
        public PieceType PieceType;

        public float PolishedPercent;

        public SerializableVector3 Position;
        public SerializableVector3 Rotation;
        public SerializableVector3 Scale;

        public int MeshID;

        public string GUID;
    }
}
