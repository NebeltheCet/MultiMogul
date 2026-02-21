using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.Utilities;

public class PacketCounter { // handles packet counting for incoming packets, to prevent packet spamming
	public const int GlobalPackets = -1;
	private class PacketInfo {
		public float arriveTime = 0f;
	}

	private readonly Dictionary<int, List<PacketInfo>> _packetCounts = [];

	public int GetPacketCount(int packetHash = GlobalPackets) {
		if (!this._packetCounts.TryGetValue(packetHash, out List<PacketInfo> packets))
			return 0;

		return packets.Count;
	}

	public void IncrementCount(int packetHash = GlobalPackets) {
		float currentTime = Time.realtimeSinceStartup;
		foreach (var list in this._packetCounts.Values) {
			list.RemoveAll(p => (currentTime - p.arriveTime) > 1f);
		}

		if (!this._packetCounts.ContainsKey(packetHash)) {
			this._packetCounts[packetHash] = [];
		}

		this._packetCounts[packetHash].Add(new PacketInfo {
			arriveTime = currentTime
		});

		if (packetHash != GlobalPackets) { // also increment global packet count 
			this.IncrementCount(GlobalPackets);
		}
	} 
}
