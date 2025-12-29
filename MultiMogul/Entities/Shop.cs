using MultiMogul.MultiMogul.Hooks;
using MultiMogul.MultiMogul.Utilities;
using Steamworks;
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

        public static void PurchaseCart(ComputerShopUI _instance, Connection? connection = null, Packet receivedPacket = null, bool createPacket = true)
        {
            if (receivedPacket != null)
            {
                Singleton<EconomyManager>.Instance.SetMoney(receivedPacket.ReadSingle());
            }

            if (!_instance.CanAffordCart())
            {
                Debug.Log("Not enough money to complete the purchase.");
                return;
            }

            Packet packet = new Packet(PacketType.OnRPCMessage);
            if (!ClientManager.IsHost())
            {
                packet.Write("SV_ShopOnPurchase");
            }
            else
            {
                packet.Write("CL_ShopOnPurchase");
            }

            packet.Write(Singleton<EconomyManager>.Instance.Money);

            FieldInfo _cartItems = typeof(ComputerShopUI).GetField("_cartItems", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo _categoryButtons = typeof(ComputerShopUI).GetField("_categoryButtons", BindingFlags.NonPublic | BindingFlags.Instance);

            Singleton<SoundManager>.Instance.PlaySoundAtLocation(_instance.PurchaseSound, Singleton<SoundManager>.Instance.PlayerTransform.position, 1f, 1f);
            bool flag = true;
            foreach (ShopCartItemButton shopCartItemButton in ((HashSet<ShopCartItemButton>)_cartItems.GetValue(_instance)).ToList<ShopCartItemButton>())
            {
                bool spawnedItem = TrySpawnItem(shopCartItemButton.ShopItem.Definition, shopCartItemButton.GetQuantity(), packet, connection, receivedPacket);
                if (!spawnedItem)
                {
                    flag = false;
                    continue;
                }

                Singleton<EconomyManager>.Instance.AddMoney((float)(-(float)shopCartItemButton.ShopItem.GetPrice() * shopCartItemButton.GetQuantity()));
                Singleton<EconomyManager>.Instance.ShopPurchases.AddPurchase(shopCartItemButton.ShopItem.GetSavableObjectID(), shopCartItemButton.GetQuantity());
                UnityEngine.Object.Destroy(shopCartItemButton.gameObject);
                ((HashSet<ShopCartItemButton>)_cartItems.GetValue(_instance)).Remove(shopCartItemButton);
            }

            if (createPacket)
            {
                if (!ClientManager.IsHost())
                {
                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }
                else
                {
                    foreach (var kvp in MultiMogulBase.serverManager.connectedClients)
                    {
                        if (connection.HasValue && kvp.Key == connection.Value)
                            continue;

                        packet.Send(kvp.Key, SendType.Reliable);
                    }
                }
            }

            packet.Dispose();

            if (!flag)
            {
                Debug.LogWarning("Some items in the cart could not be purchased due to spawn failures.");
            }

            using (HashSet<ShopCategoryButton>.Enumerator enumerator2 = (_categoryButtons.GetValue(_instance) as HashSet<ShopCategoryButton>).GetEnumerator())
            {
                while (enumerator2.MoveNext())
                {
                    ShopCategoryButton shopCategoryButton = enumerator2.Current;
                    shopCategoryButton.RefreshUI();
                }
                return;
            }
        }

        public static bool TrySpawnItem(ShopItemDefinition item, int quantity, Packet createdPacket = null, Connection? connection = null, Packet receivedPacket = null)
        {
            Transform transform = ShopSpawnPoint.GetRandomItemSpawnPoint().transform;

            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            if (receivedPacket != null)
            {
                position = receivedPacket.ReadVector3();
                rotation = receivedPacket.ReadQuaternion();
            }

            if (createdPacket != null)
            {
                createdPacket.Write(position);
                createdPacket.Write(rotation);
            }

            if (item.BuildingInventoryDefinition != null)
            {
                BuildingCrate buildingCrate = UnityEngine.Object.Instantiate<BuildingCrate>(item.BuildingInventoryDefinition.PackedPrefab ? item.BuildingInventoryDefinition.PackedPrefab : Singleton<BuildingManager>.Instance.BuildingCratePrefab, position, rotation);
                buildingCrate.Definition = item.BuildingInventoryDefinition;
                buildingCrate.Quantity = quantity;

                if (receivedPacket != null)
                {
                    string objectGUID = receivedPacket.ReadString();
                    NetworkedObjectRegistry.Register(buildingCrate, objectGUID);
                }
                else
                {
                    NetworkedObjectRegistry.Register(buildingCrate);
                }

                if (createdPacket != null)
                {
                    createdPacket.Write(NetworkedObjectRegistry.GetGUIDFromInstance(buildingCrate));
                }

                return buildingCrate != null;
            }

            if (item.PrefabToSpawn != null)
            {
                bool flag = false;
                for (int i = 0; i < quantity; i++)
                {
                    Vector3 spawnPosition = position + UnityEngine.Random.insideUnitSphere * 0.5f;
                    if (receivedPacket != null)
                    {
                        spawnPosition = receivedPacket.ReadVector3();
                    }

                    if (createdPacket != null)
                    {
                        createdPacket.Write(spawnPosition);
                    }

                    GameObject obj = UnityEngine.Object.Instantiate<GameObject>(item.PrefabToSpawn, spawnPosition, rotation);
                    if (obj == null)
                    {
                        Debug.LogWarning("Failed to spawn extra instance of " + item.GetName());
                        continue;
                    }

                    if (receivedPacket != null)
                    {
                        string objectGUID = receivedPacket.ReadString();
                        NetworkedObjectRegistry.Register(obj, objectGUID);
                    }
                    else
                    {
                        NetworkedObjectRegistry.Register(obj);
                    }

                    if (createdPacket != null)
                    {
                        createdPacket.Write(NetworkedObjectRegistry.GetGUIDFromInstance(obj));
                    }

                    flag = true;
                }

                return flag;
            }

            return false;
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

            PurchaseCart(UIManager.Instance.ComputerShopUI, connection, receivedPacket);

            receivedPacket.Dispose();
        }

        [Networkable("CL_ShopOnPurchase")]
        public static void ShopOnPurchase(Packet receivedPacket)
        {
            SetUpShop();

            PurchaseCart(UIManager.Instance.ComputerShopUI, null, receivedPacket, false);

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
