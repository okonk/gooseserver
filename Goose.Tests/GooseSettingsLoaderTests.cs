using System.IO;
using System.Text.Json;
using Goose;
using Xunit;

namespace Goose.Tests
{
    public class GooseSettingsLoaderTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "settings-loader-" + Guid.NewGuid().ToString("N"));
        private readonly string baseDir;
        private readonly string dataDir;

        public GooseSettingsLoaderTests()
        {
            baseDir = Path.Combine(root, "base");
            dataDir = Path.Combine(root, "data");
            Directory.CreateDirectory(baseDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }

        private static string SettingsPath(string dir) => Path.Combine(dir, "GooseSettings.json");

        private static void WriteSettings(string dir, string serverName)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath(dir), JsonSerializer.Serialize(
                new GooseSettings { ServerName = serverName, StartingMapID = 42 },
                JsonHelper.SettingsOptions));
        }

        [Fact]
        public void Load_DataFilePresent_ReturnsDataValues()
        {
            WriteSettings(baseDir, "shipped");
            WriteSettings(dataDir, "operator");

            var settings = GooseSettingsLoader.Load(baseDir, dataDir);

            Assert.Equal("operator", settings.ServerName);
            Assert.Equal(42, settings.StartingMapID);
        }

        [Fact]
        public void Load_DataFileMissing_CopiesShippedFileAndReturnsItsValues()
        {
            WriteSettings(baseDir, "shipped");

            var settings = GooseSettingsLoader.Load(baseDir, dataDir);

            Assert.Equal("shipped", settings.ServerName);
            Assert.True(File.Exists(SettingsPath(dataDir)));
            Assert.Equal(
                File.ReadAllText(SettingsPath(baseDir)),
                File.ReadAllText(SettingsPath(dataDir)));
        }

        [Fact]
        public void Load_BothFilesMissing_ThrowsFileNotFoundExceptionNamingDataTarget()
        {
            var ex = Assert.Throws<FileNotFoundException>(
                () => GooseSettingsLoader.Load(baseDir, dataDir));

            Assert.Equal(SettingsPath(dataDir), ex.FileName);
        }

        [Fact]
        public void Load_MalformedJson_Throws()
        {
            Directory.CreateDirectory(dataDir);
            File.WriteAllText(SettingsPath(dataDir), "{ this is not json");

            Assert.Throws<JsonException>(() => GooseSettingsLoader.Load(baseDir, dataDir));
        }

        [Fact]
        public void Load_SameBaseAndDataRoot_ReadsFileWithoutCopying()
        {
            WriteSettings(baseDir, "shipped");
            string original = File.ReadAllText(SettingsPath(baseDir));

            var settings = GooseSettingsLoader.Load(baseDir, baseDir);

            Assert.Equal("shipped", settings.ServerName);
            Assert.Equal(original, File.ReadAllText(SettingsPath(baseDir)));
        }
    }
}
