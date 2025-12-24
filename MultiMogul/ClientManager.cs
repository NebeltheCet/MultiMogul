using MultiMogul.MultiMogul;
using MultiMogul.MultiMogul.Entities;
using MultiMogul.MultiMogul.Utilities;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class ClientManager : MonoBehaviour {
    public static ClientManager Instance;

    public ConnectionManager connection;
    private AuthTicket currentTicket = null;
    private SteamId connectedServerId;

    private void Awake() {
        if (Instance != null) {
            Debug.LogWarning($"multiple instances of {nameof(ClientManager)} detected. possibly undefined behaviour?");
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
        Debug.Log($"client manager initialized");
    }

    private void OnDisable() {
        this.Disconnect();

        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        Instance = null;
    }

    private void FixedUpdate() {
        this.connection?.Receive();

        foreach (var player in Player.activePlayerList)
        {
            player.OnUpdate();
        }
    }

    public void ConnectToServer() {
        ulong hostSteamId = this.connectedServerId;
        if (!SteamManager.Instance.IsInitialized) {
            Debug.LogError("steam is not initialized!");
            return;
        }

        try {
            this.connection = SteamNetworkingSockets.ConnectRelay<ClientConnection>(hostSteamId, 0);
            Debug.Log($"connecting to server: \"{hostSteamId}\"");
        }
        catch (Exception e) {
            Debug.LogError($"failed to connect to server: {e.Message}");
            return;
        }
    }

    public void Disconnect() {
        this.currentTicket?.Cancel();
        this.currentTicket = null;

        this.connection?.Close();
        this.connection = null;

        Debug.Log($"client disconnected.");

        if (UIManager.Instance != null && UIManager.Instance.PauseMenu != null)
        {
            UIManager.Instance.PauseMenu.OnReturnToMainMenuPressed();

            Debug.Log("returned to main menu.");
        }

        if (AutoSaveManager.Instance != null)
        {
            AutoSaveManager.Instance.AutoSaveEnabled = true;
        }
    }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId) {
        Debug.Log($"Received lobby invite from {friendId}, joining lobby {lobby.Id}");

        this.connectedServerId = friendId;
        this.ConnectToServer();
    }

    public void OnConnectionApproved(Packet receivedPacket) {
        //Debug.Log("connection approved by server, requesting auth ticket...");

        //this.currentTicket = SteamUser.GetAuthSessionTicket(SteamClient.SteamId);
        //using (Packet packet = new Packet(PacketType.OnAuthTicket)) {
        //    packet.Write(SteamClient.SteamId.Value);
        //    packet.WriteBytes(this.currentTicket.Data);

        //    packet.Send(this.connection.Connection, SendType.Reliable);
        //}

        Debug.Log("connection approved by server, waiting for data...");
        using (Packet packet = new Packet(PacketType.OnConnectionApproved)) {
            packet.Write(SteamClient.SteamId.Value);
            packet.Send(this.connection.Connection, SendType.Reliable);
        }
    }

    //public void OnAuthTicketResponse(Packet receivedPacket) {
    //    AuthResponse authResponse = (AuthResponse)receivedPacket.ReadInt32();
    //    if (authResponse == AuthResponse.AuthTicketCanceled)
    //        return;

    //    SteamUser.EndAuthSession(ClientManager.Instance.connectedServerId);

    //    bool wasApproved = authResponse == AuthResponse.OK;
    //    if (!wasApproved) {
    //        Debug.LogError("auth ticket denied by server");
    //        return;
    //    }

    //    Debug.Log("auth ticket approved by server");
    //    // TODO: switch to server view
    //}

    public void TransportPacket(Packet packet)
    {
        if (packet == null)
        {
            Debug.LogWarning("failed to handle packet: invalid packet!");
            return;
        }

        int packetHash = packet.ReadString().GetHashCode();
        ThreadDispatcher.Enqueue(() => {
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
                        method.Invoke(null, new object[] { packet });
                        return;
                    }

                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        method.Invoke(UnityEngine.Object.FindObjectOfType(type), new object[] { packet });
                        return;
                    }

                    method.Invoke(Activator.CreateInstance(type), new object[] { packet });
                    return;
                }
            }

            Debug.LogWarning($"failed to find handler for: {Enum.GetName(typeof(PacketType), packet.GetPacketType())}[{packetHash}]");
        });
    }

    public void OnServerMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) {
        Packet packet = new Packet(data, size);

        //using (Packet packet = new Packet(data, size)) {
            PacketType packetType = packet.GetPacketType();
            switch (packetType) {
                case PacketType.OnConnectionApproved:
                    this.OnConnectionApproved(packet);
                    packet.Dispose();
                    break;
                case PacketType.OnAuthTicketResponse:
                    //this.OnAuthTicketResponse(packet);
                    packet.Dispose();
                    break;
                case PacketType.OnRPCMessage: // handle rpcs here and forward them to the correct function
                    this.TransportPacket(packet);
                    break;
                default:
                    break;
            }
        //}
    }

    private class ClientConnection : ConnectionManager {
        public override void OnDisconnected(ConnectionInfo info) {
            Debug.Log($"disconnected by server");
            ClientManager.Instance.Disconnect();

            //SteamUser.EndAuthSession(ClientManager.Instance.connectedServerId);
            if (ClientManager.Instance.currentTicket != null) {
                ClientManager.Instance.currentTicket.Cancel();
                ClientManager.Instance.currentTicket = null;
            }
        }

        public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) {
            ClientManager.Instance.OnServerMessage(data, size, messageNum, recvTime, channel);
        }
    }
}
