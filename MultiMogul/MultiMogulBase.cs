// these global usings are to discard any types that we definitely do not need and just mess up our auto completion
global using DISCARD_CLASS = Unity.VisualScripting.This;

using BepInEx;
using HarmonyLib;
using MultiMogul.Client;
using MultiMogul.Config;
using MultiMogul.Networking;
using MultiMogul.Networking.Packets;
using MultiMogul.Server;
using MultiMogul.Steam;
using MultiMogul.Utilities;
using System.Linq;

namespace MultiMogul;

[BepInPlugin("org.nebelthecet.minemogul.multimogul", "MultiMogul", "0.1.0")]
public class MultiMogulBase : BaseUnityPlugin {
	internal static ServerManager serverManager = null;
	internal static ClientManager clientManager = null;
	internal static Harmony multiMogulHarmony = null;

	private void Awake() {
		MMLog.Init(LogTypes.Packets | LogTypes.Debug | LogTypes.ControlFlow);
		Configuration.Init();

		ThreadDispatcher.Init();
		SteamManager.Init();

		ServerManager.Init();
		ClientManager.Init();

		multiMogulHarmony = new Harmony(this.Info.Metadata.GUID);
		multiMogulHarmony.PatchAll();
	}

	private void Start() {
		// this is in the start method to ensure that any mod that implements their own packets had their assembly loaded.
		Networkable.Register();

		// this should ideally also be called here
		// well, this is kinda messy, but this allows our static constructors in our derived packet classes to run, even when the type isn't "touched"
		var packetTypes = typeof(RawPacket).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(RawPacket)) && !t.IsAbstract);
		foreach (var type in packetTypes) {
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
		}
	}

	private void OnApplicationQuit() {
		ClientManager.Shutdown();
		ServerManager.Shutdown();

		SteamManager.Shutdown();
		MMLog.Shutdown();
	}

	public static bool IsAuthoritative() {
		if (serverManager == null)
			return false;

		return serverManager.currentLobby.Id != 0;
	}
}
