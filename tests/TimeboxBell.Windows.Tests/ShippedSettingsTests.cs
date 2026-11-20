using TimeboxBell.Core;

namespace TimeboxBell.Windows.Tests;

public class ShippedSettingsTests
{
    [Fact]
    public void ShippedAppSettings_IsCopiedNextToExe_AndMatchesBuiltInDefaults()
    {
        var path = SettingsLoader.DefaultPath;
        Assert.True(File.Exists(path), $"Chybí {path}");

        var shipped = SettingsLoader.Load(path);
        var defaults = TimeboxBellSettings.Default;

        Assert.Equal(defaults.Schedule.Rules, shipped.Schedule.Rules);
        Assert.Equal(defaults.PreventSleep, shipped.PreventSleep);
        Assert.All(Enum.GetValues<BellType>(), t => Assert.Equal(defaults.Sounds.GetPath(t), shipped.Sounds.GetPath(t)));
    }
}
