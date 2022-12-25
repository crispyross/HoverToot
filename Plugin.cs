using HoverToot;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace HoverToot
{
    public record MetaData
    {
        public const string PLUGIN_NAME = "HoverToot";
        public const string PLUGIN_GUID = "org.crispykevin.hovertoot";
        public const string PLUGIN_VERSION = "2.0.1";
    }

    [BepInPlugin(MetaData.PLUGIN_GUID, MetaData.PLUGIN_NAME, MetaData.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // All of these are temporarily null only when the plugin initializes, so I use null-forgiving operator

        public static new ManualLogSource Logger = null!;
        public static ConfigEntry<float> EarlyStartDistance { get; private set; } = null!;
        public static ConfigEntry<float> LateEndDistance { get; private set; } = null!;
        public static ConfigEntry<bool> AlwaysStartEnabled { get; private set; } = null!;
        public static ConfigEntry<SimpleKeyCode> ToggleKey { get; private set; } = null!;
        public static KeyCode RealKeyCode;

        private static bool _didToggleThisSong = false;
        public static bool DidToggleThisSong
        {
            get => _didToggleThisSong;
            set
            {
                if (_didToggleThisSong != value)
                    Logger.LogInfo($"DidToggleThisSong -> {value}");
                _didToggleThisSong = value;

            }
        }


        private static bool _enabled;
        public static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                    Logger.LogInfo(value ? "Enabled" : "Disabled");
                DidToggleThisSong = true;
                _enabled = value;
            }
        }

        public static bool ShouldPlayNote { get; set; } = false;
        public static bool DidMousedownFire { get; set; } = false;


        public Plugin()
        {
            Plugin.Logger = base.Logger; // Set my static logger that hides the base logger, equal to the base logger

            // Bind configs with file

            AlwaysStartEnabled = Config.Bind("General", "Always Start Enabled", false,
                "Always start enabled. Keep in mind progress and achievements will be therefore always be disabled."
            );
            EarlyStartDistance = Config.Bind("General", "Early Start Distance", 20f,
                "Distance before each note (on the x axis, not in time) to begin tooting. This is in arbitrary units and might need tweaking."
            );
            LateEndDistance = Config.Bind("General", "Late End Distance", 10f,
                "Distance after each note (on the x axis, not in time) to stop tooting. This is in arbitrary units and might need tweaking."
            );
            
            ToggleKey = Config.Bind("Input", "ToggleKey", SimpleKeyCode.F7,
                "Key code to toggle off and on HoverToot."
            );


            // Add GUI TrombSettings stuff
            object? page = OptionalTrombSettings.GetConfigPage("HoverToot");
            if (page != null)
            {
                
                OptionalTrombSettings.Add(page, AlwaysStartEnabled);
                OptionalTrombSettings.Add(page, ToggleKey);
                OptionalTrombSettings.AddSlider(page, 0.0f, 200.0f, 0.1f, false, EarlyStartDistance);
                OptionalTrombSettings.AddSlider(page, 0.0f, 200.0f, 0.1f, false, LateEndDistance);
            }



            // Parse configs

            /*if (Enum.TryParse(ToggleKey.Value, out KeyCode key))
            {
                ToggleKey = key;
            }
            else
            {
                Logger.LogWarning($"Invalid key: {ToggleKey.Value}");
                ToggleKey.Value = "F7";
                ToggleKey = KeyCode.F7;
            }*/

            RealKeyCode = ToggleKey.Value.ToKeyCode();
            ToggleKey.SettingChanged += ToggleKey_SettingChanged;
        }

        private void ToggleKey_SettingChanged(object sender, EventArgs e)
        {
            var args = e as SettingChangedEventArgs;
            var key = args.ChangedSetting.BoxedValue as SimpleKeyCode?;
            if (!key.HasValue)
                Logger.LogError("Uh this shouldn't happen (1)");
            else
                RealKeyCode = key.Value.ToKeyCode();

        }

        private void Awake()
        {
            new Harmony(MetaData.PLUGIN_GUID).PatchAll();
            Logger.LogInfo($"Plugin {MetaData.PLUGIN_GUID} v{MetaData.PLUGIN_VERSION} is loaded!");
        }
        
    }

    [HarmonyPatch(typeof(GameController))]
    internal class GameControllerPatch
    {
        // Prefix patch determines whether character should be tooting.
        [HarmonyPatch(nameof(GameController.Update))]
        static void Prefix(GameController __instance)
        {
            if (Input.GetKeyDown(Plugin.RealKeyCode))
            {
                Plugin.Enabled = !Plugin.Enabled;
            }


            var self = __instance; // bro __instance is a pain to type
            var xpos = Mathf.Abs(self.noteholderr.anchoredPosition3D.x - self.zeroxpos);
            var start = self.currentnotestart - Plugin.EarlyStartDistance.Value;
            var end = self.currentnoteend + Plugin.LateEndDistance.Value;
            // Log.LogInfo($"xpos {xpos}, start {start}, end {end}");
            Plugin.ShouldPlayNote = (xpos >= start && xpos <= end);
            if (!Plugin.ShouldPlayNote)
            {
                Plugin.DidMousedownFire = false;
            }
        }



        delegate bool TypeOfGetMouseButton(int whichBtn);
        static bool PatchedGetMouseButton(int whichBtn)
        {
            return Plugin.Enabled ? Plugin.ShouldPlayNote : Input.GetMouseButton(whichBtn);
        }
        static bool PatchedGetMouseButtonDown(int whichBtn)
        {
            if (!Plugin.Enabled)
                return Input.GetMouseButtonDown(whichBtn);
            if (!Plugin.DidMousedownFire && Plugin.ShouldPlayNote)
            {
                Plugin.DidMousedownFire = true;
                return true;
            }
            return false;
        }

        // Transpiler patches call to GetMouseButton to use value of shouldPlayNote instead, if enabled.
        [HarmonyPatch(nameof(GameController.Update))]
        [HarmonyAfter(new string[] { "InputFix" })]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                var inst = codes[i];
                if (inst.opcode == OpCodes.Call)
                {
                    var operand = inst.operand as MethodInfo;

                    if (operand?.Name == "GetMouseButton")
                    {
                        codes[i].operand = ((TypeOfGetMouseButton)PatchedGetMouseButton).Method;
                        Plugin.Logger.LogInfo("Patched GetMouseButton");
                    }
                    // Used in InputFix
                    else if (operand?.Name == "GetMouseButtonDown")
                    {
                        codes[i].operand = ((TypeOfGetMouseButton)PatchedGetMouseButtonDown).Method;
                        Plugin.Logger.LogInfo("Patched GetMouseButtonDown");
                    }
                }
            }
            return codes.AsEnumerable();
        }


        [HarmonyPatch(nameof(GameController.startSong))]
        [HarmonyPostfix]
        static void DisableAtStartOfEachSong()
        {
            if (Plugin.AlwaysStartEnabled.Value)
            {
                Plugin.Enabled = true;
            }
            else
            {
                Plugin.Enabled = false;
                Plugin.DidToggleThisSong = false;
            }

        }

        [HarmonyPatch(nameof(GameController.Start))]
        [HarmonyPostfix]
        static void AddIndicator()
        {
            const string comboPath = "GameplayCanvas/UIHolder/maxcombo/maxcombo_shadow";
            const string parentPath = "GameplayCanvas/UIHolder";
            var comboObject = GameObject.Find(comboPath);
            if (comboObject == null)
            {
                Plugin.Logger.LogError("Unable to find combo text, the hover toot indicator will not be present.");
                return;
            }
            var indicator = UnityEngine.Object.Instantiate(comboObject, comboObject.transform);
            indicator.AddComponent<Indicator>();

            var parent = GameObject.Find(parentPath);
            if (parent != null)
            {
                indicator.transform.parent = parent.transform;
            }
        }
    }
}
