using System;
using System.Collections.Concurrent;
using UnityEngine;

public class ThreadDispatcher : MonoBehaviour {
    private static readonly ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();

    private void Update() {
        while (actionQueue.Count > 0) {
            if (actionQueue.TryDequeue(out Action action)) {
                action.Invoke();
            }
        }
    }

    public static void Enqueue(Action action) {
        actionQueue.Enqueue(action);
    }
}