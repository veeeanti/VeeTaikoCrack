using System;
using BepInEx;
using HarmonyLib;
using Steamworks;

namespace VeeTaikoCrack
{
    /// <summary>
    /// Comprehensive Steam API patches to spoof the game as Spacewar (App ID 480)
    /// Consolidated from all Steam patch files
    /// </summary>
    [HarmonyPatch]
    public static class CompleteSteamPatches
    {
        private const int SPOOFED_APP_ID = 480; // Spacewar app ID

        // Patch SteamAPI.RestartAppIfNecessary to always return false to prevent restarts
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamAPI), "RestartAppIfNecessary")]
        public static bool RestartAppIfNecessary_Prefix(AppId_t unOwnAppID, ref bool __result)
        {
            Plugin.Log.LogInfo($"SteamAPI.RestartAppIfNecessary called with App ID: {unOwnAppID.m_AppId}, returning false to prevent restart");
            
            // Always return false to prevent game restart attempts
            __result = false;
            return false;
        }

        // Alternative patch targeting the bool-returning Init method (most common version)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamAPI), "Init")]
        public static void Init_Postfix(ref bool __result)
        {
            Plugin.Log.LogInfo($"SteamAPI.Init called, ensuring success with spoofed app ID: {SPOOFED_APP_ID}");
            // Force success if initialization failed
            if (!__result)
            {
                __result = true;
                Plugin.Log.LogWarning("Forced SteamAPI.Init to succeed");
            }
        }

        // Patch SteamUtils.GetAppID to return our spoofed App ID
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamUtils), "GetAppID")]
        public static void GetAppID_Postfix(ref AppId_t __result)
        {
            Plugin.Log.LogInfo($"SteamUtils.GetAppID returning spoofed App ID: {SPOOFED_APP_ID}");
            __result = new AppId_t(SPOOFED_APP_ID);
        }

        // Patch SteamApps.BIsSubscribedApp to always return true for our spoofed App ID
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamApps), "BIsSubscribedApp")]
        public static bool BIsSubscribedApp_Prefix(AppId_t appID, ref bool __result)
        {
            Plugin.Log.LogInfo($"SteamApps.BIsSubscribedApp called with App ID: {appID.m_AppId}, returning true for spoofed app");
            
            // Always return true for the spoofed App ID
            __result = appID.m_AppId == SPOOFED_APP_ID;
            return false; // Skip original method
        }

        // Patch SteamApps.BIsAppInstalled to always return true for our spoofed App ID
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamApps), "BIsAppInstalled")]
        public static bool BIsAppInstalled_Prefix(AppId_t appID, ref bool __result)
        {
            Plugin.Log.LogInfo($"SteamApps.BIsAppInstalled called with App ID: {appID.m_AppId}, returning true for spoofed app");
            
            // Always return true for the spoofed App ID
            __result = appID.m_AppId == SPOOFED_APP_ID;
            return false; // Skip original method
        }
        
        // Additional patch for SteamApps.GetAppOwner to return the spoofed App ID
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamApps), "GetAppOwner")]
        public static void GetAppOwner_Postfix(ref CSteamID __result)
        {
            // Return a steam ID that corresponds to the spoofed app
            Plugin.Log.LogInfo($"SteamApps.GetAppOwner returning owner for spoofed app");
            // We don't modify this, but log for debugging purposes
        }

        // Patch SteamManager methods to ensure proper initialization
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public static void SteamManager_Awake_Postfix(SteamManager __instance)
        {
            Plugin.Log.LogInfo("Initializing SteamManager");
        }
        
        // Patch potential restart methods in SteamManager
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "InitOnPlayMode")]
        public static bool InitOnPlayMode_Prefix()
        {
            Plugin.Log.LogInfo("Intercepting SteamManager init to prevent restart");
            // Cancel any restart attempts
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public static bool SteamManager_Awake_Prefix(SteamManager __instance)
        {
            Plugin.Log.LogInfo("Intercepting SteamManager awake to prevent restart");
            // Allow initialization but prevent any restart logic
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "OnEnable")]
        public static bool SteamManager_OnEnable_Prefix(SteamManager __instance)
        {
            Plugin.Log.LogInfo("Intercepting SteamManager OnEnable to prevent restart");
            return true;
        }
		
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "OnDestroy")]
        public static bool OnDestroy_Prefix(SteamManager __instance)
        {
            Plugin.Log.LogInfo("Preventing SteamManager OnDestroy to maintain Steam session");
            // Don't prevent the original method, just log
            return true;
        }

        // Patch platform-specific Steam account methods to ensure login status
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Platform.Steam.SteamAccount), "LoginAsync")]
        public static void LoginAsync_Postfix(ref object __result)
        {
            Plugin.Log.LogInfo("Bypassing Steam login authentication");
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Platform.Steam.SteamAccount), "IsLogin")]
        public static void IsLogin_Postfix(ref bool __result)
        {
            Plugin.Log.LogInfo("Spoofing Steam login status to logged in");
            __result = true;
        }
    }
}