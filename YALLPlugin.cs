using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace YetAnotherLethalLibrary;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class YALLPlugin : BaseUnityPlugin
{
    internal static readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
    internal ManualLogSource logSource => Logger;
    public static YALLPlugin instance;
    private void Awake()
    {
        instance = this;
        logSource.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        _harmony.PatchAll(typeof(GameManager));
        AudioManager.Initialize();
    }
}

