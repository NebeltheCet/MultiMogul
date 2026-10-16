using MultiMogul.Client;
using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Utilities;
using MultiMogul.Utilities.NetworkedObjects;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.Entities;
public class OrePieces {
	// this code is a partial rebuild from SavingLoadingManager.LoadGame,
	// it may break for major changes to the ore instantiation!
	private static void CreateOre(CustomOrePieceEntry orePieceEntry) {
		if (!Vector3Utils.IsValid(orePieceEntry.Position)) {
			MMLog.Log($"Loading - OrePiece ({orePieceEntry.ResourceType} {orePieceEntry.PieceType}) has invalid position, skipping");
			return;
		}

		OrePiece orePiecePrefab = Singleton<SavingLoadingManager>.Instance.GetOrePiecePrefab(orePieceEntry.ResourceType, orePieceEntry.PieceType, orePieceEntry.PolishedPercent > 0.95f);
		if (orePiecePrefab != null) {
			OrePiece orePiece = Object.Instantiate<OrePiece>(orePiecePrefab, orePieceEntry.Position, Quaternion.Euler(orePieceEntry.Rotation));
			NetworkedObjectRegistry.Register(orePiece, orePieceEntry.guid);

			orePiece.gameObject.name = orePiecePrefab.gameObject.name + " [Loaded]";
			//if (saveFile.SaveVersion > 3) { // we don't handle old save versions!
			orePiece.UseRandomScale = false;
			orePiece.transform.localScale = orePieceEntry.Scale;
			orePiece.ApplySelectedRenderMesh(orePieceEntry.MeshID);
			//}

			if (orePiece.PolishedPercent != 1f) {
				orePiece.PolishedPercent = orePieceEntry.PolishedPercent;
			}

			if (orePiece.PossibleSievedPrefabs.Count > 0) {
				orePiece.SievePercent = 0.8f;
			}
		}
	}

	private static void CreateOreList(List<CustomOrePieceEntry> orePieceEntries) {
		if (orePieceEntries.Count <= 0) {
			MMLog.LogWarning("received ore piece list has no entries, is this possibly a bug?");
			return;
		}

		foreach (CustomOrePieceEntry orePieceEntry in orePieceEntries) {
			CreateOre(orePieceEntry);
		}
	}

	#region Packets
	[Networkable(PacketType.OnOreReplication, uint.MaxValue, 256, false)]
	public static void OnOreReplication(ConnectionManager localConnection, RawPacket receivedPacket) {
		MMLog.Log("received ore replication", LogTypes.ControlFlow);
		OnOreReplication oreReplication = receivedPacket.Read<OnOreReplication>();
		if (oreReplication == null) {
			MMLog.LogWarning("received invalid ore replication packet");
			return;
		}

		System.Action action = () => {
			CreateOreList(oreReplication.orePieceEntries);
		};

		if (!ClientManager.loadSaveTask.IsCompleted) {
			ClientManager.loadSaveTask.AddCompletionCallback(action);
		}
		else {
			action();
		}
	}
	#endregion
}
