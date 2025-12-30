using BepInEx;
using BepInEx.Configuration;
using MultiMogul.Utilities;
using System.IO;

namespace MultiMogul.Config;

public static class Configuration {
	private static ConfigFile configFile;

	private static ConfigEntry<uint> appId;

	public static uint AppId => appId.Value;

	public static void Init() {
		configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "MultiMogul.cfg"), true);
		appId = configFile.Bind("Steam", "AppId", 480u, "decides the app id that will be used to set up the steam context(change to 480 to play with people that dont own the game)");

		MMLog.Log("initialized Configuration context");
	}
}
