using System;
using System.Text;
using UnityEngine;

namespace MultiMogul.Utilities.NetworkedObjects;

public class NetworkedObject : MonoBehaviour {
	public string guid = "";
	private ulong guidHash = 0;

	public static ulong GenerateHash(string str) {
		const ulong offsetBasis = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;

		ulong hash = offsetBasis;
		byte[] bytes = Encoding.UTF8.GetBytes(str);

		foreach (byte b in bytes) {
			hash ^= b;
			hash *= prime;
		}

		return hash;
	}

	public void GenerateGUIDHash() {
		this.guidHash = GenerateHash(this.guid);
	}

	public ulong GetHash() {
		if (this.guidHash != 0)
			return this.guidHash;

		this.GenerateGUIDHash();
		return this.guidHash;
	}

	private void OnDestroy() {
		NetworkedObjectRegistry.Unregister(this);
	}
}
