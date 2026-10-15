using System;
using System.Collections;
using UnityEngine;

namespace MultiMogul.Utilities;

public class CoroutineTask {
	public bool IsRunning { get; private set; }
	public bool IsCompleted { get; private set; }

	public Coroutine Coroutine { get; private set; }

	private readonly MonoBehaviour runner;

	public CoroutineTask(MonoBehaviour runner) {
		this.runner = runner;
	}

	public void Start(IEnumerator routine, Action onComplete = null) {
		if (this.IsRunning)
			throw new InvalidOperationException("this task is already running.");

		this.IsRunning = true;
		this.IsCompleted = false;

		this.Coroutine = this.runner.StartCoroutine(this.Run(routine, onComplete));
	}

	private IEnumerator Run(IEnumerator routine, Action onComplete) {
		try {
			yield return this.runner.StartCoroutine(routine);
		}
		finally {
			this.IsRunning = false;
			this.IsCompleted = true;
			this.Coroutine = null;
		}

		onComplete?.Invoke();
	}
}
