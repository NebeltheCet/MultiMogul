using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomPlayerEntry
    {
        public SerializableVector3 Position;
        public SerializableVector3 Rotation;

        public ulong SteamID;
    }
}
