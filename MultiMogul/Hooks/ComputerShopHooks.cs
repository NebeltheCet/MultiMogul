using HarmonyLib;
using MultiMogul.MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing;

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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComputerShopUI), "PurchaseCart")]
        public static void PrePurchaseCart(ComputerShopUI __instance, out float __state)
        {
            __state = Singleton<EconomyManager>.Instance.Money;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComputerShopUI), "PurchaseCart")]
        public static void PostPurchaseCart(ComputerShopUI __instance, float __state)
        {
            if (allowOverride)
                return;

            if (!ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_ShopOnPurchase");
                    packet.Write(__state);

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
            }
            else
            {
                foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                {
                    using (Packet packet = new Packet(PacketType.OnRPCMessage))
                    {
                        packet.Write("CL_ShopOnPurchase");
                        packet.Write(__state);

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                } 
            }
        }

        public static bool shouldFix = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ComputerShopUI), "AddToCart")]
        public static void PreAddToCart(ComputerShopUI __instance, ShopItem item, int quantity)
        {
            shouldFix = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComputerShopUI), "AddToCart")]
        public static void PostAddToCart(ComputerShopUI __instance, ShopItem item, int quantity)
        {
            shouldFix = false;

            if (allowOverride)
                return;

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

                        Debug.Log($"Sent add to cart network message for item ID {(int)item.GetSavableObjectID()} x{quantity} to client.");
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShopCartItemButton), "ChangeQuantity")]
        static public void PostChangeQuantity(ShopCartItemButton __instance, int quantity)
        {
            if (allowOverride || shouldFix)
                return;

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
