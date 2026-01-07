using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Utilities;

using MultiMogul.Server;
using System;
using System.Collections.Concurrent;
using UnityEngine;

public class ThreadDispatcher : MonoBehaviour {
	private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();

	public static void Init() {
		GameObject gameObject = new GameObject("MM_ThreadDispatcher", [typeof(ThreadDispatcher)]);
		DontDestroyOnLoad(gameObject);

		MMLog.Log("created ThreadDispatcher object", LogTypes.ControlFlow);
		MMLog.Log("finished initializing");
	}

	private void Update() {
		while (actionQueue.Count > 0) {
			if (!actionQueue.TryDequeue(out Action action))
				continue;

			action.Invoke();
		}
	}

	public static void Enqueue(Action action) {
		actionQueue.Enqueue(action);
	}
}