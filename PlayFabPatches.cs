using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace VeeTaikoCrack
{
    [HarmonyPatch]
    public class PlayFabPatches
    {
        private static object _localPlayerEntityKey;
        private static string _playFabUserId;
        private static bool _isAuthenticated = true; // Start as true to attempt cached authentication first    
        
        private const int MAX_AUTH_RETRIES = 5;
        private const string PLAYFAB_TITLE_ID = "AAC05";
        
        private static bool _useCustomIdFallback = true;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "AuthenticateLocalUserStart")]
        public static bool AuthenticateLocalUserStart_Prefix(object __instance)
        {
            Plugin.Log.LogInfo("PlayFab AuthenticateLocalUserStart - using custom Steam authentication");
            
            AuthenticateAsync(__instance).ConfigureAwait(false);
            
            return false;
        }
        private static async Task AuthenticateAsync(object managerInstance)
        {
            try
            {
                if (_isAuthenticated && _localPlayerEntityKey != null)
                {
                    Plugin.Log.LogInfo("Already authenticated, creating local user");
                    CreateLocalUser(managerInstance, _localPlayerEntityKey, _playFabUserId);
                    return;
                }
                
                var result = await LoginWithSteam();
                
                if (result.Success)
                {
                    _isAuthenticated = true;
                    _localPlayerEntityKey = result.EntityKey;
                    _playFabUserId = result.UserId;
                    
                    Plugin.Log.LogInfo($"PlayFab authentication successful! Entity ID: {GetEntityId(result.EntityKey)}");
                    
                    CreateLocalUser(managerInstance, result.EntityKey, result.UserId);
                }
                else
                {
                    Plugin.Log.LogError("PlayFab authentication failed!");
                    
                    CreateFallbackLocalUser(managerInstance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error during PlayFab authentication: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                
                CreateFallbackLocalUser(managerInstance);
            }
        }
        private static async Task<AuthenticationResult> LoginWithSteam()
        {
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
                                    Plugin.Log.LogInfo($"Steam is running, Steam ID: {steamId}");
                                    
                                    hwid = $"steam_{steamId}";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not get Steam ID: {ex.Message}");
            }
            
            int retries = 0;
            
            while (retries < MAX_AUTH_RETRIES)
            {
                try
                {
                    var tcs = new TaskCompletionSource<object>();
                    
                    var playFabClientAPIType = Type.GetType("PlayFab.PlayFabClientAPI, PlayFab");
                    var loginRequestType = Type.GetType("PlayFab.ClientModels.LoginWithCustomIDRequest, PlayFab");
                    
                    if (playFabClientAPIType == null || loginRequestType == null)
                    {
                        Plugin.Log.LogError("Could not find PlayFab types");
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
                        typeof(PlayFabPatches).GetMethod(nameof(OnLoginSuccess), 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            .MakeGenericMethod(resultCallbackType));
                    
                    var errorCallback = Delegate.CreateDelegate(errorCallbackType2,
                        typeof(PlayFabPatches).GetMethod(nameof(OnLoginError),
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
                        
                        return new AuthenticationResult
                        {
                            Success = true,
                            UserId = playFabId,
                            EntityKey = entityKey
                        };
                    }
                    
                    await Task.Delay(1000);
                    retries++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Exception during login attempt {retries + 1}: {ex.Message}");
                    await Task.Delay(1000);
                    retries++;
                }
            }
            
            return new AuthenticationResult { Success = false };
        }
        
        private static TaskCompletionSource<object> _currentLoginTcs;
        
        private static void OnLoginSuccess<T>(T result)
        {
            _currentLoginTcs?.SetResult(result);
        }
        
        private static void OnLoginError<T>(T error)
        {
            Plugin.Log.LogError($"PlayFab login error");
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
                var method = managerInstance.GetType().GetMethod("_CreateLocalUser",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(managerInstance, new object[] { entityKey, userId });
                    Plugin.Log.LogInfo("Local user created successfully");
                }
                else
                {
                    Plugin.Log.LogWarning("Could not find _CreateLocalUser method, trying state advancement");
                    AdvanceToAuthenticatedState(managerInstance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error creating local user: {ex.Message}");
                AdvanceToAuthenticatedState(managerInstance);
            }
        }
        private static void CreateFallbackLocalUser(object managerInstance)
        {
            Plugin.Log.LogWarning("Creating fallback local user");
            
            var entityKeyType = Type.GetType("PlayFab.ClientModels.EntityKey, PlayFab");
            if (entityKeyType != null)
            {
                var fakeEntityKey = Activator.CreateInstance(entityKeyType);
                entityKeyType.GetProperty("Id")?.SetValue(fakeEntityKey, $"FakeEntity_{Guid.NewGuid()}");
                entityKeyType.GetProperty("Type")?.SetValue(fakeEntityKey, "title_player_account");
                
                _localPlayerEntityKey = fakeEntityKey;
                _playFabUserId = $"FakeUser_{Guid.NewGuid()}";
                _isAuthenticated = true;
                
                CreateLocalUser(managerInstance, fakeEntityKey, _playFabUserId);
            }
            else
            {
                Plugin.Log.LogError("Could not create fallback entity key");
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
                    Plugin.Log.LogInfo("Advanced to LocalUserAuthenticated state");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error advancing state: {ex.Message}");
            }
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenCompleted")]
        public static bool GetEntityTokenCompleted_Prefix(object __instance, object response)
        {
            Plugin.Log.LogInfo("PlayFab GetEntityTokenCompleted - using cached authentication");
            
            if (_isAuthenticated && _localPlayerEntityKey != null)
            {
                CreateLocalUser(__instance, _localPlayerEntityKey, _playFabUserId);
            }
            else
            {
                AuthenticateAsync(__instance).ConfigureAwait(false);
            }
            
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenFailed")]
        public static bool GetEntityTokenFailed_Prefix(object __instance, object error)
        {
            Plugin.Log.LogWarning("PlayFab GetEntityTokenFailed - attempting custom authentication");
            
            AuthenticateAsync(__instance).ConfigureAwait(false);
            
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "CreateAndJoinNetworkImplStart")]
        public static bool CreateAndJoinNetworkImplStart_Prefix(object __instance, object networkConfiguration)
        {
            Plugin.Log.LogInfo("PlayFab CreateAndJoinNetworkImplStart - bypassing for compatibility");
            
            try
            {
                var completeMethod = __instance.GetType().GetMethod("CreateAndJoinNetworkImplComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (completeMethod != null)
                {
                    completeMethod.Invoke(__instance, new object[] { networkConfiguration });
                }
                else
                {
                    AdvanceToAuthenticatedState(__instance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in CreateAndJoinNetworkImplStart: {ex.Message}");
            }
            
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "JoinNetworkImplStart")]
        public static bool JoinNetworkImplStart_Prefix(object __instance, string networkId)
        {
            Plugin.Log.LogInfo($"PlayFab JoinNetworkImplStart - bypassing for network {networkId}");
            
            try
            {
                var completeMethod = __instance.GetType().GetMethod("JoinNetworkImplComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (completeMethod != null)
                {
                    completeMethod.Invoke(__instance, new object[] { networkId });
                }
                else
                {
                    var setStateMethod = __instance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (setStateMethod != null)
                    {
                        setStateMethod.Invoke(__instance, new object[] { 6 });
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in JoinNetworkImplStart: {ex.Message}");
            }
            
            return false;
        }
        private static string GetSteamAuthTicketString(byte[] ticketData)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in ticketData)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            return stringBuilder.ToString();
        }
        
        public static void Cleanup()
        {
            _isAuthenticated = false;
            _localPlayerEntityKey = null;
            _playFabUserId = null;
        }
    }
    
    internal struct AuthenticationResult
    {
        public bool Success;
        public string UserId;
        public object EntityKey;
    }
}
