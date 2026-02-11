using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.IL2CPP;
using HarmonyLib;
using Steamworks;

namespace VeeTaikoCrack;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BepInEx.IL2CPP.BasePlugin
{
    internal static new ManualLogSource Log;
    private Harmony _harmony;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();
        
        Log.LogInfo("Harmony patches applied successfully!");
    }
}
