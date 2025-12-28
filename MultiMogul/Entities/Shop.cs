using MultiMogul.MultiMogul.Hooks;
using Steamworks.Data;
using Steamworks.Ugc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing;

namespace MultiMogul.MultiMogul.Entities
{
    public class Shop
    {
        public static ShopItem GetItemFromId(int itemId)
        {
            FieldInfo _selectedShopCategory = typeof(ComputerShopUI).GetField("_selectedShopCategory", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _categoryButtons = typeof(ComputerShopUI).GetField("_categoryButtons", BindingFlags.NonPublic | BindingFlags.Instance);

            HashSet<ShopCategoryButton> categoryButtons = _categoryButtons.GetValue(UIManager.Instance?.ComputerShopUI) as HashSet<ShopCategoryButton>;
            foreach (ShopCategoryButton shopCategoryButton in categoryButtons)
            {
                foreach (ShopItem shopItem in shopCategoryButton.ShopCategory.ShopItems.OrderByDescending((ShopItem item) => !item.IsLocked).ToList<ShopItem>())
                {
                    int currentId = (int)shopItem.GetSavableObjectID();
                    if (currentId != itemId)
                        continue;

                    Debug.LogWarning($"found Shop item with ID {itemId}.");
                    return shopItem;
                }
            }

            Debug.LogWarning($"Shop item with ID {itemId} not found.");
            return null;
        }

        [Networkable("SV_ShopOnAddToCart")]
        public static void ShopOnAddToCartServer(Connection connection, Packet receivedPacket)
        {
            SetUpShop();

            int itemId = receivedPacket.ReadInt32();
            int quantity = receivedPacket.ReadInt32();

            ShopItem item = GetItemFromId(itemId);
            if (item != null)
            {
                ComputerShopHooks.allowOverride = true;
                UIManager.Instance?.ComputerShopUI?.AddToCart(item, quantity);
                ComputerShopHooks.allowOverride = false;
            }

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_ShopOnAddToCart");

                    packet.Write(itemId);
                    packet.Write(quantity);

                    packet.Send(kvp.Key, SendType.Reliable);

                    Debug.Log($"Sent add to cart network message for item ID {itemId} x{quantity} to client.");
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_ShopOnAddToCart")]
        public static void ShopOnAddToCart(Packet receivedPacket)
        {
            SetUpShop();

            int itemId = receivedPacket.ReadInt32();
            int quantity = receivedPacket.ReadInt32();

            ShopItem item = GetItemFromId(itemId);
            if (item != null)
            {
                ComputerShopHooks.allowOverride = true;
                UIManager.Instance?.ComputerShopUI?.AddToCart(item, quantity);
                ComputerShopHooks.allowOverride = false;
                
                Debug.Log($"Added item {item.GetName()} (ID: {itemId}) x{quantity} to cart via network message.");
            }

            receivedPacket.Dispose();
        }

        [Networkable("SV_ShopOnChangeQuantity")]
        public static void ShopOnChangeQuantityServer(Connection connection, Packet receivedPacket)
        {
            SetUpShop();

            int itemId = receivedPacket.ReadInt32();
            int newQuantity = receivedPacket.ReadInt32();

            FieldInfo _cartItems = typeof(ComputerShopUI).GetField("_cartItems", BindingFlags.NonPublic | BindingFlags.Instance);

            HashSet<ShopCartItemButton> cartItems = _cartItems.GetValue(UIManager.Instance?.ComputerShopUI) as HashSet<ShopCartItemButton>;

            ShopItem item = GetItemFromId(itemId);
            if (item != null)
            {
                ShopCartItemButton shopCartItemButton = cartItems.FirstOrDefault((ShopCartItemButton ci) => ci.ShopItem == item);
                if (shopCartItemButton != null)
                {
                    ComputerShopHooks.allowOverride = true;
                    shopCartItemButton.ChangeQuantity(newQuantity);
                    ComputerShopHooks.allowOverride = false;
                }
            }

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_ShopOnChangeQuantity");

                    packet.Write(itemId);
                    packet.Write(newQuantity);

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_ShopOnChangeQuantity")]
        public static void ShopOnChangeQuantity(Packet receivedPacket)
        {
            SetUpShop();

            int itemId = receivedPacket.ReadInt32();
            int newQuantity = receivedPacket.ReadInt32();

            FieldInfo _cartItems = typeof(ComputerShopUI).GetField("_cartItems", BindingFlags.NonPublic | BindingFlags.Instance);

            HashSet<ShopCartItemButton> cartItems = _cartItems.GetValue(UIManager.Instance?.ComputerShopUI) as HashSet<ShopCartItemButton>;

            ShopItem item = GetItemFromId(itemId);
            if (item != null)
            {
                ShopCartItemButton shopCartItemButton = cartItems.FirstOrDefault((ShopCartItemButton ci) => ci.ShopItem == item);
                if (shopCartItemButton != null)
                {
                    ComputerShopHooks.allowOverride = true;
                    shopCartItemButton.ChangeQuantity(newQuantity);
                    ComputerShopHooks.allowOverride = false;
                }
            }
        }

        [Networkable("SV_ShopOnPurchase")]
        public static void ShopOnPurchaseServer(Connection connection, Packet receivedPacket)
        {
            SetUpShop();

            float money = receivedPacket.ReadSingle();

            Singleton<EconomyManager>.Instance.SetMoney(money);

            ComputerShopHooks.allowOverride = true;
            UIManager.Instance?.ComputerShopUI?.PurchaseCart();
            ComputerShopHooks.allowOverride = false;

            foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
            {
                if (kvp.Key == connection)
                    continue;

                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_ShopOnPurchase");
                    packet.Write(money);
                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            receivedPacket.Dispose();
        }

        [Networkable("CL_ShopOnPurchase")]
        public static void ShopOnPurchase(Packet receivedPacket)
        {
            SetUpShop();

            float money = receivedPacket.ReadSingle();

            Singleton<EconomyManager>.Instance.SetMoney(money);

            ComputerShopHooks.allowOverride = true;
            UIManager.Instance?.ComputerShopUI?.PurchaseCart();
            ComputerShopHooks.allowOverride = false;

            receivedPacket.Dispose();
        }

        // this sets up the shop categories if they arent already set up
        public static void SetUpShop()
        {
            FieldInfo _selectedShopCategory = typeof(ComputerShopUI).GetField("_selectedShopCategory", BindingFlags.NonPublic | BindingFlags.Instance);

            ShopCategory selectedShopCategory = _selectedShopCategory.GetValue(UIManager.Instance?.ComputerShopUI) as ShopCategory;
            if (selectedShopCategory == null)
            {
                UIManager.Instance?.ComputerShopUI?.SetupCategories();
            }
        }
    }
}
