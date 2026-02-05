using System;
using System.Security.Permissions;
using HarmonyLib;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace VeeTaikoCrack
{
    [HarmonyPatch]
    public class PlayFabPatches
    {
        // Enhanced to simulate Steam-based authentication for better compatibility
        // Helper method to get Steam ID for token generation
        private static ulong GetSteamIdForToken()
        {
            // Try to get the actual Steam ID if Steam is initialized
            try
            {
                // Use reflection to safely access Steam components to avoid direct dependency
                var steamApiType = Type.GetType("Steamworks.SteamAPI, Assembly-CSharp") ??
                                  Type.GetType("Steamworks.SteamAPI");
                if (steamApiType != null)
                {
                    var isSteamRunningMethod = steamApiType.GetMethod("IsSteamRunning",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (isSteamRunningMethod != null)
                    {
                        var isRunning = (bool)(isSteamRunningMethod.Invoke(null, new object[0]) ?? false);
                        if (isRunning)
                        {
                            var steamUserType = Type.GetType("Steamworks.SteamUser, Assembly-CSharp") ??
                                              Type.GetType("Steamworks.SteamUser");
                            if (steamUserType != null)
                            {
                                var getSteamIdMethod = steamUserType.GetMethod("GetSteamID",
                                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                                if (getSteamIdMethod != null)
                                {
                                    var steamIdObj = getSteamIdMethod.Invoke(null, new object[0]);
                                    if (steamIdObj != null)
                                    {
                                        var steamIdValue = steamIdObj.GetType().GetProperty("m_SteamID");
                                        if (steamIdValue != null)
                                        {
                                            var steamId = (ulong)steamIdValue.GetValue(steamIdObj);
                                            Plugin.Log.LogInfo($"Using actual Steam ID for PlayFab token: {steamId}");
                                            return steamId;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If Steam isn't available or there's an error, use a default/fake Steam ID
                Plugin.Log.LogWarning($"Error accessing Steam for PlayFab token: {ex.Message}, using default Steam ID");
            }
            
            // Return a reasonable default Steam ID
            return 76561198000000000UL; // Standard format Steam ID base
        }
        
        // Helper method to call CreateLocalUser with reflection to avoid type dependency issues
        private static void CallCreateLocalUser(object managerInstance, string entityId, string entityType, string entityToken)
        {
            try
            {
                // Create EntityKey object via reflection to avoid direct dependency
                var entityKeyType = Type.GetType("PlayFab.ClientModels.EntityKey, Assembly-CSharp") ??
                                   Type.GetType("PlayFab.ClientModels.EntityKey");
                if (entityKeyType != null)
                {
                    var entityKeyInstance = Activator.CreateInstance(entityKeyType);
                    entityKeyType.GetProperty("Id")?.SetValue(entityKeyInstance, entityId);
                    entityKeyType.GetProperty("Type")?.SetValue(entityKeyInstance, entityType);
                    
                    var method = managerInstance.GetType().GetMethod("_CreateLocalUser",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(managerInstance, new object[] { entityKeyInstance, entityToken });
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Error in CallCreateLocalUser: {ex.Message}");
                // Fallback: try to advance the state directly
                var setStateMethod = managerInstance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setStateMethod != null)
                {
                    setStateMethod.Invoke(managerInstance, new object[] { 4 }); // LocalUserCreated state
                }
            }
        }

        // Patch the GetEntityTokenCompleted method - called when entity token is successfully retrieved
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenCompleted")]
        public static bool GetEntityTokenCompleted_Prefix(object __instance, object response)
        {
            Plugin.Log.LogInfo("PlayFab GetEntityTokenCompleted - bypassing for multiplayer compatibility with Steam-based credentials");
            // Instead of calling the original, we'll simulate success with Steam-based credentials
            var steamId = GetSteamIdForToken();
            var entityId = $"Ent{{{Guid.NewGuid()}}}-steam-{steamId}";
            var entityToken = $"steam-derived-token-{steamId}-{DateTime.UtcNow.Ticks}";
            CallCreateLocalUser(__instance, entityId, "title_player_account", entityToken);
            return false; // Skip original method
        }

        // Patch the GetEntityTokenFailed method to prevent failure and maintain multiplayer capability
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "GetEntityTokenFailed")]
        public static bool GetEntityTokenFailed_Prefix(object __instance, object error)
        {
            Plugin.Log.LogInfo("PlayFab GetEntityTokenFailed intercepted - simulating success with Steam-based credentials for multiplayer");
            // Instead of processing the failure, we'll simulate success with Steam-based credentials as if authentication worked
            var steamId = GetSteamIdForToken();
            var entityId = $"Ent{{{Guid.NewGuid()}}}-steam-{steamId}";
            var entityToken = $"steam-derived-token-{steamId}-{DateTime.UtcNow.Ticks}";
            CallCreateLocalUser(__instance, entityId, "title_player_account", entityToken);
            return false; // Skip original method
        }
        
        // Patch the AuthenticateLocalUserStart method to skip the actual authentication
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "AuthenticateLocalUserStart")]
        public static bool AuthenticateLocalUserStart_Prefix(object __instance)
        {
            Plugin.Log.LogInfo("PlayFab AuthenticateLocalUserStart - bypassing for multiplayer compatibility with Steam-based credentials");
            // Instead of doing real authentication, generate Steam-based credentials and advance state
            var steamId = GetSteamIdForToken();
            var entityId = $"Ent{{{Guid.NewGuid()}}}-steam-{steamId}";
            var entityToken = $"steam-derived-token-{steamId}-{DateTime.UtcNow.Ticks}";
            CallCreateLocalUser(__instance, entityId, "title_player_account", entityToken);
            return false; // Skip original method
        }
        
        // Patch the CreateAndJoinNetworkImplStart method to bypass network creation restrictions
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "CreateAndJoinNetworkImplStart")]
        public static bool CreateAndJoinNetworkImplStart_Prefix(object __instance, object networkConfiguration)
        {
            Plugin.Log.LogInfo("PlayFab CreateAndJoinNetworkImplStart - bypassing for multiplayer compatibility");
            // Move to the complete state directly to bypass network creation
            var completeMethod = __instance.GetType().GetMethod("CreateAndJoinNetworkImplComplete",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (completeMethod != null)
            {
                completeMethod.Invoke(__instance, new object[] { networkConfiguration });
            }
            else
            {
                // Fallback: advance state manually
                var setStateMethod = __instance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setStateMethod != null)
                {
                    setStateMethod.Invoke(__instance, new object[] { 5 }); // LocalUserAuthenticated state
                }
            }
            return false; // Skip original method
        }
        
        // Patch the JoinNetworkImplStart method to bypass network joining restrictions
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "JoinNetworkImplStart")]
        public static bool JoinNetworkImplStart_Prefix(object __instance, string networkId)
        {
            Plugin.Log.LogInfo("PlayFab JoinNetworkImplStart - bypassing for multiplayer compatibility");
            // Move to the complete state directly to bypass network joining
            var completeMethod = __instance.GetType().GetMethod("JoinNetworkImplComplete",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (completeMethod != null)
            {
                completeMethod.Invoke(__instance, new object[] { networkId });
            }
            else
            {
                // Fallback: advance state manually
                var setStateMethod = __instance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setStateMethod != null)
                {
                    setStateMethod.Invoke(__instance, new object[] { 6 }); // ConnectedToNetwork state
                }
            }
            return false; // Skip original method
        }
        
        // Patch the JoinNetworkImplComplete method to ensure network join completes properly
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "JoinNetworkImplComplete")]
        public static bool JoinNetworkImplComplete_Prefix(object __instance, string networkId)
        {
            Plugin.Log.LogInfo("PlayFab JoinNetworkImplComplete - ensuring network connection state");
            // Advance to connected state directly
            var setStateMethod = __instance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (setStateMethod != null)
            {
                setStateMethod.Invoke(__instance, new object[] { 6 }); // ConnectedToNetwork state
            }
            
            // Trigger the network joined event to notify the game
            var eventField = __instance.GetType().GetField("OnNetworkJoined",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (eventField != null)
            {
                var onNetworkJoinedDelegate = eventField.GetValue(__instance) as System.Delegate;
                if (onNetworkJoinedDelegate != null)
                {
                    foreach (System.Delegate handler in onNetworkJoinedDelegate.GetInvocationList())
                    {
                        try
                        {
                            handler.DynamicInvoke(__instance, networkId);
                        }
                        catch (System.Exception ex)
                        {
                            Plugin.Log.LogWarning($"Error invoking OnNetworkJoined handler: {ex.Message}");
                        }
                    }
                }
            }
            
            // Also call _UpdateNetworkId to set the network descriptor
            var updateNetworkIdMethod = __instance.GetType().GetMethod("UpdateNetworkId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updateNetworkIdMethod != null)
            {
                // Create a fake party network descriptor
                var partyNetworkDescriptorType = System.Type.GetType("PartyCSharpSDK.PARTY_NETWORK_DESCRIPTOR, Assembly-CSharp") ??
                                               System.Type.GetType("PartyCSharpSDK.PARTY_NETWORK_DESCRIPTOR");
                if (partyNetworkDescriptorType != null)
                {
                    var fakeDescriptor = System.Activator.CreateInstance(partyNetworkDescriptorType);
                    try
                    {
                        updateNetworkIdMethod.Invoke(__instance, new object[] { networkId, fakeDescriptor });
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Log.LogWarning($"Error calling UpdateNetworkId: {ex.Message}");
                    }
                }
            }
            
            return false; // Skip original method
        }
        
        // Patch the LeaveNetworkImpl method to handle leaving networks gracefully
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayFab.Party.PlayFabMultiplayerManager), "LeaveNetworkImpl")]
        public static bool LeaveNetworkImpl_Prefix(object __instance, bool wasCallInitiatedByDeveloper)
        {
            Plugin.Log.LogInfo("PlayFab LeaveNetworkImpl - bypassing for multiplayer stability");
            // Don't actually leave the network, just reset state to maintain stability
            var setStateMethod = __instance.GetType().GetMethod("_SetPlayFabMultiplayerManagerInternalState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (setStateMethod != null)
            {
                setStateMethod.Invoke(__instance, new object[] { 5 }); // Back to LocalUserAuthenticated state
            }
            return false; // Skip original method
        }
    }
}