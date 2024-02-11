using System;
using System.Linq;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
namespace YetAnotherLethalLibrary;

public static class GameManager
{
	public static Action<PlayerControllerB> OnPlayerSpawned;
	public static Action<PlayerControllerB> OnClientPlayerConnected;
	public static Action OnClientPlayerDisconnected;
	public static Action<PlayerControllerB> OnPlayerLateUpdate;
	public static Action<StartOfRound> OnRoundStart;
	public static Action<StartOfRound> OnRoundEnd;
	
	public static PlayerControllerB localPlayer;

	[HarmonyPatch(typeof(PlayerControllerB), "Start")]
	[HarmonyPostfix]
	private static void PlayerControllerBStartPostFix(PlayerControllerB __instance)
	{
		OnPlayerSpawned?.Invoke(__instance);
	}

	[HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
	[HarmonyPostfix]
	private static void PlayerControllerBLateUpdatePostFix(PlayerControllerB __instance)
	{
		OnPlayerLateUpdate?.Invoke(__instance);
	}

	[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
	[HarmonyPostfix]
	private static void PlayerControllerBConnectClientToPlayerObjectPostFix(PlayerControllerB __instance)
	{
		localPlayer = __instance;
		OnClientPlayerConnected?.Invoke(__instance);
	}
	
	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
	private static void GameNetworkManagerStartDisconnectPostFix() 
	{
		OnClientPlayerDisconnected?.Invoke();
	}
	
	[HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
	[HarmonyPostfix]
	private static void StartOfOnShipLandedMiscEventsPostFix(StartOfRound __instance)
	{
		OnRoundStart?.Invoke(__instance);
	}
	
	[HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
	[HarmonyPostfix]
	private static void StartOfRoundEndOfGamePostFix(StartOfRound __instance)
	{
		OnRoundEnd?.Invoke(__instance);
	}

	public static bool TryGetPlayerController(ulong id, out PlayerControllerB player)
	{
		player = null;
		var ins = StartOfRound.Instance;
		if (Helper.ConditionLog($"StartOfRound.Instance is null! Can't get player controller.", ins == null, LogLevel.Error)) return false;
		player = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(pc => pc.actualClientId == id);
		return player != null;
	}

}