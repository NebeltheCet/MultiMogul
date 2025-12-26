using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using Steamworks.Data;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiMogul.MultiMogul.Hooks
{
    [HarmonyPatch]
    public class ComputerShopHooks
    {
        public static bool allowOverride = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComputerShopUI), "Start")]
        public static bool PreStart(ComputerShopUI __instance)
        {
            return false; // prevent it from clearing the cart on start
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComputerShopUI), "AddToCart")]
        public static void PostAddToCart(ComputerShopUI __instance, ShopItem item, int quantity)
        {
            if (allowOverride)
            {
                Debug.LogWarning($"called PostAddToCart without sending data");
                return;
            }

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_ShopOnAddToCart");

                    packet.Write((int)item.GetSavableObjectID());
                    packet.Write(quantity);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
            }
            else
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_ShopOnAddToCart");

                        packet.Write((int)item.GetSavableObjectID());
                        packet.Write(quantity);

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShopCartItemButton), "ChangeQuantity")]
        static public void PostChangeQuantity(ShopCartItemButton __instance, int quantity)
        {
            if (allowOverride)
            {
                Debug.LogWarning($"called PostAddToCart without sending data");
                return;
            }

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_ShopOnChangeQuantity");

                    packet.Write((int)__instance.ShopItem.GetSavableObjectID());
                    packet.Write(quantity);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
            }
            else
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_ShopOnChangeQuantity");

                        packet.Write((int)__instance.ShopItem.GetSavableObjectID());
                        packet.Write(quantity);

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }
        }
    }
}
