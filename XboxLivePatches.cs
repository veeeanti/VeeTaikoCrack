using System;
using HarmonyLib;
using PartyXBLCSharpSDK;

namespace VeeTaikoCrack
{
    [HarmonyPatch]
    public static class XboxLivePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), MethodType.Constructor)]
        public static bool XboxLiveOnlineManager_Constructor_Prefix()
        {
            Plugin.Log.LogInfo("Bypassing XboxLiveOnlineManager constructor - Xbox Live not required");
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "Initialize")]
        public static bool Initialize_Prefix(ref bool __result)
        {
            Plugin.Log.LogInfo("XboxLiveOnlineManager.Initialize bypassed - returning success");
            __result = true;
            return false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "IsLoggedIn")]
        public static void IsLoggedIn_Postfix(ref bool __result)
        {
            Plugin.Log.LogInfo("XboxLiveOnlineManager.IsLoggedIn spoofed to true");
            __result = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "GetUserId")]
        public static void GetUserId_Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
            {
                __result = $"FakeXboxUser_{UnityEngine.SystemInfo.deviceUniqueIdentifier}";
                Plugin.Log.LogInfo($"XboxLiveOnlineManager.GetUserId spoofed to: {__result}");
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "GetToken")]
        public static void GetToken_Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
            {
                __result = $"FakeXboxToken_{Guid.NewGuid()}";
                Plugin.Log.LogInfo($"XboxLiveOnlineManager.GetToken spoofed to: {__result}");
            }
        }
    }
}
