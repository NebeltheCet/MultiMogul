using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;

namespace MultiMogul.MultiMogul.Utilities.CustomSave
{
    [Serializable]
    public class CustomInventoryEntry
    {
        public SavableObjectID SavableObjectID;
        public int InventorySlotIndex = -1;

        public int Quantity = 1;
        public SavableObjectID BuildObjectID;
    }
}
