using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace YetAnotherLethalLibrary;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class MyPlugin : BaseUnityPlugin
{
    public static readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
    public static MyPlugin instance;
    public ManualLogSource logSource => Logger;
    private void Awake()
    {
        instance = this;
        logSource.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        _harmony.PatchAll(typeof(GameManager));
        AudioManager.Initialize();
    }
}

