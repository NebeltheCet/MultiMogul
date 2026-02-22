using BepInEx.Logging;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MultiMogul.Utilities;

// the LogTypes enum is there to signal which logs should be shown or not in the mod compilation context
public enum LogTypes {
	Default = 1 << 0,
	Packets = 1 << 1,
	Debug = 1 << 2,
	ControlFlow = 1 << 3
}

// MultiMogul Logging Wrapper
internal class MMLog {
	private static ManualLogSource Logger;
	private static LogTypes logTypes;

	public static void Init(LogTypes logType = LogTypes.Default) {
		if ((logType & LogTypes.Default) == 0) {
			logType |= LogTypes.Default;
		}

		Logger = BepInEx.Logging.Logger.CreateLogSource("MMLog");
		logTypes = logType;
	}

	public static void Shutdown() {
		BepInEx.Logging.Logger.Sources.Remove(Logger);
		Logger = null;
	}

	private static string GetCallerFullName() {
		var stackTrace = new StackTrace();
		var frame = stackTrace.GetFrame(2);
		var method = frame.GetMethod();

		return $"{method.DeclaringType.Name}.{method.Name}";
	}

	public static void Log(string message, LogTypes logType = LogTypes.Default) {
		if ((logType & LogTypes.Default) == 0) {
			logType |= LogTypes.Default;
		}

		if ((logTypes & logType) == 0)
			return;

		Logger.Log(LogLevel.Info, $"[{GetCallerFullName()}]: {message}");
	}

	public static void LogWarning(string message, LogTypes logType = LogTypes.Default) {
		if ((logType & LogTypes.Default) == 0) {
			logType |= LogTypes.Default;
		}

		if ((logTypes & logType) == 0)
			return;

		Logger.Log(LogLevel.Warning, $"[{GetCallerFullName()}]: {message}");
	}

	public static void LogError(string message, LogTypes logType = LogTypes.Default) {
		if ((logType & LogTypes.Default) == 0) {
			logType |= LogTypes.Default;
		}

		if ((logTypes & logType) == 0)
			return;

		Logger.Log(LogLevel.Error, $"[{GetCallerFullName()}]: {message}");
	}

	public static void LogException(string message, LogTypes logType = LogTypes.Default) {
		if ((logType & LogTypes.Default) == 0) {
			logType |= LogTypes.Default;
		}

		if ((logTypes & logType) == 0)
			return;

		throw new Exception($"[{GetCallerFullName()}]: {message}");
	}
}
