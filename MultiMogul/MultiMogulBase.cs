using BepInEx;
using MultiMogul.Utilities;
using MultiMogul.Config;
using MultiMogul.Steam;

namespace MultiMogul;

[BepInPlugin("org.nebelthecet.minemogul.multimogul", "MultiMogul", "0.1.0")]
public class MultiMogulBase : BaseUnityPlugin {
	private void Awake() {
		MMLog.Init(LogTypes.Packets | LogTypes.Debug | LogTypes.ControlFlow);
		Configuration.Init();

		SteamManager.Init();
	}

	private void OnApplicationQuit() {
		SteamManager.Shutdown();
		MMLog.Shutdown();
	}
}
