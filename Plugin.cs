using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace VeeTaikoCrack;

[BepInPlugin("com.veetaikocrack.unioncrax", "VeeTaikoCrack", "1.0.0")]
public class Plugin : BasePlugin
{
    public const string PLUGIN_GUID = "com.veetaikocrack.unioncrax";
    public const string PLUGIN_NAME = "VeeTaikoCrack";
    public const string PLUGIN_VERSION = "1.0.0";
    
    internal static new ManualLogSource Log;
    private Harmony _harmony;

    public override void Load()
    {
        // Plugin startup logic
        Log = base.Log;
        Log.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        
        // Initialize Harmony patches
        _harmony = new Harmony(PLUGIN_GUID);
        _harmony.PatchAll();
        
        Log.LogInfo("Harmony patches applied successfully!");
    }
}
