using System;
using UnityEngine;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class SerializableVector3
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;

        public SerializableVector3() { }

        public SerializableVector3(Vector3 other) {
            this.x = other.x;
            this.y = other.y;
            this.z = other.z;
        }

        public static implicit operator SerializableVector3(Vector3 other)
        {
            return new SerializableVector3 {
                x = other.x,
                y = other.y,
                z = other.z
            };
        }

        public Vector3 ToVector3() {
            return new Vector3(this.x, this.y, this.z);
        }
    }
}
