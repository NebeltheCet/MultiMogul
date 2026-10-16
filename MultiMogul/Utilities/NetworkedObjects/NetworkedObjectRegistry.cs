
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;

namespace MultiMogul.Utilities.NetworkedObjects;

public class NetworkedObjectRegistry {
	static readonly Dictionary<ulong, NetworkedObject> hashDictionary = [];

	static GameObject ToGameObject(Object unityObject) {
		if (unityObject == null)
			return null;

		return unityObject switch {
			GameObject gameObject => gameObject,
			Component component => component.gameObject,
			_ => null,
		};
	}

	static void Assign(NetworkedObject networked, string guid) {
		Unregister(networked);

		networked.guid = guid;
		networked.GenerateGUIDHash();

		if (hashDictionary.TryGetValue(networked.GetHash(), out NetworkedObject other) && other != null && !ReferenceEquals(other, networked)) {
			MMLog.LogWarning($"guid hash collision between \"{networked.name}\" and \"{other.name}\" (hash {networked.GetHash()}).");
		}

		hashDictionary[networked.GetHash()] = networked;
		MMLog.Log($"added NetworkedObject component to \"{networked.name}\" with hash: {networked.GetHash()}");
	}

	private static string GenerateUniqueGUID() {
		while (true) {
			string guid = System.Guid.NewGuid().ToString();
			if (hashDictionary.TryGetValue(NetworkedObject.GenerateHash(guid), out NetworkedObject existing))
				continue;

			if (existing != null)
				continue;

			return guid;
		}
	}

	public static NetworkedObject Register(Object unityObject, string guid = null) {
		NetworkedObject networkedObject = Find(unityObject);
		if (networkedObject)
			return networkedObject; // we already have a networked object assigned.

		GameObject gameObject = ToGameObject(unityObject);
		if (gameObject == null) {
			MMLog.LogWarning("NetworkedObjectRegistry.Register called with a null object!");
			return null;
		}

		networkedObject = gameObject.AddComponent<NetworkedObject>();
		string resolvedGuid = guid;
		if (string.IsNullOrEmpty(resolvedGuid) && !string.IsNullOrEmpty(networkedObject.guid)) {
			resolvedGuid = networkedObject.guid;
		}
		else {
			resolvedGuid = GenerateUniqueGUID();
		}

		Assign(networkedObject, resolvedGuid);
		return networkedObject;
	}

	private static NetworkedObject Find(Object unityObject) {
		GameObject gameObject = ToGameObject(unityObject);
		if (gameObject == null) {
			MMLog.LogWarning("NetworkedObjectRegistry lookup called with a null object!");
			return null;
		}

		if (!gameObject.TryGetComponent(out NetworkedObject networked)) {
			//MMLog.LogWarning($"object \"{gameObject.name}\" has no NetworkedObject component!");
			return null;
		}

		return networked;
	}

	public static void Unregister(NetworkedObject networked) {
		if (!hashDictionary.TryGetValue(networked.GetHash(), out NetworkedObject current))
			return;

		if (!ReferenceEquals(current, networked))
			return;

		hashDictionary.Remove(networked.GetHash());
	}

	public static void Clear() {
		hashDictionary.Clear();
	}
	public static GameObject GetFromGUID(ulong guidHash) {
		return hashDictionary.TryGetValue(guidHash, out NetworkedObject networked) && networked != null ? networked.gameObject : null;
	}

	public static string GetGUIDFromInstance(Object unityObject) {
		return Find(unityObject)?.guid;
	}

	public static ulong GetGUIDHashFromInstance(Object unityObject) {
		return Find(unityObject)?.GetHash() ?? 0;
	}
}