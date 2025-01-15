using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Configs;
using Logging;
using UnityEngine;
using Jotunn.Extensions;

// To begin using: rename the solution and project, then find and replace all instances of "UnderTheSea"
// Next: rename the main plugin as desired.

// If using Jotunn then the following files should be removed from Configs:
// - ConfigManagerWatcher
// - ConfigurationManagerAttributes
// - ConfigFileExtensions should be editted to only include `DisableSaveOnConfigSet`

// If not using Jotunn
// - Remove the decorators on the MainPlugin
// - Swap from using SynchronizationManager.OnConfigurationWindowClosed to using ConfigManagerWatcher.OnConfigurationWindowClosed
// - Remove calls to SynchronizationManager.OnConfigurationSynchronized
// - Adjust using statements as needed
// - Remove nuget Jotunn package via manage nuget packages
// - Uncomment the line: <Import Project="$(JotunnProps)" Condition="Exists('$(JotunnProps)')" /> in the csproj file

namespace UnderTheSea;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
[BepInIncompatibility("ch.easy.develope.vh.diving.mod")]
[BepInIncompatibility("blacks7ar.VikingsDoSwim")]
[BepInIncompatibility("projjm.improvedswimming")]
[BepInIncompatibility("MainStreetGaming.BetterDiving")]
internal sealed class UnderTheSea : BaseUnityPlugin
{
    public const string PluginName = "UnderTheSea";
    internal const string Author = "Searica";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.1.0";

    internal static UnderTheSea Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;

    internal string CurrentEnvName = string.Empty;

    // Global settings
    internal const string GlobalSection = "Global";

    public ConfigEntry<float> RestingStaminaRegenDelay;
    public ConfigEntry<float> RestingStaminaRegenRate;
    public ConfigEntry<float> UnderwaterRestingStaminaDrainRate;
    public ConfigEntry<float> MaxSwimSpeed;
    public ConfigEntry<float> ColorDarknessFactor;
    public ConfigEntry<float> FogDensityFactor;
    public ConfigEntry<float> MinFogDensity;
    public ConfigEntry<float> MaxFogDensity;

    //Env values
    public static string EnvName = "";

    public void Awake()
    {
        Instance = this;
        ConfigFile = Config;
        Log.Init(Logger);

        Config.DisableSaveOnConfigSet();
        SetUpConfigEntries();
        Config.Save();
        Config.SaveOnConfigSet = true;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;

        // Re-initialization after reloading config and don't save since file was just reloaded
        ConfigFileWatcher = new(Config);
        ConfigFileWatcher.OnConfigFileReloaded += () =>
        {
            // do stuff
        };

        SynchronizationManager.OnConfigurationSynchronized += (obj, e) =>
        {
            // do stuff
        };

        SynchronizationManager.OnConfigurationWindowClosed += () =>
        {
            // do stuff
        };

    }

    internal void SetUpConfigEntries()
    {

        // Server synced config values
        RestingStaminaRegenDelay = Config.BindConfigInOrder(
            GlobalSection,
            "Treading Water Stamina Regen Delay",
            2f,
            "How long before you regenerate stamina when treading water as a mulitple of the default delay while on ground.",
            acceptableValues: new AcceptableValueRange<float>(1f, 5f),
            synced: true
        );

        RestingStaminaRegenRate = Config.BindConfigInOrder(
            GlobalSection,
            "Treading Water Stamina Regen Rate",
            0.5f,
            "How fast you regenerate stamina while treading water as a mulitple of the default regen rate while on ground.",
            acceptableValues: new AcceptableValueRange<float>(0f, 1f),
            synced: true
        );

        UnderwaterRestingStaminaDrainRate = Config.BindConfigInOrder(
            GlobalSection,
            "Underwater Stamina Drain",
            0.5f,
            "How fast you drain stamina while floating underwater without moving as a mulitple of the default swimming drain.",
            acceptableValues: new AcceptableValueRange<float>(0f, 2f),
            synced: true
        );

        MaxSwimSpeed = Config.BindConfigInOrder(
            GlobalSection,
            "Max Swim Speed",
            3f,
            "Peak speed when sprinting while swimming.",
            acceptableValues: new AcceptableValueRange<float>(2f, 5f),
            synced: true
        );

        ColorDarknessFactor = Config.BindConfigInOrder(
            GlobalSection,
            "Color Darkness Factor",
            0.0092f,
            "How quickly colors become darker as you dive deeper.",
            acceptableValues: new AcceptableValueRange<float>(0f, 1f),
            synced: false
        );

        FogDensityFactor = Config.BindConfigInOrder(
            GlobalSection,
            "Fog Density Factor",
            0.00092f,
            "How quickly the fog gets thicker as you dive deeper.",
            acceptableValues: new AcceptableValueRange<float>(0f, 0.5f),
            synced: false
        );
        MinFogDensity = Config.BindConfigInOrder(
            GlobalSection,
            "Min Fog Density",
            0.1f,
            "Minimum fog density underwater regardless of depth.",
            acceptableValues: new AcceptableValueRange<float>(0.05f, 1f),
            synced: false
        );
        MaxFogDensity = Config.BindConfigInOrder(
            GlobalSection,
            "Max Fog Density",
            2f,
            "Maximum fog density underwater regardless of depth.",
            acceptableValues: new AcceptableValueRange<float>(1f, 5f),
            synced: false
        );
    }

    public void OnDestroy()
    {
        Config.Save();
    }

    /// <summary>
    ///    Checks if the player is allows to dive in the current environment
    /// </summary>
    /// <returns></returns>
    public bool IsEnvAllowed()
    {
        return EnvMan.instance.GetCurrentEnvironment().m_name != "SunkenCrypt";
    }

}
