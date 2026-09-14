using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SNEAKer.Utils;
using ServerSync;
using static Skills;

namespace SNEAKer;

[BepInPlugin("blacks7ar.SNEAKer", "SNEAKer", "1.1.8")]
public class Plugin : BaseUnityPlugin
{
	[HarmonyPatch(typeof(Character), "UpdateWalking")]
	public class UpdateWalingPatch
	{
		private static readonly FieldInfo field_Character_m_crouchSpeed = AccessTools.Field(typeof(Character), "m_crouchSpeed");

		private static readonly MethodInfo method_GetMoveSpeed = AccessTools.Method(typeof(UpdateWalingPatch), "GetMoveSpeed", (Type[])null, (Type[])null);

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> source = Enumerable.ToList(instructions);
			foreach (CodeInstruction item in Enumerable.Where(source, (CodeInstruction t) => CodeInstructionExtensions.LoadsField(t, field_Character_m_crouchSpeed, false)))
			{
				item.opcode = OpCodes.Call;
				item.operand = method_GetMoveSpeed;
			}
			return Enumerable.AsEnumerable(source);
		}

		public static float GetMoveSpeed(Character __instance)
		{
			if (__instance.IsEncumbered() || !__instance.m_name.Contains("Human"))
			{
				return __instance.m_crouchSpeed;
			}
			if (_enableMod.Value != Toggle.On)
			{
				return __instance.m_crouchSpeed;
			}
			float crouchSpeed = __instance.m_crouchSpeed;
			float num = _sneakSpeedAtMaxLevel.Value * __instance.GetSkillFactor((SkillType)101);
			return crouchSpeed + num;
		}
	}

	[HarmonyPatch(typeof(Skills), "RaiseSkill")]
	public static class RaiseSkillPatch
	{
		private static void Prefix(ref SkillType skillType, ref float factor)
		{
			if (_enableMod.Value == Toggle.On && _enableExpMultiplier.Value == Toggle.On && (int)skillType == 101)
			{
				factor *= _sneakExpMultiplier.Value;
			}
		}

		private static void Postfix(Skills __instance, SkillType skillType)
		{
			if (_enableMod.Value != Toggle.On || _enableExpMultiplier.Value != Toggle.On || _displayExpGained.Value != Toggle.On || (int)skillType != 101)
			{
				return;
			}
			try
			{
				if (__instance.GetSkillLevel((SkillType)101) < 100f)
				{
					Skill skill = __instance.GetSkill((SkillType)101);
					float value = skill.m_accumulator / (skill.GetNextLevelRequirement() / 100f);
					Player.m_localPlayer.Message((MessageHud.MessageType)1, $"Level {skill.m_level.tFloat(0)} {skill.m_info.m_skill} [{skill.m_accumulator.tFloat(2)} / {skill.GetNextLevelRequirement().tFloat(2)}] ({value.tFloat(0)}%)", 0, skill.m_info.m_icon);
				}
			}
			catch
			{
			}
		}
	}

	private const string modGUID = "blacks7ar.SNEAKer";

	public const string modName = "SNEAKer";

	public const string modAuthor = "blacks7ar";

	public const string modVersion = "1.1.8";

	public const string modLink = "https://valheim.thunderstore.io/package/blacks7ar/SNEAKer/";

	private static readonly Harmony _harmony = new Harmony("blacks7ar.SNEAKer");

	private static readonly ConfigSync _configSync = new ConfigSync("blacks7ar.SNEAKer")
	{
		DisplayName = "SNEAKer",
		CurrentVersion = "1.1.8",
		MinimumRequiredVersion = "1.1.8"
	};

	private static ConfigEntry<Toggle> _serverConfigLocked;

	private static ConfigEntry<Toggle> _enableMod;

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
		_enableMod = config("2- General", "Enable Mod", Toggle.On, new ConfigDescription("Enable/Disable the mod."));
		_sneakSpeedAtMaxLevel = config("2- General", "Max Sneak Speed", 3f, new ConfigDescription("Max sneak speed at Sneak Level 100.", new AcceptableValueRange<float>(1f, 5f)));
		_enableExpMultiplier = config("3- Skill Exp", "Enable Exp Multiplier", Toggle.On, new ConfigDescription("Enable/Disable exp multiplier."));
		_sneakExpMultiplier = config("3- Skill Exp", "Exp Gain Multiplier", 1f, new ConfigDescription("Sneak exp multiplier.", new AcceptableValueRange<float>(0.1f, 5f)));
		_displayExpGained = config("3- Skill Exp", "Display Exp Gained", Toggle.On, new ConfigDescription("Enable/Disable exp gained notifications."));
		Config.SaveOnConfigSet = true;
		Config.Save();
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		_harmony.PatchAll(executingAssembly);
	}

	private void OnDestroy()
	{
		Config.Save();
	}
}
