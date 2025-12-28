using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MultiMogul.MultiMogul;
using UnityEngine;

namespace MultiMogul;

/*
TODO:

keep track of separate inventories per player
Dropping of Items
register guid's for ingame instantiated objects and network them

Bonus TODO:
certain objects are not affected by grabbing(ex. Carts and the hat), network them.

BUGS:
quest ui jitters when receiving a quest update
pickup quests dont get updated when the client does them
older save files are broken on load, implement alternative save version for multiplayer and let it convert the old save files to our own structure
*/

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MultiMogulBase : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public GameObject steamObject;
    public GameObject threadDispatcherObject;
    public static SteamManager steamManager;
    public static ClientManager clientManager;
    public static ServerManager serverManager;

    private void Awake() {
        this.steamObject = new GameObject($"{MyPluginInfo.PLUGIN_NAME}_SteamManager");
        steamManager = this.steamObject.AddComponent<SteamManager>();
        clientManager = this.steamObject.AddComponent<ClientManager>();
        serverManager = this.steamObject.AddComponent<ServerManager>();
        DontDestroyOnLoad(this.steamObject);

        this.threadDispatcherObject = new GameObject($"{MyPluginInfo.PLUGIN_NAME}_ThreadDispatcher");
        this.threadDispatcherObject.AddComponent<ThreadDispatcher>();
        DontDestroyOnLoad(this.threadDispatcherObject);

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        Logger = base.Logger;
    }

    private void Start() {
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[v{MyPluginInfo.PLUGIN_VERSION}] finished initializing");
    }

    private void Update()
    {

    }

    private void OnApplicationQuit()
    {
        serverManager.StopServer();
        clientManager.Disconnect();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} is shutting down");
    }
}
