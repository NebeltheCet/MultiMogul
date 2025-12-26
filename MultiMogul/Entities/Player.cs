using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

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
        public GameObject playerObject;

        public static List<Player> activePlayerList = new List<Player>();
        private static GameObject playerPrefab;

        private float lastTickTime = 0f;
        private static float lastServerTickTime = 0f;

        private const int tickRate = 20;
        private const float intervalPerTick = (1f / tickRate);


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

        public void OnUpdate()
        {
            CreateObject();
            if (!this.wasCreated)
            {
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

            if (!this.wasCreated)
                return;

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

                if ((Time.realtimeSinceStartup - this.lastTickTime) > intervalPerTick)
                {
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
            }
        }

        public static void OnGUI()
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

        static public void OnServerUpdate()
        {
            if ((Time.realtimeSinceStartup - lastServerTickTime) > intervalPerTick)
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
                }

                UnityEngine.Random.InitState(newSeed);
                lastServerTickTime = Time.realtimeSinceStartup;
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

                if (shouldRemove) {
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
    }
}
