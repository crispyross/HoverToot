using BepInEx.Configuration;
using System;
using System.Linq;
using System.Reflection;

namespace HoverToot
{
    public static class OptionalTrombSettings
    {
        public static object? GetConfigPage(string pageName)
        {
            try
            { 
                Type? trombConfig = null;
                trombConfig = Type.GetType("TrombSettings.TrombConfig, TrombSettings");
                if (trombConfig == null)
                {
                    Plugin.Logger.LogInfo("TrombSettings not found.");
                    return null;
                }

                var trombSettingsInstance = trombConfig.GetField("TrombSettings").GetValue(null);
                var indexerMethod = trombSettingsInstance.GetType().GetIndexer(typeof(string));
                var settingsPage = indexerMethod.GetGetMethod().Invoke(trombSettingsInstance, new object[] { pageName });
                return settingsPage;
            }
            catch (Exception e)
            {
                Plugin.Logger.LogWarning("Exception trying to get config page. Reporting TrombSettings as not found.");
                Plugin.Logger.LogWarning(e.Message);
                Plugin.Logger.LogWarning(e.StackTrace);
                return null;
            }
        }

        public static void AddSlider(object page, float min, float max, float increment, bool integerOnly, ConfigEntryBase entry)
        {
            try
            {
                Type? clazz = Type.GetType("TrombSettings.StepSliderConfig, TrombSettings");
                if (clazz == null)
                    return;
                var ctor = clazz.GetConstructor(new Type[] { typeof(float), typeof(float), typeof(float), typeof(bool), typeof(ConfigEntryBase) });
                var slider = ctor?.Invoke(new object[] { min, max, increment, integerOnly, entry });

                if (slider != null)
                {
                    // Find "public new void Add(BaseConfig configEntry)"
                    Type? baseConfigClass = Type.GetType("TrombSettings.BaseConfig, TrombSettings");
                    var addMethod = page.GetType().GetMethod("Add", new Type[] { baseConfigClass });
                    addMethod.Invoke(page, new object[] { slider });
                }
                else
                    Plugin.Logger.LogError("Couldn't create slider!");
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError("Exception trying to create slider. Reporting TrombSettings as not found.");
                Plugin.Logger.LogError(e.Message);
                Plugin.Logger.LogError(e.StackTrace);
            }

        }


        public static void Add(object page, ConfigEntryBase entry)
        {
            var addFn = page.GetType().GetMethod("Add", new Type[] { typeof(ConfigEntryBase) });
            addFn.Invoke(page, new object[] { entry });
        }
    }


    internal static class TypeExtensions
    {
        // From https://stackoverflow.com/a/55457150

        public static PropertyInfo GetIndexer(this Type type, params Type[] arguments) => type.GetProperties().First(x => x.GetIndexParameters().Select(y => y.ParameterType).SequenceEqual(arguments));
    }
}
