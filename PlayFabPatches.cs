using System;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace VeeTaikoCrack
{
    [HarmonyPatch]
    public class PlayFabPatches
    {
        // Store the authenticated entity key (using object to avoid IL2CPP type dependencies)
        private static object _localPlayerEntityKey;
        private static string _playFabUserId;
        private static bool _isAuthenticated = false;
        
        // Configuration
        private const int MAX_AUTH_RETRIES = 5;
        private const string PLAYFAB_TITLE_ID = "AAC05"; 
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "AuthenticateLocalUserStart")]
        public static bool AuthenticateLocalUserStart_Prefix(object __instance)
        {
            try
            {
                Plugin.Log.LogInfo("PlayFab AuthenticateLocalUserStart - using custom Steam authentication");
                
                // Start authentication asynchronously without blocking
                _ = AuthenticateAsync(__instance);
                
                // Skip the original method
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in AuthenticateLocalUserStart_Prefix: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                return true; // Allow original method to run on error
            }
        }
        
        private static async Task AuthenticateAsync(object managerInstance)
        {
            try
            {
                // If already authenticated, just create the local user
                if (_isAuthenticated && _localPlayerEntityKey != null)
                {
                    Plugin.Log.LogInfo("Already authenticated, creating local user");
                    CreateLocalUser(managerInstance, _localPlayerEntityKey, _playFabUserId);
                    return;
                }
                
                // Attempt to login with Custom ID
                var result = await LoginWithCustomID();
                
                if (result.Success)
                {
                    _isAuthenticated = true;
                    _localPlayerEntityKey = result.EntityKey;
                    _playFabUserId = result.UserId;
                    
                    Plugin.Log.LogInfo($"PlayFab authentication successful! Entity ID: {GetEntityId(result.EntityKey)}");
                    
                    // Create the local user in the multiplayer manager
                    CreateLocalUser(managerInstance, result.EntityKey, result.UserId);
                }
                else
                {
                    Plugin.Log.LogError("PlayFab authentication failed!");
                    
                    // Fallback: Create a fake entity to allow the game to continue
                    CreateFallbackLocalUser(managerInstance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error during PlayFab authentication: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                
                // Fallback: Create a fake entity to allow the game to continue
                CreateFallbackLocalUser(managerInstance);
            }
        }
        
        private static async Task<AuthenticationResult> LoginWithCustomID()
        {
            // Get device unique identifier as the custom ID
            string customId = SystemInfo.deviceUniqueIdentifier;
            Plugin.Log.LogInfo($"Using Custom ID: {customId}");
            
            int retries = 0;
            
            // Retry loop
            while (retries < MAX_AUTH_RETRIES)
            {
                try
                {
                    var tcs = new TaskCompletionSource<object>();
                    
                    // Use reflection to call PlayFabClientAPI.LoginWithCustomID
                    var playFabClientAPIType = Type.GetType("PlayFab.PlayFabClientAPI, PlayFab");
                    var loginRequestType = Type.GetType("PlayFab.ClientModels.LoginWithCustomIDRequest, PlayFab");
                    
                    if (playFabClientAPIType == null || loginRequestType == null)
                    {
                        Plugin.Log.LogError("Could not find PlayFab types");
                        return new AuthenticationResult { Success = false };
                    }
                    
                    // Create the login request with CreateAccount enabled
                    var loginRequest = Activator.CreateInstance(loginRequestType);
                    loginRequestType.GetProperty("TitleId")?.SetValue(loginRequest, PLAYFAB_TITLE_ID);
                    loginRequestType.GetProperty("CustomId")?.SetValue(loginRequest, customId);
                    loginRequestType.GetProperty("CreateAccount")?.SetValue(loginRequest, true); // Auto-create new accounts
                    
                    Plugin.Log.LogInfo($"Attempting PlayFab login with CustomID: {customId} (CreateAccount: true)");
                    
                    // Create callbacks
                    var resultCallbackType = Type.GetType("PlayFab.ClientModels.LoginResult, PlayFab");
                    var errorCallbackType = Type.GetType("PlayFab.PlayFabError, PlayFab");
                    
                    var successCallbackType = typeof(Action<>).MakeGenericType(resultCallbackType);
                    var errorCallbackType2 = typeof(Action<>).MakeGenericType(errorCallbackType);
                    
                    var successMethod = typeof(PlayFabPatches).GetMethod(nameof(OnLoginSuccess),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var errorMethod = typeof(PlayFabPatches).GetMethod(nameof(OnLoginError),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    
                    if (successMethod == null || errorMethod == null)
                    {
                        Plugin.Log.LogError("Could not find callback methods");
                        return new AuthenticationResult { Success = false };
                    }
                    
                    var successCallback = Delegate.CreateDelegate(successCallbackType,
                        successMethod.MakeGenericMethod(resultCallbackType));
                    
                    var errorCallback = Delegate.CreateDelegate(errorCallbackType2,
                        errorMethod.MakeGenericMethod(errorCallbackType));
                    
                    // Store TCS in a static field so callbacks can access it
                    _currentLoginTcs = tcs;
                    
                    // Call LoginWithCustomID
                    var loginMethod = playFabClientAPIType.GetMethod("LoginWithCustomID");
                    loginMethod?.Invoke(null, new object[] { loginRequest, successCallback, errorCallback, null, null });
                    
                    var loginResult = await tcs.Task;
                    
                    if (loginResult != null)
                    {
                        // Extract entity key and user ID from result
                        var entityTokenProp = loginResult.GetType().GetProperty("EntityToken");
                        var entityToken = entityTokenProp?.GetValue(loginResult);
                        var entityProp = entityToken?.GetType().GetProperty("Entity");
                        var entityKey = entityProp?.GetValue(entityToken);
                        
                        var playFabIdProp = loginResult.GetType().GetProperty("PlayFabId");
                        var playFabId = playFabIdProp?.GetValue(loginResult) as string;
                        
                        // Check if this was a new account creation
                        var newlyCreatedProp = loginResult.GetType().GetProperty("NewlyCreated");
                        var newlyCreated = newlyCreatedProp?.GetValue(loginResult) as bool? ?? false;
                        
                        Plugin.Log.LogInfo($"PlayFab login successful! PlayFabId: {playFabId}, NewlyCreated: {newlyCreated}");
                        
                        // Success!
                        return new AuthenticationResult
                        {
                            Success = true,
                            UserId = playFabId,
                            EntityKey = entityKey
                        };
                    }
                    
                    // Failed, retry
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
            
            // Max retries reached
            return new AuthenticationResult { Success = false };
        }
        
        private static TaskCompletionSource<object> _currentLoginTcs;
        
        private static void OnLoginSuccess<T>(T result)
        {
            _currentLoginTcs?.SetResult(result);
        }
        
        private static void OnLoginError<T>(T error)
        {
            try
            {
                if (error != null)
                {
                    var errorType = error.GetType();
                    var errorMessageProp = errorType.GetProperty("ErrorMessage");
                    var errorCodeProp = errorType.GetProperty("Error");
                    var httpCodeProp = errorType.GetProperty("HttpCode");
                    
                    var errorMessage = errorMessageProp?.GetValue(error) as string ?? "Unknown error";
                    var errorCode = errorCodeProp?.GetValue(error)?.ToString() ?? "Unknown";
                    var httpCode = httpCodeProp?.GetValue(error)?.ToString() ?? "Unknown";
                    
                    Plugin.Log.LogError($"PlayFab login error - Code: {errorCode}, HTTP: {httpCode}, Message: {errorMessage}");
                }
                else
                {
                    Plugin.Log.LogError("PlayFab login error - No error details available");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"PlayFab login error (failed to parse error details): {ex.Message}");
            }
            
            _currentLoginTcs?.SetResult(null);
        }
        
        private static string GetEntityId(object entityKey)
        {
            if (entityKey == null) return "null";
            var idProp = entityKey.GetType().GetProperty("Id");
            return idProp?.GetValue(entityKey) as string ?? "unknown";
        }
        
        /// <summary>
        /// Creates a local user in the PlayFab multiplayer manager.
        /// </summary>
        private static void CreateLocalUser(object managerInstance, object entityKey, string userId)
        {
            if (managerInstance == null)
            {
                Plugin.Log.LogError("CreateLocalUser: managerInstance is null");
                return;
            }
            
            try
            {
                // Call _CreateLocalUser using reflection
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
            catch (System.Reflection.TargetInvocationException ex)
            {
                Plugin.Log.LogError($"Error invoking _CreateLocalUser: {ex.InnerException?.Message ?? ex.Message}");
                if (ex.InnerException != null)
                {
                    Plugin.Log.LogError(ex.InnerException.StackTrace);
                }
                AdvanceToAuthenticatedState(managerInstance);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error creating local user: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                AdvanceToAuthenticatedState(managerInstance);
            }
        }
        
        private static void CreateFallbackLocalUser(object managerInstance)
        {
            Plugin.Log.LogWarning("Creating fallback local user");
            
            // Create a fake entity key using reflection
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
                    // State 5 = LocalUserAuthenticated
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
            try
            {
                Plugin.Log.LogInfo("PlayFab GetEntityTokenCompleted - using cached authentication");
                
                if (_isAuthenticated && _localPlayerEntityKey != null)
                {
                    CreateLocalUser(__instance, _localPlayerEntityKey, _playFabUserId);
                }
                else
                {
                    // Trigger authentication without blocking
                    _ = AuthenticateAsync(__instance);
                }
                
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in GetEntityTokenCompleted_Prefix: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                return true; // Allow original method to run on error
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenFailed")]
        public static bool GetEntityTokenFailed_Prefix(object __instance, object error)
        {
            try
            {
                Plugin.Log.LogWarning("PlayFab GetEntityTokenFailed - attempting custom authentication");
                
                // Trigger authentication without blocking
                _ = AuthenticateAsync(__instance);
                
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in GetEntityTokenFailed_Prefix: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                return true; // Allow original method to run on error
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "CreateAndJoinNetworkImplStart")]
        public static bool CreateAndJoinNetworkImplStart_Prefix(object __instance, object networkConfiguration)
        {
            try
            {
                Plugin.Log.LogInfo("PlayFab CreateAndJoinNetworkImplStart - bypassing for compatibility");
                
                if (__instance == null)
                {
                    Plugin.Log.LogError("CreateAndJoinNetworkImplStart: __instance is null");
                    return true;
                }
                
                // Try to call the complete method
                var completeMethod = __instance.GetType().GetMethod("CreateAndJoinNetworkImplComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (completeMethod != null)
                {
                    completeMethod.Invoke(__instance, new object[] { networkConfiguration });
                }
                else
                {
                    // Fallback: advance state
                    AdvanceToAuthenticatedState(__instance);
                }
                
                return false; // Skip original method
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                Plugin.Log.LogError($"Error invoking CreateAndJoinNetworkImplComplete: {ex.InnerException?.Message ?? ex.Message}");
                if (ex.InnerException != null)
                {
                    Plugin.Log.LogError(ex.InnerException.StackTrace);
                }
                return true; // Allow original method to run on error
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in CreateAndJoinNetworkImplStart: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                return true; // Allow original method to run on error
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "JoinNetworkImplStart")]
        public static bool JoinNetworkImplStart_Prefix(object __instance, string networkId)
        {
            try
            {
                Plugin.Log.LogInfo($"PlayFab JoinNetworkImplStart - bypassing for network {networkId}");
                
                if (__instance == null)
                {
                    Plugin.Log.LogError("JoinNetworkImplStart: __instance is null");
                    return true;
                }
                
                // Try to call the complete method
                var completeMethod = __instance.GetType().GetMethod("JoinNetworkImplComplete",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (completeMethod != null)
                {
                    completeMethod.Invoke(__instance, new object[] { networkId });
                }
                else
                {
                    // Fallback: set state to ConnectedToNetwork (6)
                    var setStateMethod = __instance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (setStateMethod != null)
                    {
                        setStateMethod.Invoke(__instance, new object[] { 6 });
                    }
                }
                
                return false; // Skip original method
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                Plugin.Log.LogError($"Error invoking JoinNetworkImplComplete: {ex.InnerException?.Message ?? ex.Message}");
                if (ex.InnerException != null)
                {
                    Plugin.Log.LogError(ex.InnerException.StackTrace);
                }
                return true; // Allow original method to run on error
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error in JoinNetworkImplStart: {ex.Message}");
                Plugin.Log.LogError(ex.StackTrace);
                return true; // Allow original method to run on error
            }
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
        public object EntityKey; // Using object to avoid IL2CPP type dependency
    }
}