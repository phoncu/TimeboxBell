using TimeboxBell.Core;
using TimeboxBell.Tray;

namespace TimeboxBell.Windows.Tests;

public class TrayApplicationTests
{
    [Fact]
    public void Menu_ShowsNextBellPauseAndExit()
    {
        using var app = Create(new DateTime(2026, 11, 20, 10, 5, 0), out _, out _);

        Assert.Equal("Další zvonění: 10:23 varování", app.NextBellItem.Text);
        Assert.False(app.NextBellItem.Enabled);
        Assert.Equal("Pozastavit", app.PauseItem.Text);
        Assert.Equal("Ukončit", app.ExitItem.Text);
        Assert.Contains("Další zvonění: 10:23 varování", app.ToolTipText);
    }

    [Fact]
    public void PauseClick_StopsBells_ResumeClick_ContinuesWithoutReplayingMissed()
    {
        using var app = Create(new DateTime(2026, 11, 20, 10, 22, 0), out var clock, out var output);

        app.PauseItem.PerformClick();
        Assert.True(app.IsPaused);
        Assert.Equal("Pokračovat", app.PauseItem.Text);
        Assert.StartsWith("Pozastaveno", app.NextBellItem.Text);
        Assert.Contains("pozastaveno", app.ToolTipText);
        Run(app, clock, TimeSpan.FromMinutes(4)); // přes :23 a :25

        app.PauseItem.PerformClick();
        Assert.False(app.IsPaused);
        Assert.Equal("Pozastavit", app.PauseItem.Text);
        Run(app, clock, TimeSpan.FromMinutes(5)); // 10:26–10:31

        Assert.Equal([BellSounds.Default.GetPath(BellType.NewHalfHour)!], output.Played);
    }

    [Fact]
    public void Tick_AfterBell_UpdatesNextBellText()
    {
        using var app = Create(new DateTime(2026, 11, 20, 10, 23, 0), out _, out var output);

        Assert.Equal(BellType.Warning, app.Tick());

        Assert.Equal("Další zvonění: 10:25 konec pomodora", app.NextBellItem.Text);
        Assert.Single(output.Played);
    }

    [Fact]
    public void Start_ShowsIconAndStartsTimer_Exit_StopsIt()
    {
        using var app = Create(new DateTime(2026, 11, 20, 10, 0, 0), out _, out _);
        Assert.False(app.IsRunning);

        app.Start();
        Assert.True(app.IsRunning);

        app.ExitItem.PerformClick();
        Assert.False(app.IsRunning);
    }

    [Fact]
    public void EmptySchedule_SaysSo()
    {
        var settings = TimeboxBellSettings.Default with { Schedule = new BellSchedule([]) };
        using var app = new TrayApplication(settings, new FixedClock(new DateTime(2026, 11, 20, 10, 0, 0)), new RecordingOutput(), true);

        Assert.Equal("Rozvrh je prázdný", app.NextBellItem.Text);
    }

    private static TrayApplication Create(DateTime local, out FixedClock clock, out RecordingOutput output)
    {
        clock = new FixedClock(local);
        output = new RecordingOutput();
        return new TrayApplication(TimeboxBellSettings.Default, clock, output, preventingSleep: true);
    }

    private static void Run(TrayApplication app, FixedClock clock, TimeSpan duration)
    {
        for (var t = TimeSpan.Zero; t < duration; t += TimeSpan.FromSeconds(5))
        {
            app.Tick();
            clock.Advance(TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>Hodiny v pásmu Europe/Prague, posouvané ručně.</summary>
    internal sealed class FixedClock(DateTime local) : TimeProvider
    {
        private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

        private DateTimeOffset _utc = new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone => Zone;

        public override DateTimeOffset GetUtcNow() => _utc;

        public void Advance(TimeSpan by) => _utc += by;
    }

    internal sealed class RecordingOutput : ISoundOutput
    {
        public List<string> Played { get; } = [];

        public void Play(string path) => Played.Add(path);

        public void Beep()
        {
        }
    }
}
