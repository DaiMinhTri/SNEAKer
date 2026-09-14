using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SNEAKer.Utils;
using ServerSync;
using UnityEngine;
using static Skills;

namespace SNEAKer;

[BepInPlugin("blacks7ar.SNEAKer", "SNEAKer", "1.2.0")]
public class Plugin : BaseUnityPlugin
{
	private const SkillType SneakSkill = (SkillType)101;

	[HarmonyPatch(typeof(Character), "UpdateWalking")]
	public static class SneakSpeedPatch
	{
		[HarmonyPrefix]
		public static void Prefix(Character __instance, out float __state)
		{
			__state = __instance.m_crouchSpeed;
			if (__instance.IsPlayer() && __instance.IsCrouching()
				&& !__instance.IsEncumbered()
				&& __instance.m_name.Contains("Human"))
			{
				float skillFactor = __instance.GetSkillFactor(SneakSkill);
				__instance.m_crouchSpeed *= Mathf.Lerp(1f, _sneakSpeedAtMaxLevel.Value, skillFactor);
			}
		}

		[HarmonyFinalizer]
		public static Exception Finalizer(Character __instance, float __state, Exception __exception)
		{
			__instance.m_crouchSpeed = __state;
			return __exception;
		}
	}

	[HarmonyPatch(typeof(Skills), "RaiseSkill")]
	public static class RaiseSkillPatch
	{
		private static void Prefix(ref SkillType skillType, ref float factor)
		{
			if (_enableExpMultiplier.Value == Toggle.On && skillType == SneakSkill)
			{
				factor *= _sneakExpMultiplier.Value;
			}
		}

		private static void Postfix(Skills __instance, SkillType skillType)
		{
			if (_enableExpMultiplier.Value != Toggle.On || _displayExpGained.Value != Toggle.On || skillType != SneakSkill)
			{
				return;
			}
			try
			{
				if (Player.m_localPlayer == null) return;
				if (__instance.GetSkillLevel(SneakSkill) < 100f)
				{
					Skill skill = __instance.GetSkill(SneakSkill);
					float value = skill.m_accumulator / (skill.GetNextLevelRequirement() / 100f);
					Player.m_localPlayer.Message((MessageHud.MessageType)1, $"Level {skill.m_level.tFloat(0)} {skill.m_info.m_skill} [{skill.m_accumulator.tFloat(2)} / {skill.GetNextLevelRequirement().tFloat(2)}] ({value.tFloat(0)}%)", 0, skill.m_info.m_icon);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[SNEAKer] Failed to display exp: {ex.Message}");
			}
		}
	}

	public const string modName = "SNEAKer";

	public const string modAuthor = "blacks7ar";

	public const string modVersion = "1.2.0";

	public const string modLink = "https://valheim.thunderstore.io/package/blacks7ar/SNEAKer/";

	private static readonly Harmony _harmony = new Harmony("blacks7ar.SNEAKer");

	private static readonly ConfigSync _configSync = new ConfigSync("blacks7ar.SNEAKer")
	{
		DisplayName = "SNEAKer",
		CurrentVersion = "1.2.0",
		MinimumRequiredVersion = "1.2.0"
	};

	private static ConfigEntry<Toggle> _serverConfigLocked;

	private static ConfigEntry<Toggle> _enableExpMultiplier;

	private static ConfigEntry<float> _sneakExpMultiplier;

	private static ConfigEntry<Toggle> _displayExpGained;

	private static ConfigEntry<float> _sneakSpeedAtMaxLevel;

	private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
	{
		ConfigDescription val = new ConfigDescription(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
		ConfigEntry<T> val2 = Config.Bind(group, name, value, val);
		_configSync.AddConfigEntry(val2).SynchronizedConfig = synchronizedSetting;
		return val2;
	}

	public void Awake()
	{
		Config.SaveOnConfigSet = false;
		_serverConfigLocked = config("1- ServerSync", "Lock Configuration", Toggle.On, new ConfigDescription("If On, the configuration is locked and can be changed by server admins only."));
		_configSync.AddLockingConfigEntry(_serverConfigLocked);
		_sneakSpeedAtMaxLevel = config("2- General", "Max Sneak Speed", 3f, new ConfigDescription("Max sneak speed at Sneak Level 100.", new AcceptableValueRange<float>(1f, 5f)));
		_enableExpMultiplier = config("3- Skill Exp", "Enable Exp Multiplier", Toggle.On, new ConfigDescription("Enable/Disable exp multiplier."));
		_sneakExpMultiplier = config("3- Skill Exp", "Exp Gain Multiplier", 1f, new ConfigDescription("Sneak exp multiplier.", new AcceptableValueRange<float>(0.1f, 5f)));
		_displayExpGained = config("3- Skill Exp", "Display Exp Gained", Toggle.On, new ConfigDescription("Enable/Disable exp gained notifications."));
		Config.SaveOnConfigSet = true;
		Config.Save();
		_harmony.PatchAll(typeof(Plugin).Assembly);
	}

	private void OnDestroy()
	{
		Config.Save();
	}
}
