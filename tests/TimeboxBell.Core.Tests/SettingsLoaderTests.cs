namespace TimeboxBell.Core.Tests;

public sealed class SettingsLoaderTests : IDisposable
{
    private const string Base = @"D:\app";

    private readonly string _dir = Directory.CreateTempSubdirectory("TimeboxBell.Settings.").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = SettingsLoader.Load(Path.Combine(_dir, "neni.json"));

        Assert.Same(TimeboxBellSettings.Default, settings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{ // jen komentář\n }")]
    public void Parse_EmptyContent_ReturnsDefaults(string json)
    {
        AssertSettings(TimeboxBellSettings.Default, Parse(json));
    }

    [Fact]
    public void Parse_FullFile_ReadsEverything()
    {
        var settings = Parse("""
            {
              "Bells": [ { "Minute": 50, "Type": "Warning" }, { "Minute": 10, "Type": "pomodoroend" } ],
              "Sounds": { "Warning": "C:\\a.wav", "PomodoroEnd": "zvuky\\b.wav", "NewHalfHour": "" },
              "PreventSleep": false,
            }
            """);

        Assert.Equal([new BellRule(10, BellType.PomodoroEnd), new BellRule(50, BellType.Warning)], settings.Schedule.Rules);
        Assert.Equal(@"C:\a.wav", settings.Sounds.GetPath(BellType.Warning));
        Assert.Equal(@"D:\app\zvuky\b.wav", settings.Sounds.GetPath(BellType.PomodoroEnd));
        Assert.Null(settings.Sounds.GetPath(BellType.NewHalfHour)); // "" = jen pípnutí
        Assert.False(settings.PreventSleep);
    }

    [Fact]
    public void Parse_MissingKeys_UseDefaultsPerKey()
    {
        var settings = Parse("""{ "Sounds": { "Warning": "C:\\jiny.wav" } }""");

        Assert.Same(BellSchedule.Default, settings.Schedule);
        Assert.True(settings.PreventSleep);
        Assert.Equal(@"C:\jiny.wav", settings.Sounds.GetPath(BellType.Warning));
        Assert.Equal(BellSounds.Default.GetPath(BellType.PomodoroEnd), settings.Sounds.GetPath(BellType.PomodoroEnd));
        Assert.Equal(BellSounds.Default.GetPath(BellType.NewHalfHour), settings.Sounds.GetPath(BellType.NewHalfHour));
    }

    [Fact]
    public void Parse_EmptyBellList_MeansNoBells()
    {
        Assert.Empty(Parse("""{ "Bells": [] }""").Schedule.Rules);
    }

    [Theory]
    [InlineData("""{ "Bells": [ { "Minute": 61, "Type": "Warning" } ] }""", "Bells[0]", "61", "0–59")]
    [InlineData("""{ "Bells": [ { "Minute": -1, "Type": "Warning" } ] }""", "Bells[0]", "-1")]
    [InlineData("""{ "Bells": [ { "Minute": 5, "Type": "Warning" }, { "Minute": 5, "Type": "NewHalfHour" } ] }""", "Bells[1]", "Bells[0]")]
    [InlineData("""{ "Bells": [ { "Type": "Warning" } ] }""", "Bells[0]", "Minute")]
    [InlineData("""{ "Bells": [ { "Minute": 5 } ] }""", "Bells[0]", "Type")]
    [InlineData("""{ "Bells": [ { "Minute": 5, "Type": "Zvonek" } ] }""", "Zvonek", "Warning, PomodoroEnd, NewHalfHour")]
    [InlineData("""{ "Bells": [ { "Minute": 5, "Type": "7" } ] }""", "\"7\"")]
    [InlineData("""{ "Bells": [ null ] }""", "Bells[0]")]
    [InlineData("""{ "Sounds": { "Varovani": "a.wav" } }""", "Varovani")]
    [InlineData("""{ "Sounds": { "Warning": null } }""", "Sounds.Warning")]
    [InlineData("""{ "Bels": [] }""", "Bels")]
    [InlineData("""{ "Bells": [ { "Minute": 5, "Type": "Warning", "Zvuk": "x" } ] }""", "Zvuk")]
    [InlineData("""{ "PreventSleep": "ano" }""", "PreventSleep", "řádku 1")]
    [InlineData("""{ "Bells": [ { "Minute": "pět", "Type": "Warning" } ] }""", "Minute")]
    [InlineData("{\n  \"Bells\": [\n    { \"Minute\": 5, }\n    { \"Minute\": 6 }\n  ]\n}", "řádku 4")]
    [InlineData("{ \"Bells\": [ ", "chybný")]
    [InlineData("[]", "chybný")]
    public void Parse_InvalidContent_ThrowsReadableError(string json, params string[] expectedFragments)
    {
        var error = Assert.Throws<SettingsException>(() => Parse(json));

        Assert.Contains("test.json", error.Message);
        Assert.All(expectedFragments, f => Assert.Contains(f, error.Message));
    }

    [Fact]
    public void Load_ReadsFileAndResolvesRelativeSoundsAgainstItsFolder()
    {
        var path = Path.Combine(_dir, SettingsLoader.FileName);
        File.WriteAllText(path, """{ "Sounds": { "Warning": "muj.wav" } }""");

        var settings = SettingsLoader.Load(path);

        Assert.Equal(Path.Combine(_dir, "muj.wav"), settings.Sounds.GetPath(BellType.Warning));
    }

    [Fact]
    public void Load_InvalidFile_ErrorNamesTheFile()
    {
        var path = Path.Combine(_dir, SettingsLoader.FileName);
        File.WriteAllText(path, "{ nejde to }");

        var error = Assert.Throws<SettingsException>(() => SettingsLoader.Load(path));

        Assert.Contains(path, error.Message);
    }

    private static TimeboxBellSettings Parse(string json) => SettingsLoader.Parse(json, Base, "test.json");

    internal static void AssertSettings(TimeboxBellSettings expected, TimeboxBellSettings actual)
    {
        Assert.Equal(expected.Schedule.Rules, actual.Schedule.Rules);
        Assert.Equal(expected.PreventSleep, actual.PreventSleep);
        Assert.All(Enum.GetValues<BellType>(), t => Assert.Equal(expected.Sounds.GetPath(t), actual.Sounds.GetPath(t)));
    }
}
