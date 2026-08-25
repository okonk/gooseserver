namespace Goose.Testing;

internal static class SettingsBridge
{
    // Exists only while the GameWorld.Settings static bridge exists; deleted with it (Task 6).
    public static GooseSettings Swap(GooseSettings newSettings)
    {
        var previous = GameWorld.Settings;
        GameWorld.Settings = newSettings;
        return previous;
    }
}
