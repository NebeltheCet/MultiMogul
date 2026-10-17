using MultiMogul.Client;
using MultiMogul.Hooks;
using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Server;
using MultiMogul.Utilities;
using MultiMogul.Utilities.NetworkedObjects;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace MultiMogul.Entities;
public static class DamageEntities {
	[Networkable(PacketType.OnRPCMessage, "OnTakeDamage", 64, 4, true)]
	public static void SV_OnTakeDamage(ConnectionData connectionData, RawPacket receivedPacket) {
		MMLog.Log("received take damage", LogTypes.ControlFlow);

		OnTakeDamage takeDamage = receivedPacket.Read<OnTakeDamage>();
		if (takeDamage == null) {
			MMLog.LogWarning("received invalid take damage packet");
			return;
		}

		GameObject networkedObject = NetworkedObjectRegistry.GetFromGUIDHash(takeDamage.victimGuid);
		if (networkedObject == null) {
			MMLog.LogWarning($"received non networked victim, possibly a networking error, notify the developers! | {takeDamage.victimGuid}");
			return;
		}

		IDamageable damageableComponent = networkedObject.GetComponent<IDamageable>();
		if (damageableComponent == null) {
			MMLog.LogWarning("received networked object doesn't have a IDamageable component!");
			return;
		}

		IDamageableHooks.ignoreHook = true;
		damageableComponent.TakeDamage(new DamageInfo(takeDamage.damageInfo.Damage, takeDamage.damageInfo.DamageType, takeDamage.damageInfo.Position));
		IDamageableHooks.ignoreHook = false;

		foreach (var client in MultiMogulBase.serverManager.connectedClients) {
			if (client.Key == connectionData.connection)
				continue;

			RawPacket.Send(takeDamage, client.Key, SendType.Unreliable, true);
		}
	}

	[Networkable(PacketType.OnRPCMessage, "OnTakeDamage", false)]
	public static void CL_OnTakeDamage(ConnectionManager localConnection, RawPacket receivedPacket) {
		MMLog.Log("received take damage", LogTypes.ControlFlow);

		OnTakeDamage takeDamage = receivedPacket.Read<OnTakeDamage>();
		if (takeDamage == null) {
			MMLog.LogWarning("received invalid take damage packet");
			return;
		}

		GameObject networkedObject = NetworkedObjectRegistry.GetFromGUIDHash(takeDamage.victimGuid);
		if (networkedObject == null) {
			MMLog.LogWarning($"received non networked victim, possibly a networking error, notify the developers! | {takeDamage.victimGuid}");
			return;
		}

		IDamageable damageableComponent = networkedObject.GetComponent<IDamageable>();
		if (damageableComponent == null) {
			MMLog.LogWarning("received networked object doesn't have a IDamageable component!");
			return;
		}

		IDamageableHooks.ignoreHook = true;
		damageableComponent.TakeDamage(new DamageInfo(takeDamage.damageInfo.Damage, takeDamage.damageInfo.DamageType, takeDamage.damageInfo.Position));
		IDamageableHooks.ignoreHook = false;
	}

	public static void TakeDamage(MonoBehaviour instance, DamageInfo damageInfo) {
		NetworkedObject networkedObject = NetworkedObjectRegistry.Find(instance);
		if (networkedObject == null) {
			MMLog.LogWarning("something that isn't networked took damage! report this inconsistency to the developers!");
			MMLog.LogWarning($"{instance.name} attempted to take damage");
			return;
		}

		CustomDamageInfo customDamageInfo = new CustomDamageInfo {
			Damage = damageInfo.Damage,
			DamageType = damageInfo.DamageType,
			Position = damageInfo.Position,
		};

		MMLog.Log($"sent take damage packet to other clients for {instance.name} with id: {networkedObject.GetHash()}");
		if (ServerManager.IsHosting()) {
			foreach (var client in MultiMogulBase.serverManager.connectedClients) {
				RawPacket.Send(new OnTakeDamage {
					damageInfo = customDamageInfo,
					victimGuid = networkedObject.GetHash(),
				}, client.Key, SendType.Unreliable, true);
			}
		}
		else if (ClientManager.IsConnected()) {
			RawPacket.Send(new OnTakeDamage {
				damageInfo = customDamageInfo,
				victimGuid = networkedObject.GetHash(),
			}, ClientManager.GetConnection(), SendType.Unreliable, true);
		}
	}
}
