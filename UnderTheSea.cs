using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Configs;
using Logging;
using UnityEngine;

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


    //config values
    public static ConfigEntry<string> configGreeting;
    public static ConfigEntry<bool> configDisplayGreeting;
    public static ConfigEntry<bool> showYouCanBreatheMsg;
    public static ConfigEntry<bool> showDivingMsg;
    public static ConfigEntry<string> divingMsg;
    public static ConfigEntry<string> divingCancelledMsg;
    public static ConfigEntry<bool> showSurfacingMsg;
    public static ConfigEntry<string> surfacingMsg;
    public static ConfigEntry<bool> allowRestInWater;
    public static ConfigEntry<bool> owBIPos;
    public static ConfigEntry<float> owBIPosX;
    public static ConfigEntry<float> owBIPosY;
    public static ConfigEntry<float> breatheDrain;
    public static ConfigEntry<bool> allowFastSwimming;
    public static ConfigEntry<float> c_swimStaminaDrainMinSkill;
    public static ConfigEntry<float> c_swimStaminaDrainMaxSkill;
    public static ConfigEntry<bool> ow_staminaRestoreValue;
    public static ConfigEntry<float> ow_staminaRestorPerTick;
    public static ConfigEntry<float> ow_color_brightness_factor;
    public static ConfigEntry<float> ow_fogdensity_factor;
    public static ConfigEntry<float> ow_Min_fogdensity;
    public static ConfigEntry<float> ow_Max_fogdensity;
    public static ConfigEntry<bool> doDebug;


    //character values
    public static float loc_m_m_maxDistance = 0f;
    public static float loc_m_diveAaxis = 0f;
    public static float loc_cam_pos_y = 0;
    public static float char_swim_depth = 0;

    //camera values
    public static bool render_settings_updated_camera = true;
    public static bool set_force_env = false;
    public static float minwaterdist = 0f;

    //water values
    public static Material mai_water_mat = null;
    public static float water_level_camera = 30f;
    public static float water_level_player = 30f;

    //player stamina values
    public static float m_swimStaminaDrainMinSkill = 0f;
    public static float m_swimStaminaDrainMaxSkill = 0f;

    //Oxygen bar
    public static bool dive_timer_is_running = false;
    public static float breathBarRemoveDelay = 2f;
    public static float breathDelayTimer;
    public static float highestOxygen = 1f;
    public static bool has_created_breathe_bar = false;
    public static GameObject loc_breath_bar;
    public static GameObject loc_depleted_breath;
    public static GameObject loc_breath_bar_bg;
    public static GameObject loc_breathe_overlay;
    public static Sprite breath_prog_sprite;
    public static Texture2D breath_prog_tex;
    public static Sprite breath_bg_sprite;
    public static Texture2D breath_bg_tex;
    public static Sprite breath_overlay_sprite;
    public static Texture2D breath_overlay_tex;

    //Water
    public static Shader water_shader;
    public static Texture2D water_texture;
    public static Material water_mat;
    public static Material[] water_volum_list;

    //Env values
    public static string EnvName = "";

    //Diving skill
    public static Skills.SkillType DivingSkillType = 0;
    public static Texture2D dive_texture;
    public static Sprite DivingSprite;
    public static float m_diveSkillImproveTimer = 0f;
    public static float m_minDiveSkillImprover = 0f;

    //Swim speed
    public static float baseSwimSpeed = 2f;
    public static float fastSwimSpeedMultiplier = 0.01f;
    public static float swimStaminaDrainRate = 10f;
    public static float fastSwimSpeed;
    public static float fastSwimStamDrain;

    //Graphics
    public static string graphicsDeviceType;

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
        ConfigurationManagerAttributes isAdminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

        // Local config values
        doDebug = Config.Bind("Debug", "doDebug", false, "Debug mode on or off");
        configGreeting = Config.Bind("Local config", "GreetingText", "Hello, thanks for using the Better Diving Mod by Main Street Gaming!", "");
        configDisplayGreeting = Config.Bind("Local config", "DisplayGreeting", true, "Whether or not to show the greeting text");
        showYouCanBreatheMsg = Config.Bind("Local config", "showYouCanBreatheMsg", false, "Whether or not to show the You Can Breathe message. Disable if the surfacing message is enabled.");
        showDivingMsg = Config.Bind("Local config", "showDivingMsg", true, "Whether or not to show messages when triggering/cancelling diving");
        divingMsg = Config.Bind("Local config", "Diving message", "You prepare to dive");
        divingCancelledMsg = Config.Bind("Local config", "Diving cancelled message", "You remain on the surface");
        showSurfacingMsg = Config.Bind("Local config", "showSurfacingMsg", true, "Whether or not to show a message when surfacing");
        surfacingMsg = Config.Bind("Local config", "Surfacing message", "You have surfaced");
        owBIPos = Config.Bind("Local config", "owBIPos", false, "Override breathe indicator position");
        owBIPosX = Config.Bind("Local config", "owBIPosX", 30f, "Override breathe indicator position X");
        owBIPosY = Config.Bind("Local config", "owBIPosY", 150f, "Override breathe indicator position Y");


        // Server synced config values
        allowRestInWater = Config.Bind("Server config", "allowRestInWater", true, new ConfigDescription("Whether or not to allow stamina regen in water when able to breath and not moving", null, isAdminOnly));
        allowFastSwimming = allowRestInWater = Config.Bind("Server config", "allowFastSwimming", true, new ConfigDescription("Allow fast swimming when holding the Run button", null, isAdminOnly));
        breatheDrain = Config.Bind("Server config", "breatheDrain", 4f, new ConfigDescription("Breathe indicator reduction per tick", null, isAdminOnly));
        c_swimStaminaDrainMinSkill = Config.Bind("Server config", "c_swimStaminaDrainMinSkill", 0.7f, new ConfigDescription("Min stamina drain while diving", null, isAdminOnly));
        c_swimStaminaDrainMaxSkill = Config.Bind("Server config", "c_swimStaminaDrainMaxSkill", 0.8f, new ConfigDescription("Max stamina drain while diving", null, isAdminOnly));
        ow_staminaRestoreValue = Config.Bind("Server config", "ow_staminaRestoreValue", false, new ConfigDescription("Overwrite stamina restore value per tick when take rest in water", null, isAdminOnly));
        ow_staminaRestorPerTick = Config.Bind("Server config", "ow_staminaRestorPerTick", 0.7f, new ConfigDescription("Stamina restore value per tick when take rest in water", null, isAdminOnly));

        // Water - Server synced config values
        ow_color_brightness_factor = Config.Bind("Server config - Water", "ow_color_brightness_factor", -0.0092f, new ConfigDescription(
                                        "Reduce color brightness based on swimdepth (RGB)\n\n" +
                                        "char_swim_depth * ow_color_brightness_factor = correctionFactor.\n\nCorrection:\n" +
                                        "correctionFactor *= -1;\n" +
                                        "red -= red * correctionFactor;\n" +
                                        "green -= green * correctionFactor;\n" +
                                        "blue -= blue * correctionFactor;\n\n" +
                                        "ow_color_brightness_factor must be a negative value", null, isAdminOnly));
        ow_fogdensity_factor = Config.Bind("Server config - Water", "ow_fogdensity_factor", 0.00092f, new ConfigDescription(
                                        "Set fog density based on swimdepth\n\nCorrection:\n" +
                                        "RenderSettings.fogDensity = RenderSettings.fogDensity + (char_swim_depth * ow_fogdensity_factor)", null, isAdminOnly));
        ow_Min_fogdensity = Config.Bind("Server config - Water", "ow_Min_fogdensity", 0.175f, new ConfigDescription("Set min fog density", null, isAdminOnly));
        ow_Max_fogdensity = Config.Bind("Server config - Water", "ow_Max_fogdensity", 2f, new ConfigDescription("Set max fog density", null, isAdminOnly));
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
