using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FathomlessVoidling
{
    public static class ModConfig
    {
        internal static ConfigFile FVConfig;
        public static ConfigEntry<bool> enableFog;
        public static ConfigEntry<float> jointBaseHealth;
        public static ConfigEntry<float> jointLevelHealth;
        public static ConfigEntry<float> mazeCooldown;
        public static ConfigEntry<float> multiBeamCooldown;
        public static ConfigEntry<float> singularityCooldown;
        public static ConfigEntry<float> hauntP1Cooldown;
        public static ConfigEntry<float> hauntP2Cooldown;
        public static ConfigEntry<float> gravityBombChance;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            if (Main.RooInstalled)
                InitRoO();

            FVConfig = new ConfigFile(Paths.ConfigPath + "\\com.Nuxlar.FathomlessVoidling.cfg", true);

            enableFog = FVConfig.BindOption(
                "General",
                "Enable Fog",
                false,
                "Enables void fog in Void Locus.");

            jointBaseHealth = FVConfig.BindOptionSteppedSlider(
                "Stats",
                "Base Health",
                1500f,
                25f,
                "Base health for joints",
                500f, 3000f);
            jointLevelHealth = FVConfig.BindOptionSteppedSlider(
                "Stats",
                "Level Health",
                425f,
                25f,
                "Health gained per level for joints",
                100f, 1000f);

            mazeCooldown = FVConfig.BindOptionSteppedSlider(
                "Skills",
                "Maze Cooldown",
                45f,
                1f,
                "Cooldown for Maze attack",
                10f, 90f);
            multiBeamCooldown = FVConfig.BindOptionSteppedSlider(
                "Skills",
                "Portal Beam Cooldown",
                15f,
                1f,
                "Cooldown for Portal Beam attack",
                5f, 30f);
            singularityCooldown = FVConfig.BindOptionSteppedSlider(
                "Skills",
                "Singularity Cooldown",
                60f,
                1f,
                "Cooldown for Wandering Singularity attack",
                10f, 90f);

            hauntP1Cooldown = FVConfig.BindOptionSteppedSlider(
                "Haunt",
                "P1 Cooldown",
                30f,
                1f,
                "Gravity bomb downtime in Phase 1",
                5f, 120f);
            hauntP2Cooldown = FVConfig.BindOptionSteppedSlider(
                "Haunt",
                "P2/P3 Cooldown",
                20f,
                1f,
                "Gravity bomb downtime in Phase 2/3",
                5f, 120f);
            gravityBombChance = FVConfig.BindOptionSteppedSlider(
                "Haunt",
                "Gravity Bomb Chance",
                0.15f,
                0.01f,
                "Chance per second to fire a gravity bomb during uptime (higher chance = much more bombs)",
                0.01f, 1f);

            WipeConfig();
        }

        private static void WipeConfig()
        {
            PropertyInfo orphanedEntriesProp = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(FVConfig);
            orphanedEntries.Clear();

            FVConfig.Save();
        }

        #region Config Binding
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void InitRoO()
        {
            try
            {
                RiskOfOptions.ModSettingsManager.SetModDescription("Fathomless Voidling", Main.PluginGUID, Main.PluginName);
                string pathString = Path.GetDirectoryName(Main.Instance.Info.Location);
                var iconStream = File.ReadAllBytes(Path.Combine(pathString, "icon.png"));
                var tex = new Texture2D(256, 256);
                tex.LoadImage(iconStream);
                var icon = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));

                RiskOfOptions.ModSettingsManager.SetModIcon(icon, Main.PluginGUID, Main.PluginName);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOption<T>(this ConfigFile myConfig, string section, string name, T defaultValue, string description = "", bool restartRequired = true)
        {
            if (defaultValue is int or float)
            {
                return myConfig.BindOptionSlider(section, name, defaultValue, description, 0, 20, restartRequired);
            }
            if (string.IsNullOrEmpty(description))
                description = name;

            if (restartRequired)
                description += " (restart required)";

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, null));

            if (Main.RooInstalled)
                TryRegisterOption(configEntry, restartRequired);

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSlider<T>(this ConfigFile myConfig, string section, string name, T defaultValue, string description = "", float min = 0, float max = 20, bool restartRequired = true)
        {
            if (defaultValue is not int and not float)
            {
                return myConfig.BindOption(section, name, defaultValue, description, restartRequired);
            }

            if (string.IsNullOrEmpty(description))
                description = name;

            description += " (Default: " + defaultValue + ")";

            if (restartRequired)
                description += " (restart required)";

            AcceptableValueBase range = typeof(T) == typeof(int)
                ? new AcceptableValueRange<int>((int)min, (int)max)
                : new AcceptableValueRange<float>(min, max);

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, range));

            if (Main.RooInstalled)
                TryRegisterOptionSlider(configEntry, min, max, restartRequired);

            return configEntry;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static ConfigEntry<T> BindOptionSteppedSlider<T>(this ConfigFile myConfig, string section, string name, T defaultValue, float increment = 1f, string description = "", float min = 0, float max = 20, bool restartRequired = true)
        {
            if (string.IsNullOrEmpty(description))
                description = name;

            description += " (Default: " + defaultValue + ")";

            if (restartRequired)
                description += " (restart required)";

            var configEntry = myConfig.Bind(section, name, defaultValue, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

            if (Main.RooInstalled)
                TryRegisterOptionSteppedSlider(configEntry, increment, min, max, restartRequired);

            return configEntry;
        }
        #endregion

        #region RoO
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOption<T>(ConfigEntry<T> entry, bool restartRequired)
        {
            if (entry is ConfigEntry<string> stringEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StringInputFieldOption(stringEntry, new RiskOfOptions.OptionConfigs.InputFieldConfig()
                {
                    submitOn = RiskOfOptions.OptionConfigs.InputFieldConfig.SubmitEnum.OnExitOrSubmit,
                    restartRequired = restartRequired
                }), Main.PluginGUID, Main.PluginName);
            }
            else if (entry is ConfigEntry<bool> boolEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(boolEntry, restartRequired), Main.PluginGUID, Main.PluginName);
            }
            else if (entry is ConfigEntry<KeyboardShortcut> shortCutEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(shortCutEntry, restartRequired), Main.PluginGUID, Main.PluginName);
            }
            else if (typeof(T).IsEnum)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.ChoiceOption(entry, restartRequired), Main.PluginGUID, Main.PluginName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSlider<T>(ConfigEntry<T> entry, float min, float max, bool restartRequired)
        {
            if (entry is ConfigEntry<int> intEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(intEntry, new RiskOfOptions.OptionConfigs.IntSliderConfig()
                {
                    min = (int)min,
                    max = (int)max,
                    formatString = "{0:0.00}",
                    restartRequired = restartRequired
                }), Main.PluginGUID, Main.PluginName);
            }
            else if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(floatEntry, new RiskOfOptions.OptionConfigs.SliderConfig()
                {
                    min = min,
                    max = max,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired
                }), Main.PluginGUID, Main.PluginName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void TryRegisterOptionSteppedSlider<T>(ConfigEntry<T> entry, float increment, float min, float max, bool restartRequired)
        {
            if (entry is ConfigEntry<float> floatEntry)
            {
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StepSliderOption(floatEntry, new RiskOfOptions.OptionConfigs.StepSliderConfig()
                {
                    increment = increment,
                    min = min,
                    max = max,
                    FormatString = "{0:0.00}",
                    restartRequired = restartRequired
                }), Main.PluginGUID, Main.PluginName);
            }
        }
        #endregion
    }
}
