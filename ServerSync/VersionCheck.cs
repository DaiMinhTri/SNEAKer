using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ServerSync;

[PublicAPI]
internal class VersionCheck
{
	private static readonly HashSet<VersionCheck> versionChecks = new();
	private readonly ConfigSync configSync;
	private string DisplayName = "";
	private string CurrentVersion = "";
	private string MinimumRequiredVersion = "";
	private string ReceivedCurrentVersion = "";
	private string ReceivedMinimumRequiredVersion = "";

	public VersionCheck(ConfigSync configSync)
	{
		this.configSync = configSync;
		versionChecks.Add(this);
	}

	[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
	private static class RegisterVersionCheckPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNetPeer peer)
		{
			if (!ZNet.instance.IsServer())
			{
				return;
			}
			foreach (VersionCheck versionCheck in versionChecks)
			{
				peer.m_rpc.Register<string, string>(versionCheck.configSync.Name + " Version", (Action<ZRpc, string, string>)versionCheck.RPC_ReceiveClientVersion);
			}
		}
	}

	[HarmonyPatch(typeof(ZRpc), "HandlePackage")]
	private static class SnatchCurrentlyHandlingRPC
	{
		[HarmonyPrefix]
		private static void Prefix(ZRpc __instance)
		{
			SnatchCurrentlyHandlingRPC2.currentRpc = __instance;
		}
	}

	private static class SnatchCurrentlyHandlingRPC2
	{
		public static ZRpc? currentRpc;
	}

	private void RPC_ReceiveClientVersion(ZRpc rpc, string currentVersion, string minimumVersion)
	{
		ReceivedCurrentVersion = currentVersion;
		ReceivedMinimumRequiredVersion = minimumVersion;
		DisplayName = configSync.DisplayName;
		CurrentVersion = configSync.CurrentVersion;
		MinimumRequiredVersion = configSync.MinimumRequiredVersion;
	}

	[HarmonyPatch(typeof(ZNet), "Rpc_All")]
	private static class VersionCheckPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance, string func, object[] parameters)
		{
			if (__instance.IsServer() || func != "ClientConnected")
			{
				return;
			}
			foreach (VersionCheck versionCheck in versionChecks)
			{
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, versionCheck.configSync.Name + " Version", versionCheck.configSync.CurrentVersion, versionCheck.configSync.MinimumRequiredVersion);
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "Shutdown")]
	private static class ResetOnShutdown
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			foreach (VersionCheck versionCheck in versionChecks)
			{
				versionCheck.ReceivedCurrentVersion = "";
				versionCheck.ReceivedMinimumRequiredVersion = "";
			}
		}
	}

	public string? CheckVersion()
	{
		if (string.IsNullOrEmpty(ReceivedCurrentVersion) || string.IsNullOrEmpty(CurrentVersion))
		{
			return null;
		}
		bool currentTooLow = new System.Version(CurrentVersion) < new System.Version(ReceivedMinimumRequiredVersion);
		bool currentTooHigh = new System.Version(CurrentVersion) > new System.Version(ReceivedCurrentVersion);
		bool requiredTooLow = new System.Version(MinimumRequiredVersion) < new System.Version(ReceivedMinimumRequiredVersion);
		bool requiredTooHigh = new System.Version(MinimumRequiredVersion) > new System.Version(ReceivedCurrentVersion);
		if (currentTooLow || currentTooHigh)
		{
			return DisplayName + " needs to be at least version " + ReceivedMinimumRequiredVersion + ". You have version " + CurrentVersion + ".";
		}
		if (requiredTooLow || requiredTooHigh)
		{
			return DisplayName + " may not be higher than version " + ReceivedCurrentVersion + ". You have version " + CurrentVersion + ".";
		}
		return null;
	}
}
