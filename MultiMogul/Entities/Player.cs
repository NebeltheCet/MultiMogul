using MultiMogul.MultiMogul.Hooks;
using MultiMogul.MultiMogul.Utilities;
using MultiMogul.MultiMogul.Utilities.CustomSave;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.PostProcessing.SubpixelMorphologicalAntialiasing;

namespace MultiMogul.MultiMogul.Entities
{
    public class Player
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public SteamId steamId;
        public string playerName;

        public bool isLocalPlayer;
        public bool wasCreated;
        public bool finishedLoadingSave;
        public GameObject playerObject;

        public static List<Player> activePlayerList = new List<Player>();
        private static GameObject playerPrefab;

        private float lastTickTime = 0f;
        private float lastInventoryTime = 0f;
        private static float lastServerTickTime = 0f; // this one is used for base data that can be sent frequently
        private static float lastServerTickTime2 = 0f; // this one is used for reliable data that needs to be sent less frequently
        private static int lastQuestHash = -1;

        private const int tickRate = 20;
        private const float intervalPerTick = (1f / tickRate);

        public BuildingObject ghostObject;

        public static Player GetLocalPlayer()
        {
            return Player.activePlayerList.Find(p => p.isLocalPlayer);
        }

        private static void CreateObject()
        {
            if (playerPrefab != null)
                return;

            var player = new GameObject($"Player_");
            var playerBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            playerBody.transform.SetParent(player.transform);
            player.SetActive(false);

            playerPrefab = player;
            UnityEngine.Object.DontDestroyOnLoad(playerPrefab);
        }

        public void OnRemoved()
        {
            if (this.playerObject != null)
            {
                UnityEngine.Object.Destroy(this.playerObject);
            }
        }

        private void CreatePlayer()
        {
            if (this.wasCreated)
                return;

            if (!this.isLocalPlayer)
            {
                this.playerObject = UnityEngine.Object.Instantiate(playerPrefab, this.position, this.rotation);
                this.playerObject.name = $"Player_{this.steamId}";
                this.playerObject.transform.localScale = this.scale;
                this.playerObject.SetActive(true);

                UnityEngine.Object.DontDestroyOnLoad(this.playerObject);
            }
            else
            {
                GameObject playerObject = UnityEngine.GameObject.Find("Player");
                if (playerObject != null)
                {
                    this.playerObject = playerObject;
                    this.playerObject.name = $"Player_{this.steamId}";
                }
            }

            Friend friend = new Friend(this.steamId);

            this.playerName = friend.Name;
            this.wasCreated = this.playerObject != null;
        }

        private void SendPlayerTick()
        {
            if ((Time.realtimeSinceStartup - this.lastTickTime) < intervalPerTick)
                return;

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("SV_OnReceivePlayerTick");

                packet.Write(this.position);
                packet.Write(this.rotation);
                packet.Write(this.scale);

                if (!ClientManager.IsHost())
                {
                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Unreliable);
                }
            }

            this.lastTickTime = Time.realtimeSinceStartup;
        }

        private void SendInventoryUpdate()
        {
            PlayerInventory inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            if (inventory == null)
                return;

            if ((Time.realtimeSinceStartup - this.lastInventoryTime) > (intervalPerTick * (tickRate / 2)) && !ClientManager.IsHost())
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("SV_OnInventoryUpdate");
                    packet.Write(inventory.Items.Count);
                    foreach (var item in inventory.Items)
                    {
                        packet.Write(item != null);
                        if (item == null)
                            continue;

                        int quantity = -1;
                        int savableObjectId = (int)item.GetSavableObjectID();
                        int toolBuilderObjectId = 0;
                        if (item is ToolBuilder builderItem)
                        {
                            quantity = builderItem.Quantity;
                            toolBuilderObjectId = (int)builderItem.Definition.BuildingPrefab.SavableObjectID;
                        }

                        packet.Write(savableObjectId);
                        packet.Write(inventory.GetInventoryIndexForTool(item));
                        packet.Write(quantity);
                        packet.Write(toolBuilderObjectId);
                    }

                    packet.Send(ClientManager.Instance.connection.Connection, SendType.Reliable);
                }

                this.lastInventoryTime = Time.realtimeSinceStartup;
            }

            if (ClientManager.IsHost() && inventory != null)
            {
                CustomPlayerEntry customPlayerEntry = SaveManager.lastPlayerEntries.Find(p => p.SteamID == this.steamId);
                if (customPlayerEntry != null)
                {
                    customPlayerEntry.InventoryEntries.Clear();
                    foreach (var item in inventory.Items)
                    {
                        if (item == null)
                            continue;

                        int quantity = -1;
                        SavableObjectID savableObjectId = item.GetSavableObjectID();
                        SavableObjectID toolBuilderObjectId = SavableObjectID.INVALID;
                        if (item is ToolBuilder builderItem)
                        {
                            quantity = builderItem.Quantity;
                            toolBuilderObjectId = builderItem.Definition.BuildingPrefab.SavableObjectID;
                        }

                        customPlayerEntry.InventoryEntries.Add(new CustomInventoryEntry
                        {
                            SavableObjectID = savableObjectId,
                            InventorySlotIndex = inventory.GetInventoryIndexForTool(item),

                            Quantity = quantity,
                            BuildObjectID = toolBuilderObjectId
                        });
                    }
                }
            }
        }

        public void OnUpdate()
        {
            CreateObject();
            this.CreatePlayer();
            if (!this.wasCreated)
                return;

            if (!SaveManager.IsLoadingGame() && !this.finishedLoadingSave && this.isLocalPlayer)
            {
                CustomPlayerEntry customPlayerEntry = SaveManager.lastPlayerEntries.Find(p => p.SteamID == this.steamId);
                if (customPlayerEntry != null)
                {
                    this.InitializeInventory(customPlayerEntry);

                    UnityEngine.Object.FindObjectOfType<PlayerController>().TeleportPlayer(customPlayerEntry.Position.ToVector3(), customPlayerEntry.Rotation.ToVector3());
                }
                else
                {
                    StartingElevator startingElevator = UnityEngine.Object.FindFirstObjectByType<StartingElevator>();

                    if (startingElevator != null)
                    {
                        UnityEngine.Object.FindObjectOfType<PlayerController>().TeleportPlayer(startingElevator.PlayerTeleportPosition.position, Vector3.zero);
                    }
                }

                this.finishedLoadingSave = true;
            }

            if (!this.isLocalPlayer)
            {
                this.playerObject.transform.position = this.position;
                this.playerObject.transform.rotation = this.rotation;
                this.playerObject.transform.localScale = this.scale;
            }
            else
            {
                this.position = this.playerObject.transform.position;
                this.rotation = this.playerObject.transform.rotation;
                this.scale = this.playerObject.transform.localScale;

                this.SendPlayerTick();
                this.SendInventoryUpdate();
            }
        }

        public void InitializeInventory(CustomPlayerEntry customPlayerEntry)
        {
            if (ClientManager.IsHost())
                return; // host items get processed by our save manager

            foreach (var itemEntry in customPlayerEntry.InventoryEntries)
            {
                GameObject prefab = SavingLoadingManager.Instance.GetPrefab(itemEntry.SavableObjectID);
                ISaveLoadableObject saveLoadableObject2;

                GameObject obj = UnityEngine.Object.Instantiate<GameObject>(prefab, Vector3.zero, Quaternion.Euler(Vector3.zero));
                if (prefab != null && obj.TryGetComponent<ISaveLoadableObject>(out saveLoadableObject2))
                {
                    MethodInfo method = typeof(BaseHeldTool).GetMethod("WaitThenAddToInventory", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (method == null)
                        throw new MissingMethodException("WaitThenAddToInventory not found");

                    if (itemEntry.Quantity != -1)
                    {
                        ToolBuilder toolBuilder = obj.GetComponent<ToolBuilder>();
                        if (toolBuilder != null)
                        {
                            toolBuilder.Definition = Singleton<SavingLoadingManager>.Instance.GetBuildingInventoryDefinition(itemEntry.BuildObjectID);
                            toolBuilder.Quantity = itemEntry.Quantity;

                            IEnumerator coroutine = (IEnumerator)method.Invoke(toolBuilder, [itemEntry.InventorySlotIndex]);
                            toolBuilder.StartCoroutine(coroutine);
                        }
                    }
                    else
                    {
                        BaseHeldTool heldTool = obj.GetComponent<BaseHeldTool>();
                        if (heldTool != null)
                        {
                            IEnumerator coroutine = (IEnumerator)method.Invoke(heldTool, [itemEntry.InventorySlotIndex]);
                            heldTool.StartCoroutine(coroutine);
                        }
                    }
                }
            }
        }

        public static void DrawNametags() // this has to be cleaned up at some point
        {
            foreach (var player in activePlayerList)
            {
                if (player.isLocalPlayer)
                    continue;

                if (player.playerObject == null)
                    continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(player.playerObject.transform.position + new Vector3(0f, player.scale.y + 0.15f, 0f));
                if (screenPos.z < 0f)
                    continue;

                GUIStyle defaultStyle = new GUIStyle(GUI.skin.label);
                defaultStyle.alignment = TextAnchor.UpperLeft;
                defaultStyle.fontSize = 14;
                defaultStyle.normal.textColor = UnityEngine.Color.white;
                defaultStyle.fontStyle = FontStyle.Normal;

                GUIStyle shadowStyle = new GUIStyle(GUI.skin.label);
                shadowStyle.alignment = TextAnchor.UpperLeft;
                shadowStyle.fontSize = 14;
                shadowStyle.normal.textColor = UnityEngine.Color.black;
                shadowStyle.fontStyle = FontStyle.Normal;

                GUIContent content = new GUIContent(player.playerName);

                Vector2 rectSize = defaultStyle.CalcSize(content);
                Vector2 textPosition = new Vector2(screenPos.x - (rectSize.x / 2f), (Screen.height - screenPos.y) - (rectSize.y / 2f));

                GUI.Label(new Rect(textPosition.x - 1f, textPosition.y - 1f, rectSize.x, rectSize.y), player.playerName, shadowStyle);
                GUI.Label(new Rect(textPosition.x - 1f, textPosition.y + 1f, rectSize.x, rectSize.y), player.playerName, shadowStyle);
                GUI.Label(new Rect(textPosition.x + 1f, textPosition.y + 1f, rectSize.x, rectSize.y), player.playerName, shadowStyle);
                GUI.Label(new Rect(textPosition.x + 1f, textPosition.y - 1f, rectSize.x, rectSize.y), player.playerName, shadowStyle);

                GUI.Label(new Rect(textPosition.x, textPosition.y, rectSize.x, rectSize.y), player.playerName, defaultStyle);
            }
        }

        public static void OnGUI()
        {
            DrawNametags();
        }

        public static void SendServerTick()
        {
            int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            foreach (var player in activePlayerList)
            {
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnReceivePlayerTick");

                    packet.Write(player.steamId);
                    packet.Write(player.position);
                    packet.Write(player.rotation);
                    packet.Write(player.scale);
                    packet.Write(newSeed);

                    foreach (var kvp in ServerManager.Instance.connectedClients)
                    {
                        if (player.steamId == kvp.Value.steamId)
                            continue;

                        packet.Send(kvp.Key, SendType.Unreliable);
                    }
                }

                CustomPlayerEntry customPlayerEntry = SaveManager.lastPlayerEntries.Find(p => p.SteamID == player.steamId);
                if (customPlayerEntry != null)
                {
                    customPlayerEntry.Position = player.position;
                    customPlayerEntry.Rotation = player.rotation.eulerAngles;
                }
                else
                {
                    SaveManager.lastPlayerEntries.Add(new CustomPlayerEntry
                    {
                        Position = player.position,
                        Rotation = player.rotation.eulerAngles,
                        SteamID = player.steamId
                    });
                }
            }

            UnityEngine.Random.InitState(newSeed);
        }

        public static void SendServerEconomy()
        {
            if (EconomyManager.Instance == null)
                return;

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("CL_OnReceiveState");

                packet.Write(EconomyManager.Instance.Money);
                foreach (var kvp in ServerManager.Instance.connectedClients)
                {
                    packet.Send(kvp.Key, SendType.Unreliable);
                }
            }
        }

        public static void SendServerQuests()
        {
            if (QuestManager.Instance == null)
                return;

            List<QuestID> completedQuestIDs = QuestManager.Instance.GetCompletedQuestIDs();
            List<ActiveQuestEntry> activeQuestEntries = QuestManager.Instance.GetActiveQuestSaveEntries();

            string completedQuestIDsSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(completedQuestIDs);
            string activeQuestEntriesSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(activeQuestEntries);

            int currentHash = (completedQuestIDsSerialized + activeQuestEntriesSerialized).GetHashCode();
            if (currentHash == lastQuestHash)
                return;

            using (Packet packet = new Packet(PacketType.OnRPCMessage))
            {
                packet.Write("CL_OnReceiveQuests");
                packet.Write(completedQuestIDsSerialized);
                packet.Write(activeQuestEntriesSerialized);

                foreach (var kvp in ServerManager.Instance.connectedClients)
                {
                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            //Debug.Log($"sent quest updates to all clients[size: {(completedQuestIDsSerialized.Length + activeQuestEntriesSerialized.Length)}]");
            lastQuestHash = currentHash;
        }

        static public void OnServerUpdate()
        {
            if ((Time.realtimeSinceStartup - lastServerTickTime) > intervalPerTick)
            {
                SendServerTick();
                SendServerEconomy();

                lastServerTickTime = Time.realtimeSinceStartup;
            }

            if ((Time.realtimeSinceStartup - lastServerTickTime2) > (intervalPerTick * (tickRate / 8)))
            {
                SendServerQuests();

                lastServerTickTime2 = Time.realtimeSinceStartup;
            }
        }

        static public void NetworkPlayerList()
        {
            foreach (var kvp in ServerManager.Instance.connectedClients)
            {
                ConnectedClient connectedClient = ServerManager.Instance.connectedClients[kvp.Key];
                using (Packet packet = new Packet(PacketType.OnRPCMessage))
                {
                    packet.Write("CL_OnPlayerListReceive");

                    if (Player.activePlayerList == null)
                    {
                        packet.Write(0);
                    }
                    else
                    {
                        packet.Write(Player.activePlayerList.Count);
                        foreach (Player player in Player.activePlayerList)
                        {
                            packet.Write(player.steamId);
                            packet.Write(player.position);
                            packet.Write(player.rotation);
                            packet.Write(player.scale);
                        }
                    }

                    packet.Send(kvp.Key, SendType.Reliable);
                }
            }

            Debug.Log("sent player list to clients");
        }

        [Networkable("CL_OnReceiveState")]
        static public void OnReceiveState(Packet receivedPacket)
        {
            float money = receivedPacket.ReadSingle();

            EconomyManager.Instance?.SetMoney(money);

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnPlayerListReceive")]
        static public void OnPlayerListReceive(Packet receivedPacket)
        {
            int playerCount = receivedPacket.ReadInt32();
            HashSet<SteamId> handledPlayers = new HashSet<SteamId>();

            Debug.Log($"received {playerCount} players");

            handledPlayers.Add(SteamClient.SteamId);
            for (int i = 0; i < playerCount; i++)
            {
                SteamId steamId = receivedPacket.ReadUInt64();
                Vector3 position = receivedPacket.ReadVector3();
                Quaternion rotation = receivedPacket.ReadQuaternion();
                Vector3 scale = receivedPacket.ReadVector3();

                handledPlayers.Add(steamId);

                Player existingPlayer = Player.activePlayerList
                    .Find(p => p.steamId == steamId);

                if (existingPlayer != null)
                {
                    existingPlayer.position = position;
                    existingPlayer.rotation = rotation;
                    existingPlayer.scale = scale;
                }
                else
                {
                    Player newPlayer = new Player()
                    {
                        steamId = steamId,
                        position = position,
                        rotation = rotation,
                        scale = scale,
                        isLocalPlayer = (steamId == SteamClient.SteamId)
                    };
                    Player.activePlayerList.Add(newPlayer);

                    Debug.Log($"created player[local: {(steamId == SteamClient.SteamId).ToString()}]");

                }
            }

            Player.activePlayerList.RemoveAll(p =>
            {
                bool shouldRemove = !handledPlayers.Contains(p.steamId);

                if (shouldRemove)
                {
                    p.OnRemoved();
                    Debug.Log($"removed player[{p.steamId}]");
                }

                return shouldRemove;
            });

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnReceivePlayerTick")]
        static public void OnReceivePlayerTick(Packet receivedPacket)
        {
            ulong steamId = receivedPacket.ReadUInt64();

            Player existingPlayer = Player.activePlayerList.Find(p => p.steamId == steamId);
            if (existingPlayer == null)
            {
                Debug.LogWarning($"received player tick for non-existent player {steamId}");
                receivedPacket.Dispose();
                return;
            }

            existingPlayer.position = receivedPacket.ReadVector3();
            existingPlayer.rotation = receivedPacket.ReadQuaternion();
            existingPlayer.scale = receivedPacket.ReadVector3();

            // sync random seed with the server
            UnityEngine.Random.InitState(receivedPacket.ReadInt32());

            //Debug.LogWarning($"received player tick for player {steamId}");

            receivedPacket.Dispose();
        }

        [Networkable("CL_OnReceiveQuests")]
        static public void OnReceiveQuests(Packet receivedPacket)
        {
            string completedQuestIDsSerialized = receivedPacket.ReadString();
            string activeQuestEntriesSerialized = receivedPacket.ReadString();

            CustomSaveFile questSaveFile = new CustomSaveFile // this is stupid, but makes things a lot cleaner in the end
            {
                CompletedQuestsIDs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestID>>(completedQuestIDsSerialized),
                ActiveQuests = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ActiveQuestEntry>>(activeQuestEntriesSerialized)
            };

            SaveManager.LoadQuestsFromSaveFile(Singleton<QuestManager>.Instance, questSaveFile);
            receivedPacket.Dispose();
        }

        [Networkable("SV_OnReceivePlayerTick")]
        static public void OnReceivePlayerTickServer(Connection connection, Packet receivedPacket)
        {
            ConnectedClient connectedClient = ServerManager.Instance.connectedClients[connection];

            Player existingPlayer = Player.activePlayerList.Find(p => p.steamId == connectedClient.steamId);
            if (existingPlayer == null)
            {
                Debug.LogWarning($"received player tick for non-existent player {connectedClient.steamId}");
                receivedPacket.Dispose();
                return;
            }

            existingPlayer.position = receivedPacket.ReadVector3();
            existingPlayer.rotation = receivedPacket.ReadQuaternion();
            existingPlayer.scale = receivedPacket.ReadVector3();

            //Debug.LogWarning($"received server player tick for player {connectedClient.steamId}");

            receivedPacket.Dispose();
        }

        [Networkable("SV_OnInventoryUpdate")]
        public static void OnInventoryUpdateServer(Connection connection, Packet receivedPacket)
        {
            ConnectedClient connectedClient = MultiMogulBase.serverManager.connectedClients[connection];
            if (connectedClient == null)
            {
                receivedPacket.Dispose();
                return;
            }

            CustomPlayerEntry customPlayerEntry = SaveManager.lastPlayerEntries.Find(p => p.SteamID == connectedClient.steamId);
            if (customPlayerEntry == null)
            {
                Debug.LogWarning($"failed to find CustomPlayerEntry for player with id[{connectedClient.steamId}]");

                receivedPacket.Dispose();
                return;
            }

            customPlayerEntry.InventoryEntries.Clear();

            int itemAmount = receivedPacket.ReadInt32();
            for (int itemIndex = 0; itemIndex < itemAmount; itemIndex++)
            {
                bool isValid = receivedPacket.ReadBool();
                if (!isValid)
                    continue;

                SavableObjectID savableObjectID = (SavableObjectID)receivedPacket.ReadInt32();
                int inventoryIndex = receivedPacket.ReadInt32();
                int itemQuantity = receivedPacket.ReadInt32();
                SavableObjectID toolBuilderObjectId = (SavableObjectID)receivedPacket.ReadInt32();

                customPlayerEntry.InventoryEntries.Add(new CustomInventoryEntry
                {
                    SavableObjectID = savableObjectID,
                    InventorySlotIndex = inventoryIndex,

                    Quantity = itemQuantity,
                    BuildObjectID = toolBuilderObjectId
                });
            }

            receivedPacket.Dispose();
        }
    }
}
