using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MultiMogul.Utilities;

// the LogTypes enum is there to signal which logs should be shown or not in the mod compilation context
public enum LogTypes {
	Default = 0,
	Packets = 1,
	Debug = 2,
	ControlFlow = 4
}

// MultiMogul Logging Wrapper
internal class MMLog {
	private static ManualLogSource Logger;
	private static LogTypes logTypes;

	public static void Init(LogTypes logType = LogTypes.Default) {
		Logger = BepInEx.Logging.Logger.CreateLogSource("MMLog");
		logTypes = logType;
	}

	public static void Shutdown() {
		BepInEx.Logging.Logger.Sources.Remove(Logger);
		Logger = null;
	}

	public static void Log(string message, LogTypes logType = LogTypes.Default, [CallerMemberName] string caller = "") {
		if ((logTypes & logType) == 0)
			return;

		Logger.Log(LogLevel.None, $"[{caller}]: {message}");
	}

	public static void LogWarning(string message, LogTypes logType = LogTypes.Default, [CallerMemberName] string caller = "") {
		if ((logTypes & logType) == 0)
			return;

		Logger.Log(LogLevel.Warning, $"[{caller}]: {message}");
	}

	public static void LogError(string message, LogTypes logType = LogTypes.Default, [CallerMemberName] string caller = "") {
		if ((logTypes & logType) == 0)
			return;

		Logger.Log(LogLevel.Error, $"[{caller}]: {message}");
	}

	public static void LogException(string message, LogTypes logType = LogTypes.Default, [CallerMemberName] string caller = "") {
		if ((logTypes & logType) == 0)
			return;

		throw new Exception($"[{caller}]: {message}");
	}
}
