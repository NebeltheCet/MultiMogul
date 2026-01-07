using Steamworks;
using Steamworks.Data;
using System;

namespace MultiMogul.Server;

public class ServerSocketManager : SocketManager {
	public override void OnConnectionChanged(Connection connection, ConnectionInfo info) {
		switch (info.State) {
			case ConnectionState.Connected:
				MultiMogulBase.serverManager.OnClientConnected(connection, info.State);
				break;
			case ConnectionState.ClosedByPeer:
			case ConnectionState.ProblemDetectedLocally:
			case ConnectionState.None:
				MultiMogulBase.serverManager.OnClientDisconnected(connection, info.State);
				break;
			default:
				break;
		}

		// allow the SocketManager to do its thing
		base.OnConnectionChanged(connection, info);
	}

	public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel) {
		MultiMogulBase.serverManager.OnClientMessage(connection, identity, data, size, messageNum, recvTime, channel);
	}
}
