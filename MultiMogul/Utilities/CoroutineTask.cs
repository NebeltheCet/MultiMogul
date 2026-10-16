using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiMogul.Utilities;

public class CoroutineTask {
	public bool IsRunning { get; private set; }
	public bool IsCompleted { get; private set; }

	public Coroutine Coroutine { get; private set; }

	private readonly MonoBehaviour runner;

	private List<Action> _onCompleteCallbacks = [];

	public CoroutineTask(MonoBehaviour runner) {
		this.runner = runner;
	}

	public void AddCompletionCallback(Action action) {
		this._onCompleteCallbacks.Add(action);
	}

	public void Start(IEnumerator routine) {
		if (this.IsRunning)
			throw new InvalidOperationException("this task is already running.");

		this.IsRunning = true;
		this.IsCompleted = false;

		this.Coroutine = this.runner.StartCoroutine(this.Run(routine));
	}

	private IEnumerator Run(IEnumerator routine) {
		try {
			yield return this.runner.StartCoroutine(routine);
		}
		finally {
			this.IsRunning = false;
			this.IsCompleted = true;
			this.Coroutine = null;
		}

		foreach (Action action in this._onCompleteCallbacks) {
			action.Invoke();
		}
	}
}
