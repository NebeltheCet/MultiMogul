using System.Runtime.InteropServices;
using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using System.Net.Sockets;
using Steamworks.ServerList;
using System.Collections.Generic;
using MultiMogul.MultiMogul;
using MultiMogul.MultiMogul.Utilities;
using System.Reflection;
using System.Linq;
using MultiMogul.MultiMogul.Entities;

public class ConnectedClient
{
    public SteamId steamId = 0;
    public bool hasValidTicket = true; // originally "false" when auth tickets were implemented
}

// gameplay loop runs here, the server simulates all player commands and sends updated data to all clients
// that should allow server authoritative actions
public class ServerManager : MonoBehaviour
{
    public static ServerManager Instance;

    public Dictionary<Connection, ConnectedClient> connectedClients = new Dictionary<Connection, ConnectedClient>();
    private SocketManager serverSocket;
    public Lobby currentLobby = new Lobby(0);

    [Header("Server Defaults")]
    public int maxPlayers = 8;

    public enum ServerLobbyType : int
    { // straight copy from Facepunch.Steamworks
        Private = 0,
        FriendsOnly = 1,
        Public = 2,
        Invisible = 3,
        PrivateUnique = 4,
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"multiple instances of {nameof(ServerManager)} detected. possibly undefined behaviour?");
            return;
        }

        Instance = this;
        //SteamUser.OnValidateAuthTicketResponse += this.OnValidateAuthTicketResponse;

        DontDestroyOnLoad(this.gameObject);
        Debug.Log($"server manager initialized");
    }

    private void OnDisable()
    {
        this.StopServer();

        Instance = null;
    }

    private void FixedUpdate()
    {
        if (this.serverSocket != null && this.serverSocket.Connected.Count > 0)
        {
            foreach (var currentConnection in this.serverSocket.Connected)
            { // disconnect all players
                if (this.connectedClients.ContainsKey(currentConnection))
                    continue; // only close connections that aren't "tracked"

                currentConnection.Close(true);
            }

            int authenticatedClients = 0;
            foreach (var kvp in this.connectedClients)
            {
                if (!kvp.Value.hasValidTicket)
                    continue;

                authenticatedClients++;
            }

            this.currentLobby.SetData("playerCount", authenticatedClients.ToString());
        }

        this.serverSocket?.Receive();

        foreach (var player in Player.activePlayerList)
        {
            player.OnUpdate();
        }

        Player.OnServerUpdate();
    }

    public void StartServer()
    {
        if (this.serverSocket != null)
        {
            Debug.LogWarning("server is already running");
            return;
        }

        this.serverSocket = SteamNetworkingSockets.CreateRelaySocket<ServerSocket>(0);
        if (this.serverSocket == null)
        {
            Debug.LogError("failed to create server socket");
            return;
        }

        Debug.Log($"server socket created: {SteamClient.SteamId}");

        CreateLobbyAsync(this.maxPlayers, ServerLobbyType.Public);

        GameObject playerObject = UnityEngine.GameObject.Find("Player");
        if (playerObject != null)
        {
            Player localPlayer = new Player();
            localPlayer.position = playerObject.transform.position;
            localPlayer.rotation = playerObject.transform.rotation;
            localPlayer.scale = playerObject.transform.localScale;
            localPlayer.steamId = SteamClient.SteamId;
            localPlayer.isLocalPlayer = true;

            Player.activePlayerList.Add(localPlayer);
        }
    }

    public void StopServer()
    {
        //SteamUser.OnValidateAuthTicketResponse -= this.OnValidateAuthTicketResponse;
        if (this.serverSocket != null && this.serverSocket.Connected.Count > 0)
        {
            foreach (var currentConnection in this.serverSocket.Connected)
            { // disconnect all players
                currentConnection.Close(true);
            }
        }

        this.serverSocket?.Close();
        this.serverSocket = null;

        if (this.currentLobby.Id != 0)
        {
            this.currentLobby.Leave();
        }
        this.currentLobby = new Lobby(0);

        Debug.Log($"server socket stopped.");
    }

    private async void CreateLobbyAsync(int maxPlayers = 8, ServerLobbyType lobbyType = ServerLobbyType.Private)
    {
        var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        if (!lobbyResult.HasValue)
        {
            Debug.LogError("failed to create steam lobby");
            return;
        }

        this.currentLobby = lobbyResult.Value;
        this.currentLobby.SetJoinable(true); // allow people to join

        // change the publicity type
        switch (lobbyType)
        {
            case ServerLobbyType.Private:
                this.currentLobby.SetPrivate();
                break;
            case ServerLobbyType.FriendsOnly:
                this.currentLobby.SetFriendsOnly();
                break;
            case ServerLobbyType.Public:
                this.currentLobby.SetPublic();
                break;
            case ServerLobbyType.Invisible:
                this.currentLobby.SetInvisible();
                break;
            // PrivateUnique isn't implemented in Facepunch.Steamworks
            //case ServerLobbyType.PrivateUnique:
            //    break;
            default:
                break;
        }

        // Set custom key/value data that clients can filter on
        this.currentLobby.SetData("gameName", Application.productName);
        this.currentLobby.SetData("gameBuild", Application.version + " | " + Application.unityVersion);

        // game data
        this.currentLobby.SetData("serverID", SteamClient.SteamId.ToString());
        this.currentLobby.SetData("serverName", $"{SteamClient.SteamId.Value}'s Lobby");
        this.currentLobby.SetData("playerCount", "0");
        this.currentLobby.SetData("maxPlayers", maxPlayers.ToString());

        Debug.Log($"created steam lobby: {SteamClient.SteamId}[{lobbyType.ToString().ToLower()}]");
    }

    /* TODO: Auth Tickets are needed to prevent steam id spoofing and spawning in as another player. */

    // "owner" will be ignored unless we want to make checks for family shared accounts
    //private void OnValidateAuthTicketResponse(SteamId steamId, SteamId owner, AuthResponse response) {
    //    foreach (var kvp in this.connectedClients) {
    //        if (kvp.Value.steamId != steamId)
    //            continue;

    //        Debug.Log($"auth ticket state for \"{steamId}\": {response}");
    //        using (Packet packet = new Packet(PacketType.OnAuthTicketResponse)) {
    //            packet.Write((int)response);
    //            packet.Send(kvp.Key, SendType.Reliable);
    //        }

    //        if (response == AuthResponse.OK) {
    //            kvp.Value.hasValidTicket = true;
    //        }

    //        break;
    //    }
    //}

    //private void HandleAuthTicket(Connection connection, Packet receivedPacket) {
    //    if (!this.connectedClients.ContainsKey(connection)) {
    //        Debug.LogWarning($"client with id [{connection.Id}] tried requesting auth ticket while not tracked, ignoring request");
    //        return;
    //    }

    //    ConnectedClient connectedClient = this.connectedClients[connection];
    //    if (connectedClient == null)
    //        return;

    //    if (connectedClient.hasValidTicket)
    //        return; // the client already authenticated properly

    //    SteamId steamId = receivedPacket.ReadUInt64();
    //    byte[] ticketData = receivedPacket.ReadBytes();

    //    connectedClient.steamId = steamId;

    //    var result = SteamUser.BeginAuthSession(ticketData, steamId);
    //    if (result != BeginAuthResult.OK) {
    //        Debug.LogWarning($"immediate BeginAuthSession failure for \"{steamId}\", kicking");

    //        connection.Close(true);
    //    }

    //    Debug.Log($"client with id [{connection.Id}] requested auth ticket for steam id \"{steamId}\", processing...");
    //}

    public void OnClientConnected(Connection connection, ConnectionState connectionState)
    {
        if (this.connectedClients.ContainsKey(connection))
        {
            Debug.LogWarning($"client with id [{connection.Id}] is already tracked as connected. ignoring connect");
            return;
        }

        // accept client connection
        connection.Accept();

        Debug.Log($"client with id [{connection.Id}] connected");
        using (Packet packet = new Packet(PacketType.OnConnectionApproved))
        {
            packet.Send(connection, SendType.Reliable);
        }

        // add the connection as client
        ServerManager.Instance.connectedClients.Add(connection, new ConnectedClient());
    }

    public void OnClientDisconnected(Connection connection, ConnectionState connectionState)
    {
        if (!this.connectedClients.ContainsKey(connection))
        {
            Debug.LogWarning($"client with id [{connection.Id}] is not tracked as connected. ignoring disconnect");
            return;
        }

        ConnectedClient connectedClient = this.connectedClients[connection];
        if (connectedClient == null)
            return;

        if (!connectedClient.hasValidTicket)
            return; // the client never authenticated properly

        //SteamUser.EndAuthSession(connectedClient.steamId);

        Player existingPlayer = Player.activePlayerList
                     .Find(p => p.steamId == connectedClient.steamId);
        if (existingPlayer != null)
        {
            existingPlayer.OnRemoved();
            Player.activePlayerList.Remove(existingPlayer);
        }

        Debug.Log($"client with id [{connection.Id}] disconnected");
        connection.Close(false);
        this.connectedClients.Remove(connection);
    }

    public void TransportPacket(Connection connection, Packet packet)
    {
        if (packet == null)
        {
            Debug.LogWarning("failed to handle packet: invalid packet!");
            return;
        }

        int packetHash = packet.ReadString().GetHashCode();
        ThreadDispatcher.Enqueue(() =>
        {
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in allTypes)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttributes(typeof(Networkable), false).FirstOrDefault() is not Networkable attribute)
                        continue;

                    if (packet.GetPacketType() != PacketType.OnRPCMessage)
                        continue;

                    if (attribute.packetHash != packetHash)
                        continue;

                    if (method.IsStatic)
                    {
                        method.Invoke(null, [connection, packet]);
                        return;
                    }

                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        method.Invoke(UnityEngine.Object.FindFirstObjectByType(type), [connection, packet]);
                        return;
                    }

                    method.Invoke(Activator.CreateInstance(type), [connection, packet]);
                    return;
                }
            }

            Debug.LogWarning($"failed to find handler for: {Enum.GetName(typeof(PacketType), packet.GetPacketType())}[{packetHash}]");
        });
    }

    public void OnClientMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        if (!this.connectedClients.ContainsKey(connection))
        {
            Debug.LogWarning($"client with id [{connection.Id}] is not tracked as connected. ignoring message");
            return;
        }

        Packet receivedPacket = new Packet(data, size);

        PacketType packetType = receivedPacket.GetPacketType();
        switch (packetType)
        {
            case PacketType.OnConnectionApproved:
                this.OnConnectionApproved(connection, receivedPacket);
                receivedPacket.Dispose();
                break;
            case PacketType.OnAuthTicket:
                //this.HandleAuthTicket(connection, receivedPacket);
                receivedPacket.Dispose();
                break;
            case PacketType.OnRPCMessage:
                this.TransportPacket(connection, receivedPacket);
                break;
            default:
                break;
        }
    }

    // client sent back approved state, send them the rest of the data
    private void OnConnectionApproved(Connection connection, Packet receivedPacket)
    {
        Debug.Log($"client with id [{connection.Id}] sent back approved state");

        ulong steamId = receivedPacket.ReadUInt64();
        ConnectedClient connectedClient = this.connectedClients[connection];

        connectedClient.steamId = steamId;
        using (Packet packet = new Packet(PacketType.OnRPCMessage))
        {
            packet.Write("CL_OnWorldReceive");
            packet.Write(SaveManager.GetSaveFile());
            packet.Send(connection, SendType.Reliable);
            Debug.Log("sending save file to client");
        }

        Player player = new Player();
        player.position = new Vector3(0f, 0f, 0f);
        player.rotation = new Quaternion(0f, 0f, 0f, 0f);
        player.scale = new Vector3(1f, 1f, 1f);
        player.steamId = steamId;

        Player.activePlayerList.Add(player);

        Player.NetworkPlayerList();
    }
}

public class ServerSocket : SocketManager
{
    public override void OnConnectionChanged(Connection connection, ConnectionInfo info)
    {
        switch (info.State)
        {
            case ConnectionState.Connected:
                ServerManager.Instance.OnClientConnected(connection, info.State);
                break;
            case ConnectionState.ClosedByPeer:
            case ConnectionState.ProblemDetectedLocally:
            case ConnectionState.None:
                ServerManager.Instance.OnClientDisconnected(connection, info.State);
                break;
            default:
                break;
        }

        // allow the SocketManager to do its thing
        base.OnConnectionChanged(connection, info);
    }

    public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        ServerManager.Instance.OnClientMessage(connection, identity, data, size, messageNum, recvTime, channel);
    }
}