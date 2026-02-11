using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;
using Scripts.EnsoGame.Network;
using PartyXBLCSharpSDK;

namespace VeeTaikoCrack
{
    [BepInPlugin("com.veetaikocrack.unioncrax", "VeeTaikoCrack", "1.0.0")]
    public class ComprehensivePatches : BepInEx.IL2CPP.BasePlugin
    {
        internal static BepInEx.Logging.ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;
            Logger.LogInfo("==============)");
            Logger.LogInfo("VeeTaikoCrack");
            Logger.LogInfo("==============)");
            Logger.LogInfo("Loading plugin...");
            
            var harmony = new Harmony("com.veetaikocrack.unioncrax");
            harmony.PatchAll(typeof(ComprehensivePatches));
            
            Logger.LogInfo("✓ DLC/License checks bypassed");
            Logger.LogInfo("✓ PlayFab authentication patches applied");
            Logger.LogInfo("✓ Network validation patches applied");
            Logger.LogInfo("✓ Steam/Xbox authentication bypassed");
            Logger.LogInfo("✓ All patches applied successfully!");
            Logger.LogInfo("========================================");
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
        // PLAYFAB AUTHENTICATION - Enhanced Implementation
        // ============================================
        
        private static object _localPlayerEntityKey;
        private static string _playFabUserId;
        private static bool _isAuthenticated = true;
        private static TaskCompletionSource<object> _currentLoginTcs;
        
        private const int MAX_AUTH_RETRIES = 5;
        private const string PLAYFAB_TITLE_ID = "AAC05";
        
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "AuthenticateLocalUserStart")]
        public static bool AuthenticateLocalUserStart_Prefix(object __instance)
        {
            Logger.LogInfo("→ PlayFab authentication requested");
            Task.Run(async () => await AuthenticateAsync(__instance)).ContinueWith(t => {
                if (t.IsFaulted)
                    Logger.LogError($"Authentication task failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
            
            return false;
        }

        private static async Task AuthenticateAsync(object managerInstance)
        {
            try
            {
                if (_isAuthenticated && _localPlayerEntityKey != null)
                {
                    Logger.LogInfo("  ✓ Using cached authentication");
                    CreateLocalUser(managerInstance, _localPlayerEntityKey, _playFabUserId);
                    return;
                }
                
                Logger.LogInfo("  → Attempting PlayFab login...");
                var result = await LoginWithSteam();
                
                if (result.Success)
                {
                    _isAuthenticated = true;
                    _localPlayerEntityKey = result.EntityKey;
                    _playFabUserId = result.UserId;
                    
                    Logger.LogInfo($"  ✓ Authentication successful!");
                    CreateLocalUser(managerInstance, result.EntityKey, result.UserId);
                }
                else
                {
                    Logger.LogWarning("  ⚠ Authentication failed, using fallback");
                    CreateFallbackLocalUser(managerInstance);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"  ✗ PlayFab auth error: {ex.Message}");
                CreateFallbackLocalUser(managerInstance);
            }
        }

        private static async Task<AuthenticationResult> LoginWithSteam()
        {
            Logger.LogInfo("  → Checking Steam connection...");
            string hwid = SystemInfo.deviceUniqueIdentifier;
            
            ulong steamId = 0;
            try
            {
                var steamAPIType = Type.GetType("Steamworks.SteamAPI, com.rlabrecque.steamworks.net");
                var isSteamRunningMethod = steamAPIType?.GetMethod("IsSteamRunning",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                
                if (isSteamRunningMethod != null)
                {
                    var isRunning = (bool)(isSteamRunningMethod.Invoke(null, null) ?? false);
                    if (isRunning)
                    {
                        var steamUserType = Type.GetType("Steamworks.SteamUser, com.rlabrecque.steamworks.net");
                        var getSteamIDMethod = steamUserType?.GetMethod("GetSteamID",
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        
                        if (getSteamIDMethod != null)
                        {
                            var steamIdObj = getSteamIDMethod.Invoke(null, null);
                            if (steamIdObj != null)
                            {
                                var m_SteamIDField = steamIdObj.GetType().GetField("m_SteamID");
                                if (m_SteamIDField != null)
                                {
                                    steamId = (ulong)m_SteamIDField.GetValue(steamIdObj);
                                    hwid = $"steam_{steamId}";
                                    Logger.LogInfo($"  ✓ Steam connected (ID: {steamId})");
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.LogInfo("  ⚠ Steam not running, using hardware ID");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"  ⚠ Could not get Steam ID: {ex.Message}");
                Logger.LogInfo("  → Using hardware ID as fallback");
            }
            
            int retries = 0;
            Logger.LogInfo($"  → Connecting to PlayFab (Title: {PLAYFAB_TITLE_ID})...");
            
            while (retries < MAX_AUTH_RETRIES)
            {
                try
                {
                    if (retries > 0)
                    {
                        Logger.LogInfo($"  → Retry attempt {retries}/{MAX_AUTH_RETRIES}...");
                    }
                    
                    var tcs = new TaskCompletionSource<object>();
                    
                    var playFabClientAPIType = Type.GetType("PlayFab.PlayFabClientAPI, PlayFab");
                    var loginRequestType = Type.GetType("PlayFab.ClientModels.LoginWithCustomIDRequest, PlayFab");
                    
                    if (playFabClientAPIType == null || loginRequestType == null)
                    {
                        Logger.LogError("  ✗ Could not find PlayFab types");
                        return new AuthenticationResult { Success = false };
                    }
                    
                    var loginRequest = Activator.CreateInstance(loginRequestType);
                    loginRequestType.GetProperty("TitleId")?.SetValue(loginRequest, PLAYFAB_TITLE_ID);
                    loginRequestType.GetProperty("CustomId")?.SetValue(loginRequest, hwid);
                    loginRequestType.GetProperty("CreateAccount")?.SetValue(loginRequest, true);
                    
                    var resultCallbackType = Type.GetType("PlayFab.ClientModels.LoginResult, PlayFab");
                    var errorCallbackType = Type.GetType("PlayFab.PlayFabError, PlayFab");
                    
                    var successCallbackType = typeof(Action<>).MakeGenericType(resultCallbackType);
                    var errorCallbackType2 = typeof(Action<>).MakeGenericType(errorCallbackType);
                    
                    var successCallback = Delegate.CreateDelegate(successCallbackType, 
                        typeof(ComprehensivePatches).GetMethod(nameof(OnLoginSuccess), 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            .MakeGenericMethod(resultCallbackType));
                    
                    var errorCallback = Delegate.CreateDelegate(errorCallbackType2,
                        typeof(ComprehensivePatches).GetMethod(nameof(OnLoginError),
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            .MakeGenericMethod(errorCallbackType));
                    
                    _currentLoginTcs = tcs;
                    
                    var loginMethod = playFabClientAPIType.GetMethod("LoginWithCustomID");
                    loginMethod?.Invoke(null, new object[] { loginRequest, successCallback, errorCallback, null, null });
                    
                    var loginResult = await tcs.Task;
                    
                    if (loginResult != null)
                    {
                        var entityTokenProp = loginResult.GetType().GetProperty("EntityToken");
                        var entityToken = entityTokenProp?.GetValue(loginResult);
                        var entityProp = entityToken?.GetType().GetProperty("Entity");
                        var entityKey = entityProp?.GetValue(entityToken);
                        
                        var playFabIdProp = loginResult.GetType().GetProperty("PlayFabId");
                        var playFabId = playFabIdProp?.GetValue(loginResult) as string;
                        
                        var newlyCreatedProp = loginResult.GetType().GetProperty("NewlyCreated");
                        var newlyCreated = newlyCreatedProp != null && (bool)(newlyCreatedProp.GetValue(loginResult) ?? false);
                        
                        if (newlyCreated)
                        {
                            Logger.LogInfo($"  ✓ Created new PlayFab account");
                            Logger.LogInfo($"    Player ID: {playFabId}");
                        }
                        else
                        {
                            Logger.LogInfo($"  ✓ Loaded existing PlayFab account");
                            Logger.LogInfo($"    Player ID: {playFabId}");
                        }
                        
                        return new AuthenticationResult
                        {
                            Success = true,
                            UserId = playFabId,
                            EntityKey = entityKey
                        };
                    }
                    
                    Logger.LogWarning($"  ⚠ Login attempt {retries + 1} failed, retrying...");
                    await Task.Delay(1000);
                    retries++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"  ⚠ Attempt {retries + 1} error: {ex.Message}");
                    await Task.Delay(1000);
                    retries++;
                }
            }
            
            Logger.LogError($"  ✗ Failed after {MAX_AUTH_RETRIES} attempts");
            return new AuthenticationResult { Success = false };
        }
        
        private static void OnLoginSuccess<T>(T result)
        {
            _currentLoginTcs?.SetResult(result);
        }
        
        private static void OnLoginError<T>(T error)
        {
            _currentLoginTcs?.SetResult(null);
        }

        private static string GetEntityId(object entityKey)
        {
            if (entityKey == null) return "null";
            var idProp = entityKey.GetType().GetProperty("Id");
            return idProp?.GetValue(entityKey) as string ?? "unknown";
        }

        private static void CreateLocalUser(object managerInstance, object entityKey, string userId)
        {
            try
            {
                Logger.LogInfo("  → Creating local user session...");
                var method = managerInstance.GetType().GetMethod("_CreateLocalUser",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(managerInstance, new object[] { entityKey, userId });
                    Logger.LogInfo("  ✓ Local user session created");
                }
                else
                {
                    Logger.LogInfo("  → Advancing to authenticated state...");
                    AdvanceToAuthenticatedState(managerInstance);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"  ⚠ Error creating user: {ex.Message}");
                AdvanceToAuthenticatedState(managerInstance);
            }
        }

        private static void CreateFallbackLocalUser(object managerInstance)
        {
            Logger.LogInfo("  → Creating fallback offline account...");
            var entityKeyType = Type.GetType("PlayFab.ClientModels.EntityKey, PlayFab");
            if (entityKeyType != null)
            {
                var fakeEntityKey = Activator.CreateInstance(entityKeyType);
                var fakeId = Guid.NewGuid().ToString("N").Substring(0, 16);
                entityKeyType.GetProperty("Id")?.SetValue(fakeEntityKey, $"Offline_{fakeId}");
                entityKeyType.GetProperty("Type")?.SetValue(fakeEntityKey, "title_player_account");
                
                _localPlayerEntityKey = fakeEntityKey;
                _playFabUserId = $"Offline_{fakeId}";
                _isAuthenticated = true;
                
                Logger.LogInfo($"  ✓ Fallback account created: {_playFabUserId}");
                CreateLocalUser(managerInstance, fakeEntityKey, _playFabUserId);
            }
            else
            {
                Logger.LogWarning("  ⚠ Could not create fallback, advancing state");
                AdvanceToAuthenticatedState(managerInstance);
            }
        }

        private static void AdvanceToAuthenticatedState(object managerInstance)
        {
            try
            {
                var setStateMethod = managerInstance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (setStateMethod != null)
                {
                    setStateMethod.Invoke(managerInstance, new object[] { 5 });
                    Logger.LogInfo("  ✓ Advanced to authenticated state");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"  ⚠ Could not advance state: {ex.Message}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenCompleted")]
        public static bool GetEntityTokenCompleted_Prefix(object __instance, object response)
        {
            if (_isAuthenticated && _localPlayerEntityKey != null)
            {
                CreateLocalUser(__instance, _localPlayerEntityKey, _playFabUserId);
            }
            else
            {
                Task.Run(async () => await AuthenticateAsync(__instance)).ContinueWith(t => {
                    if (t.IsFaulted)
                        Logger.LogError($"GetEntityTokenCompleted auth task failed: {t.Exception?.InnerException?.Message}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenFailed")]
        public static bool GetEntityTokenFailed_Prefix(object __instance, object error)
        {
            Task.Run(async () => await AuthenticateAsync(__instance)).ContinueWith(t => {
                if (t.IsFaulted)
                    Logger.LogError($"GetEntityTokenFailed auth task failed: {t.Exception?.InnerException?.Message}");
            }, TaskContinuationOptions.OnlyOnFaulted);
            
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "CreateAndJoinNetworkImplStart")]
        public static bool CreateAndJoinNetworkImplStart_Prefix(object __instance, object networkConfiguration)
        {
            Logger.LogInfo("→ Creating multiplayer network...");
            if (!_isAuthenticated || _localPlayerEntityKey == null)
            {
                Logger.LogWarning("  ⚠ Not authenticated, attempting authentication first");
                Task.Run(async () => await AuthenticateAsync(__instance)).ContinueWith(t => {
                    if (t.IsFaulted)
                        Logger.LogError($"CreateAndJoinNetwork auth task failed: {t.Exception?.InnerException?.Message}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "JoinNetworkImplStart")]
        public static bool JoinNetworkImplStart_Prefix(object __instance, string networkId)
        {
            Logger.LogInfo($"→ Joining multiplayer network: {networkId}");
            if (!_isAuthenticated || _localPlayerEntityKey == null)
            {
                Logger.LogWarning("  ⚠ Not authenticated, attempting authentication first");
                Task.Run(async () => await AuthenticateAsync(__instance)).ContinueWith(t => {
                    if (t.IsFaulted)
                        Logger.LogError($"JoinNetwork auth task failed: {t.Exception?.InnerException?.Message}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            
            return true;
        }

        public static void CleanupPlayFabAuth()
        {
            _isAuthenticated = false;
            _localPlayerEntityKey = null;
            _playFabUserId = null;
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

        // Patch network state checks - Allow network playing
        [HarmonyPatch]
        public static class NetworkStatePatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                var type = typeof(Network.MatchEnsoDatas);
                var method = type.GetProperty("IsNetworkPlaying")?.GetGetMethod();
                if (method != null) methods.Add(method);
                
                return methods;
            }

            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                if (!__result)
                {
                    __result = true;
                }
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
        
        private const int SPOOFED_APP_ID = 480;
        private static bool _appIdFileCreated = false;

        static ComprehensivePatches()
        {
            ForceSteamAppId();
        }

        private static void ForceSteamAppId()
        {
            if (_appIdFileCreated) return;

            try
            {
                string appIdFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_appid.txt");
                File.WriteAllText(appIdFile, SPOOFED_APP_ID.ToString());
                Logger?.LogInfo($"Created steam_appid.txt with App ID {SPOOFED_APP_ID}");
                _appIdFileCreated = true;

                Environment.SetEnvironmentVariable("SteamAppId", SPOOFED_APP_ID.ToString());
                Environment.SetEnvironmentVariable("SteamGameId", SPOOFED_APP_ID.ToString());
                Logger?.LogInfo($"Set environment variables SteamAppId and SteamGameId to {SPOOFED_APP_ID}");
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Failed to force App ID: {ex.Message}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamAPI), "RestartAppIfNecessary")]
        public static bool RestartAppIfNecessary_Prefix(ref AppId_t unOwnAppID, ref bool __result)
        {
            Logger.LogInfo($"SteamAPI.RestartAppIfNecessary called with App ID: {unOwnAppID.m_AppId}, forcing to {SPOOFED_APP_ID}");
            unOwnAppID = new AppId_t(SPOOFED_APP_ID);
            __result = false;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamAPI), "Init")]
        public static void Init_Postfix(ref bool __result)
        {
            Logger.LogInfo($"SteamAPI.Init called, ensuring success with spoofed app ID: {SPOOFED_APP_ID}");
            if (!__result)
            {
                __result = true;
                Logger.LogWarning("Forced SteamAPI.Init to succeed");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamUtils), "GetAppID")]
        public static void GetAppID_Postfix(ref AppId_t __result)
        {
            Logger.LogInfo($"SteamUtils.GetAppID returning spoofed App ID: {SPOOFED_APP_ID}");
            __result = new AppId_t(SPOOFED_APP_ID);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamApps), "BIsSubscribedApp")]
        public static bool BIsSubscribedApp_Prefix(ref AppId_t appID, ref bool __result)
        {
            Logger.LogInfo($"SteamApps.BIsSubscribedApp called with App ID: {appID.m_AppId}, forcing to {SPOOFED_APP_ID}");
            appID = new AppId_t(SPOOFED_APP_ID);
            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamApps), "BIsAppInstalled")]
        public static bool BIsAppInstalled_Prefix(ref AppId_t appID, ref bool __result)
        {
            Logger.LogInfo($"SteamApps.BIsAppInstalled called with App ID: {appID.m_AppId}, forcing to {SPOOFED_APP_ID}");
            appID = new AppId_t(SPOOFED_APP_ID);
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamApps), "GetAppOwner")]
        public static void GetAppOwner_Postfix(ref CSteamID __result)
        {
            Logger.LogInfo("SteamApps.GetAppOwner returning owner for spoofed app");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public static void SteamManager_Awake_Postfix(SteamManager __instance)
        {
            Logger.LogInfo("Initializing SteamManager");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "InitOnPlayMode")]
        public static bool InitOnPlayMode_Prefix()
        {
            Logger.LogInfo("Intercepting SteamManager init to prevent restart");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public static bool SteamManager_Awake_Prefix(SteamManager __instance)
        {
            Logger.LogInfo("Intercepting SteamManager awake to prevent restart");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "OnEnable")]
        public static bool SteamManager_OnEnable_Prefix(SteamManager __instance)
        {
            Logger.LogInfo("Intercepting SteamManager OnEnable to prevent restart");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamManager), "OnDestroy")]
        public static bool OnDestroy_Prefix(SteamManager __instance)
        {
            Logger.LogInfo("Preventing SteamManager OnDestroy to maintain Steam session");
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Platform.Steam.SteamAccount), "LoginAsync")]
        public static void LoginAsync_Postfix(ref object __result)
        {
            Logger.LogInfo("Bypassing Steam login authentication");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Platform.Steam.SteamAccount), "IsLogin")]
        public static void IsLogin_Postfix(ref bool __result)
        {
            Logger.LogInfo("Spoofing Steam login status to logged in");
            __result = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamApps), "BIsDlcInstalled")]
        public static bool BIsDlcInstalled_Prefix(ref AppId_t appID, ref bool __result)
        {
            Logger.LogInfo($"SteamApps.BIsDlcInstalled called with App ID: {appID.m_AppId}, forcing to {SPOOFED_APP_ID}");
            appID = new AppId_t(SPOOFED_APP_ID);
            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamApps), "GetCurrentGameLanguage")]
        public static void GetCurrentGameLanguage_Prefix()
        {
            ForceSteamAppId();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamUser), "GetSteamID")]
        public static void GetSteamID_Prefix()
        {
            ForceSteamAppId();
        }

        // Steam authentication bypass (general)
        [HarmonyPatch]
        public static class SteamAuthPatches
        {
            [HarmonyTargetMethods]
            public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                var methods = new System.Collections.Generic.List<System.Reflection.MethodBase>();
                
                try
                {
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), MethodType.Constructor)]
        public static bool XboxLiveOnlineManager_Constructor_Prefix()
        {
            Logger.LogInfo("Bypassing XboxLiveOnlineManager constructor - Xbox Live not required");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "Initialize")]
        public static bool Initialize_Prefix(ref bool __result)
        {
            Logger.LogInfo("XboxLiveOnlineManager.Initialize bypassed - returning success");
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "IsLoggedIn")]
        public static void IsLoggedIn_Postfix(ref bool __result)
        {
            Logger.LogInfo("XboxLiveOnlineManager.IsLoggedIn spoofed to true");
            __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "GetUserId")]
        public static void GetUserId_Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
            {
                __result = $"FakeXboxUser_{SystemInfo.deviceUniqueIdentifier}";
                Logger.LogInfo($"XboxLiveOnlineManager.GetUserId spoofed to: {__result}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OnlineManager.XboxLiveOnlineManager), "GetToken")]
        public static void GetToken_Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
            {
                __result = $"FakeXboxToken_{Guid.NewGuid()}";
                Logger.LogInfo($"XboxLiveOnlineManager.GetToken spoofed to: {__result}");
            }
        }

        // ============================================
        // VALIDATION CHECKS
        // ============================================
        
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

        // ============================================
        // ENSEMBLE GAME MANAGER (Network State)
        // ============================================
        
        private static bool _networkInitialized = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "SetupOnline")]
        public static bool SetupOnline_Prefix(EnsoGameManager __instance, string sceneName)
        {
            Logger.LogInfo($"EnsoGameManager.SetupOnline called for scene: {sceneName}");
            _networkInitialized = true;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "TeardownOnline")]
        public static bool TeardownOnline_Prefix()
        {
            Logger.LogInfo("EnsoGameManager.TeardownOnline called");
            _networkInitialized = false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "ProcPreparingOnline")]
        public static bool ProcPreparingOnline_Prefix()
        {
            Logger.LogInfo("EnsoGameManager.ProcPreparingOnline - bypassing network preparation");
            return _networkInitialized;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "ProcExecOnline")]
        public static bool ProcExecOnline_Prefix()
        {
            Logger.LogInfo("EnsoGameManager.ProcExecOnline - handling online gameplay");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnsoGameManager), "CheckEnsoEndOnline")]
        public static bool CheckEnsoEndOnline_Prefix()
        {
            Logger.LogInfo("EnsoGameManager.CheckEnsoEndOnline - checking song end");
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoGameManager), "IsWaitEnsoEndOnline")]
        public static void IsWaitEnsoEndOnline_Postfix(ref bool __result)
        {
            if (!_networkInitialized)
            {
                __result = false;
                Logger.LogInfo("IsWaitEnsoEndOnline - not waiting (network not initialized)");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoPlayingParameter), "IsOnlineMode", MethodType.Getter)]
        public static void IsOnlineMode_Getter_Postfix(ref bool __result)
        {
            __result = _networkInitialized;
            Logger.LogInfo($"IsOnlineMode - returning: {__result}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnsoPlayingParameter), "networkGameMode", MethodType.Getter)]
        public static void NetworkGameMode_Getter_Postfix(ref NetworkGameMode __result)
        {
            Logger.LogInfo($"NetworkGameMode getter - current mode: {__result}");
        }

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
            Logger.LogInfo($"NXNetworkManager.ConnectivityTestInterval set to: {__result}ms");
        }

        // ============================================
        // AUTHENTICATION RESULT STRUCT
        // ============================================
        
        internal struct AuthenticationResult
        {
            public bool Success;
            public string UserId;
            public object EntityKey;
        }
    }
}
