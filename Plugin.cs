using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HoverToot
{
    public record MetaData
    {
        public const string PLUGIN_NAME = "HoverToot";
        public const string PLUGIN_GUID = "org.crispykevin.hovertoot";
        public const string PLUGIN_VERSION = "2.1.0";
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

        public Plugin()
        {
            Plugin.Logger = base.Logger; // Set my static logger that hides the base logger, equal to the base logger

            // Bind configs with file

            AlwaysStartEnabled = Config.Bind("General", "Always Start Enabled", false,
                "Always start enabled. Keep in mind progress and achievements will be therefore always be disabled."
            );
            EarlyStartDistance = Config.Bind("General", "Early Start Distance", 10f,
                "Distance before each note (on the x axis, not in time) to begin tooting. This is in arbitrary units and might need tweaking."
            );
            LateEndDistance = Config.Bind("General", "Late End Distance", 25f,
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
                OptionalTrombSettings.AddSlider(page, 0.0f, 100.0f, 0.1f, false, EarlyStartDistance);
                OptionalTrombSettings.AddSlider(page, 0.0f, 100.0f, 0.1f, false, LateEndDistance);
            }

            RealKeyCode = ToggleKey.Value.ToKeyCode();
            // Whenever ToggleKey changes, update RealKeyCode
            ToggleKey.SettingChanged += (sender, e) =>
            {
                var args = e as SettingChangedEventArgs;
                var key = args.ChangedSetting.BoxedValue as SimpleKeyCode?;
                if (!key.HasValue)
                {
                    Logger.LogError("Uh this shouldn't happen (1)");
                    RealKeyCode = KeyCode.F7;
                }
                else
                    RealKeyCode = key.Value.ToKeyCode();
            };
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
        // Determines whether character should be tooting.
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

            Plugin.ShouldPlayNote = (xpos >= start && xpos <= end);
        }


        // If plugin is enabled, override result of isNoteButtonPressed
        [HarmonyPostfix]
        [HarmonyPatch("isNoteButtonPressed")] // TODO: Replace string w/ method whenever gamelib updates
        static void Postfix(ref bool __result)
        {
            if (Plugin.Enabled)
                __result = Plugin.ShouldPlayNote;
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
