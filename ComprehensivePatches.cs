using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using System;
using UnityEngine;

namespace VeeTaikoCrack
{
    [BepInPlugin("com.vee.taikocrack.comprehensive", "Taiko Comprehensive Crack", "1.0.0")]
    public class ComprehensivePatches : BepInEx.IL2CPP.BasePlugin
    {
        public override void Load()
        {
            Log.LogInfo("Loading Comprehensive Taiko Patches...");
            
            var harmony = new Harmony("com.vee.taikocrack.comprehensive");
            harmony.PatchAll(typeof(ComprehensivePatches));
            
            Log.LogInfo("All comprehensive patches applied successfully!");
        }

        // ============================================
        // DLC / LICENSE CHECKS - AddOnContents
        // ============================================
        
        [HarmonyPatch(typeof(AddOnContents), nameof(AddOnContents.CanPlay))]
        [HarmonyPrefix]
        public static bool CanPlay_Prefix(ref bool __result)
        {
            __result = true;
            return false; // Skip original
        }

        [HarmonyPatch(typeof(AddOnContents), "CanPlay2")]
        [HarmonyPrefix]
        public static bool CanPlay2_Prefix(ref bool __result)
        {
            __result = true;
            return false; // Skip original
        }

        [HarmonyPatch(typeof(AddOnContents), nameof(AddOnContents.IsLicenseChecking))]
        [HarmonyPrefix]
        public static bool IsLicenseChecking_Prefix(ref bool __result)
        {
            __result = false; // Never checking
            return false;
        }

        [HarmonyPatch(typeof(AddOnContents), nameof(AddOnContents.IsAddOnContentsLicnseNetError))]
        [HarmonyPrefix]
        public static bool IsAddOnContentsLicnseNetError_Prefix(ref bool __result)
        {
            __result = false; // No errors
            return false;
        }

        [HarmonyPatch(typeof(AddOnContents), nameof(AddOnContents.StartLicenseCheck))]
        [HarmonyPrefix]
        public static bool StartLicenseCheck_Prefix()
        {
            return false; // Skip license check entirely
        }

        [HarmonyPatch(typeof(AddOnContents), nameof(AddOnContents.EndLicenseCheck))]
        [HarmonyPrefix]
        public static bool EndLicenseCheck_Prefix()
        {
            return false; // Skip
        }

        // ============================================
        // PLAYFAB AUTHENTICATION
        // ============================================
        
        [HarmonyPatch(typeof(PlayFab.PlayFabAuthenticationAPI), "IsEntityLoggedIn")]
        [HarmonyPrefix]
        public static bool IsEntityLoggedIn_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(PlayFab.PlayFabClientAPI), "IsClientLoggedIn")]
        [HarmonyPrefix]
        public static bool IsClientLoggedIn_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        // ============================================
        // NETWORK / ONLINE VALIDATION
        // ============================================
        
        [HarmonyPatch(typeof(EnsoPlayingParameter), "IsNetworkError", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IsNetworkError_Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }

        [HarmonyPatch(typeof(EnsoPlayingParameter), "IsNetworkError", MethodType.Setter)]
        [HarmonyPrefix]
        public static bool IsNetworkError_Setter_Prefix()
        {
            return false; // Prevent setting network error
        }

        // Patch network state checks
        [HarmonyPatch]
        public static class NetworkStatePatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                // Find all IsNetworkPlaying getters
                var type = typeof(Network.MatchEnsoDatas);
                var method = type.GetProperty("IsNetworkPlaying")?.GetGetMethod();
                if (method != null) methods.Add(method);
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                __result = true; // we are playing online
                return true;
            }
        }

        // ============================================
        // SUBSCRIPTION / ENTITLEMENT CHECKS
        // ============================================
        
        [HarmonyPatch(typeof(KpiSaveData), "subscriptionCheckDate", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool SubscriptionCheckDate_Prefix(ref int __result)
        {
            __result = 0; // No subscription check needed
            return false;
        }

        [HarmonyPatch(typeof(KpiSaveData), "isSubscription", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IsSubscription_Prefix(ref bool __result)
        {
            __result = true; // Always subscribed
            return false;
        }

        // ============================================
        // PLATFORM AUTHENTICATION (Steam/Xbox)
        // ============================================
        
        // Steam authentication bypass
        [HarmonyPatch]
        public static class SteamAuthPatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                try
                {
                    // Find Steamworks authentication methods
                    var steamType = Type.GetType("Steamworks.SteamUser, com.rlabrecque.steamworks.net");
                    if (steamType != null)
                    {
                        var loggedOn = steamType.GetMethod("BLoggedOn");
                        if (loggedOn != null) methods.Add(loggedOn);
                    }
                }
                catch { }
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        // Xbox authentication bypass
        [HarmonyPatch]
        public static class XboxAuthPatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                try
                {
                    // Find Xbox authentication methods
                    var xboxType = Type.GetType("Microsoft.Xbox.Services.XboxLiveUser");
                    if (xboxType != null)
                    {
                        var isSignedIn = xboxType.GetProperty("IsSignedIn")?.GetGetMethod();
                        if (isSignedIn != null) methods.Add(isSignedIn);
                    }
                }
                catch { }
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        // ============================================
        // VALIDATION CHECKS
        // ============================================
        
        // Patch all IsValid methods to return true
        [HarmonyPatch]
        public static class IsValidPatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                var types = new[]
                {
                    typeof(CustomizeFavoriteInfo),
                    typeof(DailyMusicsInfo),
                    typeof(DonClothes),
                    typeof(EnsoMode),
                    typeof(EnsoDonChanBandRecordInfo),
                    typeof(EnsoRecordInfo),
                    typeof(EnsoWarRecordInfo),
                    typeof(HiScoreRecordInfo),
                    typeof(MusicInfoEx),
                    typeof(PlayerNamePlateInfo),
                    typeof(PlayHistoryItem),
                    typeof(PlayerInfo),
                    typeof(ShopInfo),
                    typeof(SystemOption),
                    typeof(UnlockInfo),
                    typeof(RankMatchSeasonRecordInfo)
                };

                foreach (var type in types)
                {
                    try
                    {
                        var method = type.GetMethod("IsValid");
                        if (method != null && method.ReturnType == typeof(bool))
                        {
                            methods.Add(method);
                        }
                    }
                    catch { }
                }
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        // ============================================
        // PERMISSION / AUTHORIZATION CHECKS
        // ============================================
        
        [HarmonyPatch]
        public static class PermissionPatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                try
                {
                    // Chat permissions
                    var chatType = Type.GetType("PartyCSharpSDK.SDK");
                    if (chatType != null)
                    {
                        var getPerms = chatType.GetMethod("PartyChatControlGetPermissions");
                        if (getPerms != null) methods.Add(getPerms);
                    }
                }
                catch { }
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref uint __result)
            {
                __result = 0; // Success code
                return false;
            }
        }

        // ============================================
        // ENTITLEMENT CHECKS (PS5/Platform specific)
        // ============================================
        
        [HarmonyPatch]
        public static class EntitlementPatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                try
                {
                    // PS5 entitlement checks
                    var entType = Type.GetType("Platform.PS5.UnifiedEntitlementInfo");
                    if (entType != null)
                    {
                        var activeFlag = entType.GetProperty("ActiveFlag")?.GetGetMethod();
                        if (activeFlag != null) methods.Add(activeFlag);
                    }
                }
                catch { }
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        // ============================================
        // AUTHENTICATION STATE CHECKS
        // ============================================
        
        [HarmonyPatch]
        public static class AuthenticationStatePatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                try
                {
                    // WebSocket authentication
                    var wsType = Type.GetType("WebSocketSharp.Net.WebSockets.WebSocketContext");
                    if (wsType != null)
                    {
                        var isAuth = wsType.GetProperty("IsAuthenticated")?.GetGetMethod();
                        if (isAuth != null) methods.Add(isAuth);
                    }

                    var httpType = Type.GetType("WebSocketSharp.Net.HttpListenerRequest");
                    if (httpType != null)
                    {
                        var isAuth = httpType.GetProperty("IsAuthenticated")?.GetGetMethod();
                        if (isAuth != null) methods.Add(isAuth);
                    }
                }
                catch { }
                
                return methods;
            }

            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        // ============================================
        // RANKED MATCH VALIDATION
        // ============================================
        
        [HarmonyPatch(typeof(RankmatchDataManager), "IsValidAdditionalRankPoint")]
        [HarmonyPrefix]
        public static bool IsValidAdditionalRankPoint_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(RankmatchDataManager), "IsValidAdditionalWinCount")]
        [HarmonyPrefix]
        public static bool IsValidAdditionalWinCount_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(RankmatchDataManager), "IsValidRankPoint")]
        [HarmonyPrefix]
        public static bool IsValidRankPoint_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(RankmatchDataManager), "IsValidWinCount")]
        [HarmonyPrefix]
        public static bool IsValidWinCount_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        // ============================================
        // MUSIC DATA VALIDATION
        // ============================================
        
        [HarmonyPatch(typeof(MusicDataInterface), nameof(MusicDataInterface.CheckBaseScore))]
        [HarmonyPrefix]
        public static bool CheckBaseScore_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        // ============================================
        // PACKED SONG FILE CHECKS
        // ============================================
        
        [HarmonyPatch(typeof(PackedSongUtility), nameof(PackedSongUtility.CheckSongFileExists))]
        [HarmonyPrefix]
        public static bool CheckSongFileExists_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(PackedSongUtility), nameof(PackedSongUtility.CheckPreviewFileExists))]
        [HarmonyPrefix]
        public static bool CheckPreviewFileExists_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        // ============================================
        // PLAY DATA MANAGER DLC CHECK
        // ============================================
        
        [HarmonyPatch(typeof(PlayDataManager), "IsDLCCheckRequired", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool IsDLCCheckRequired_Prefix(ref bool __result)
        {
            __result = false; // No DLC check required
            return false;
        }

        [HarmonyPatch(typeof(PlayDataManager), "IsDLCCheckRequired", MethodType.Setter)]
        [HarmonyPrefix]
        public static bool IsDLCCheckRequired_Setter_Prefix()
        {
            return false; // Prevent setting
        }
    }
}
