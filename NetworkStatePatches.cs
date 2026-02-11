using System;
using HarmonyLib;
using Scripts.EnsoGame.Network;

namespace VeeTaikoCrack
{
    [HarmonyPatch]
    public static class NetworkStatePatches
    {
        private static bool _networkInitialized = false;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "SetupOnline")]
        public static bool SetupOnline_Prefix(EnsoGameManager __instance, string sceneName)
        {
            Plugin.Log.LogInfo($"EnsoGameManager.SetupOnline called for scene: {sceneName}");
            
            try
            {
                _networkInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in SetupOnline: {ex.Message}");
                _networkInitialized = true;
                return false;
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "TeardownOnline")]
        public static bool TeardownOnline_Prefix()
        {
            Plugin.Log.LogInfo("EnsoGameManager.TeardownOnline called");
            _networkInitialized = false;
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "ProcPreparingOnline")]
        public static bool ProcPreparingOnline_Prefix()
        {
            Plugin.Log.LogInfo("EnsoGameManager.ProcPreparingOnline - bypassing network preparation");
            
            if (!_networkInitialized)
            {
                return false;
            }
            
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "ProcExecOnline")]
        public static bool ProcExecOnline_Prefix()
        {
            Plugin.Log.LogInfo("EnsoGameManager.ProcExecOnline - handling online gameplay");
            return true;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "CheckEnsoEndOnline")]
        public static bool CheckEnsoEndOnline_Prefix()
        {
            Plugin.Log.LogInfo("EnsoGameManager.CheckEnsoEndOnline - checking song end");
            return true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoGameManager), "IsWaitEnsoEndOnline")]
        public static void IsWaitEnsoEndOnline_Postfix(ref bool __result)
        {
            if (!_networkInitialized)
            {
                __result = false;
                Plugin.Log.LogInfo("IsWaitEnsoEndOnline - not waiting (network not initialized)");
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoPlayingParameter), "IsNetworkError", MethodType.Getter)]
        public static void IsNetworkError_Getter_Postfix(ref bool __result)
        {
            __result = false;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoPlayingParameter), "IsOnlineMode", MethodType.Getter)]
        public static void IsOnlineMode_Getter_Postfix(ref bool __result)
        {
            __result = _networkInitialized;
            Plugin.Log.LogInfo($"IsOnlineMode - returning: {__result}");
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoPlayingParameter), "networkGameMode", MethodType.Getter)]
        public static void NetworkGameMode_Getter_Postfix(ref NetworkGameMode __result)
        {
            Plugin.Log.LogInfo($"NetworkGameMode getter - current mode: {__result}");
        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OnpuPlayer), "ProcessForOnlineRankMatch")]
        public static bool ProcessForOnlineRankMatch_Prefix(OnpuPlayer __instance, GameDrawInfo drawInfo, OnpuBase onpu)
        {
            Plugin.Log.LogInfo("OnpuPlayer.ProcessForOnlineRankMatch - handling ranked match note");
            return true; // Allow original method
        }
        */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkHeartEffects), "Update")]
        public static bool NetworkHeartEffects_Update_Prefix()
        {
            return true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NXNetworkManager), "ConnectivityTestInterval", MethodType.Getter)]
        public static void ConnectivityTestInterval_Postfix(ref int __result)
        {
            __result = 60000;
            Plugin.Log.LogInfo($"NXNetworkManager.ConnectivityTestInterval set to: {__result}ms");
        }
        public static void ResetNetworkState()
        {
            _networkInitialized = false;
            Plugin.Log.LogInfo("Network state reset");
        }
    }
}
