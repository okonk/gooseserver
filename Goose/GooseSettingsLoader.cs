using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Goose
{
    public static class GooseSettingsLoader
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static GooseSettings Load()
        {
            return Load(Paths.BaseDir, Paths.DataDir);
        }

        /**
         * Settings come from the data dir when present, otherwise from the shipped copy
         * next to the binaries. The first time a data dir is used, the shipped default is
         * copied there so the operator has one editable copy that survives updates.
         */
        internal static GooseSettings Load(string baseDirectory, string dataDirectory)
        {
            var dataSettings = Path.Combine(dataDirectory, "GooseSettings.json");
            var baseSettings = Path.Combine(baseDirectory, "GooseSettings.json");
            string settingsPath;

            if (File.Exists(dataSettings))
            {
                settingsPath = dataSettings;
            }
            else if (File.Exists(baseSettings))
            {
                if (dataDirectory != baseDirectory)
                {
                    Directory.CreateDirectory(dataDirectory);
                    File.Copy(baseSettings, dataSettings);
                    settingsPath = dataSettings;
                }
                else
                {
                    settingsPath = baseSettings;
                }
            }
            else
            {
                throw new FileNotFoundException(
                    "GooseSettings.json not found in the data dir or next to the server binaries.",
                    dataSettings);
            }

            log.Info("Loaded settings from {0}", settingsPath);

            return JsonSerializer.Deserialize<GooseSettings>(
                File.ReadAllText(settingsPath, Encoding.UTF8),
                JsonHelper.SettingsOptions);
        }
    }
}
