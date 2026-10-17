using MultiMogul.Client;
using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Utilities;
using MultiMogul.Utilities.NetworkedObjects;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiMogul.Entities;
public class Ores {
	// this code is a partial rebuild from SavingLoadingManager.LoadGame,
	// it may break for major changes to the ore instantiation!
	private static void CreateOrePiece(CustomOrePieceEntry orePieceEntry) {
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

	private static void CreateOrePieceList(List<CustomOrePieceEntry> orePieceEntries) {
		if (orePieceEntries.Count <= 0) {
			MMLog.LogWarning("received ore piece list has no entries, is this possibly a bug?");
			return;
		}

		foreach (CustomOrePieceEntry orePieceEntry in orePieceEntries) {
			CreateOrePiece(orePieceEntry);
		}
	}

	// this code may break for major changes to the static ore breakables!
	private static void CreateOreNodeList(List<CustomOreNodeEntry> oreNodeEntries) {
		if (oreNodeEntries.Count <= 0) {
			//MMLog.LogWarning("received ore piece list has no entries, is this possibly a bug?");
			return;
		}

		// create a lookup dictionary through positions
		Dictionary<Vector3, CustomOreNodeEntry> entryByPosition = [];
		foreach (CustomOreNodeEntry entry in oreNodeEntries) {
			if (entryByPosition.ContainsKey(entry.Position)) {
				MMLog.LogWarning($"two ore node entries share the position {entry.Position}! notify the developers!");
				continue;
			}

			entryByPosition.Add(entry.Position, entry);
		}

		var matched = new HashSet<Vector3>();
		foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveLoadableStaticBreakable>()) {
			if (saveLoadableStaticBreakable is not Component breakableComponent)
				continue;

			Vector3 position = saveLoadableStaticBreakable.GetPosition();
			if (!entryByPosition.TryGetValue(position, out CustomOreNodeEntry entry)) {
				MMLog.LogWarning($"no host entry for the static breakable at {position}");
				continue;
			}

			NetworkedObjectRegistry.Register(breakableComponent, entry.guid);
			matched.Add(position);
		}

		foreach (CustomOreNodeEntry entry in oreNodeEntries) {
			if (matched.Contains(entry.Position))
				continue;

			MMLog.LogWarning($"host ore node at {entry.Position} has no matching object on this client");
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
			CreateOrePieceList(oreReplication.orePieceEntries);
			CreateOreNodeList(oreReplication.oreNodeEntries);

			// static breakables are only networked during either of these conditions
			ReflectionField<List<Vector3>> destroyedStaticBreakablePositions = ReflectionHelper.GetField<List<Vector3>>(Singleton<SavingLoadingManager>.Instance, "_destroyedStaticBreakablePositions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (oreReplication.destroyedStaticBreakablePositions.Count > 0 || oreReplication.oreNodeEntries.Count > 0) {
				destroyedStaticBreakablePositions.Value = oreReplication.destroyedStaticBreakablePositions;
				foreach (ISaveLoadableStaticBreakable saveLoadableStaticBreakable in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveLoadableStaticBreakable>()) {
					if (!destroyedStaticBreakablePositions.Value.Contains(saveLoadableStaticBreakable.GetPosition()))
						continue;

					saveLoadableStaticBreakable.DestroyFromLoading();
				}
			}
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
