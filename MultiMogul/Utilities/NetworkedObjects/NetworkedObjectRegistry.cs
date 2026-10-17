using System.Collections.Generic;
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

	static void Assign(NetworkedObject networkedObject, string guid) {
		Unregister(networkedObject);

		networkedObject.guid = guid;
		networkedObject.GenerateGUIDHash();

		ulong hash = networkedObject.GetHash();
		WarnOnCollision(networkedObject, hash);

		hashDictionary[hash] = networkedObject;
	}

	static void WarnOnCollision(NetworkedObject networkedObject, ulong hash) {
		if (!hashDictionary.TryGetValue(hash, out NetworkedObject other))
			return;

		if (other == null)
			return;

		if (ReferenceEquals(other, networkedObject))
			return;

		MMLog.LogWarning($"guid hash collision between \"{networkedObject.name}\" and \"{other.name}\" (hash {hash})");
	}

	static string ResolveGuid(NetworkedObject networkedObject, string requestedGuid) {
		if (!string.IsNullOrEmpty(requestedGuid))
			return requestedGuid;

		if (!string.IsNullOrEmpty(networkedObject.guid))
			return networkedObject.guid;

		return GenerateUniqueGUID();
	}

	static string GenerateUniqueGUID() {
		while (true) {
			string guid = System.Guid.NewGuid().ToString();
			if (!IsHashInUse(NetworkedObject.GenerateHash(guid)))
				return guid;
		}
	}

	static bool IsHashInUse(ulong hash) {
		if (!hashDictionary.TryGetValue(hash, out NetworkedObject existing))
			return false;

		return existing != null;
	}

	public static NetworkedObject Register(Object unityObject, string guid = null) {
		GameObject gameObject = ToGameObject(unityObject);
		if (gameObject == null) {
			MMLog.LogWarning("NetworkedObjectRegistry.Register called with a null object!");
			return null;
		}

		if (!gameObject.TryGetComponent(out NetworkedObject networkedObject)) {
			networkedObject = gameObject.AddComponent<NetworkedObject>();
		}
		else {
			MMLog.Log($"added NetworkedObject component to \"{networkedObject.name}\" with hash: {networkedObject.GetHash()}", LogTypes.ControlFlow | LogTypes.Debug);
		}

		Assign(networkedObject, ResolveGuid(networkedObject, guid));
		return networkedObject;
	}

	public static NetworkedObject Find(Object unityObject) {
		GameObject gameObject = ToGameObject(unityObject);
		if (gameObject == null) {
			MMLog.LogWarning("NetworkedObjectRegistry lookup called with a null object!");
			return null;
		}

		if (!gameObject.TryGetComponent(out NetworkedObject networkedObject)) {
			//MMLog.LogWarning($"object \"{gameObject.name}\" has no NetworkedObject component!");
			return null;
		}

		return networkedObject;
	}

	public static void Unregister(NetworkedObject networkedObject) {
		ulong hash = networkedObject.GetHash();
		if (!hashDictionary.TryGetValue(hash, out NetworkedObject current))
			return;

		if (!ReferenceEquals(current, networkedObject))
			return;

		hashDictionary.Remove(hash);
	}

	public static void Clear() {
		hashDictionary.Clear();
	}
	public static GameObject GetFromGUIDHash(ulong guidHash) {
		if (!hashDictionary.TryGetValue(guidHash, out NetworkedObject networkedObject))
			return null;

		if (networkedObject == null)
			return null;

		return networkedObject.gameObject;
	}
}