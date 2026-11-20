namespace TimeboxBell.Core.Tests;

public class BellRunnerTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(5);

    [Fact]
    public void Tick_PlaysScheduledBell()
    {
        var (_, runner, output) = Create(new DateTime(2026, 11, 20, 10, 25, 0));

        Assert.Equal(BellType.PomodoroEnd, runner.Tick());
        Assert.Null(runner.Tick());

        Assert.Equal([BellSounds.Default.GetPath(BellType.PomodoroEnd)!], output.Played);
    }

    [Fact]
    public void Paused_NoBellRings_ResumeDoesNotReplayMissed()
    {
        var (clock, runner, output) = Create(new DateTime(2026, 11, 20, 10, 20, 0));

        runner.IsPaused = true;
        var duringPause = RunFor(clock, runner, TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(30)); // přes :23 a :25 až do 10:25:30
        runner.IsPaused = false;
        var afterResume = RunFor(clock, runner, TimeSpan.FromMinutes(5)); // 10:25:30–10:30:30

        Assert.Empty(duringPause);
        Assert.Equal([BellType.NewHalfHour], afterResume); // :25 se po pokračování v téže minutě nepřehrálo
        Assert.Equal([BellSounds.Default.GetPath(BellType.NewHalfHour)!], output.Played);
    }

    [Fact]
    public void PausedAcrossBellMinuteThenResumedInsideIt_DoesNotRing()
    {
        var (clock, runner, output) = Create(new DateTime(2026, 11, 20, 10, 24, 50));
        runner.IsPaused = true;
        RunFor(clock, runner, TimeSpan.FromSeconds(20)); // 10:24:50–10:25:10, minuta :25 nastala v pauze

        runner.IsPaused = false;

        Assert.Null(runner.Tick());
        Assert.Empty(output.Played);
    }

    [Fact]
    public void GetNext_ReturnsNearestBellAfterCurrentMinute()
    {
        var schedule = BellSchedule.Default;

        Assert.Equal(new UpcomingBell(new DateTime(2026, 11, 20, 10, 23, 0), BellType.Warning),
            schedule.GetNext(new DateTime(2026, 11, 20, 10, 1, 30)));
        Assert.Equal(new UpcomingBell(new DateTime(2026, 11, 20, 10, 25, 0), BellType.PomodoroEnd),
            schedule.GetNext(new DateTime(2026, 11, 20, 10, 23, 59)));
        Assert.Equal(new UpcomingBell(new DateTime(2026, 11, 21, 0, 0, 0), BellType.NewHalfHour),
            schedule.GetNext(new DateTime(2026, 11, 20, 23, 55, 10)));
    }

    [Fact]
    public void GetNext_SingleRule_WrapsToNextHour_EmptySchedule_ReturnsNull()
    {
        var single = new BellSchedule([new BellRule(15, BellType.Warning)]);

        Assert.Equal(new DateTime(2026, 11, 20, 11, 15, 0), single.GetNext(new DateTime(2026, 11, 20, 10, 15, 0))!.At);
        Assert.Null(new BellSchedule([]).GetNext(new DateTime(2026, 11, 20, 10, 0, 0)));
    }

    private static List<BellType> RunFor(TestClock clock, BellRunner runner, TimeSpan duration)
    {
        var rang = new List<BellType>();
        var end = clock.GetUtcNow() + duration;
        while (clock.GetUtcNow() < end)
        {
            if (runner.Tick() is { } bell)
            {
                rang.Add(bell);
            }

            clock.Advance(Step);
        }

        return rang;
    }

    private static (TestClock, BellRunner, RecordingOutput) Create(DateTime local)
    {
        var clock = TestClock.At(local);
        var output = new RecordingOutput();
        var runner = new BellRunner(
            new BellScheduler(BellSchedule.Default, clock), new BellPlayer(BellSounds.Default, output));
        return (clock, runner, output);
    }

    private sealed class RecordingOutput : ISoundOutput
    {
        public List<string> Played { get; } = [];

        public void Play(string path) => Played.Add(path);

        public void Beep()
        {
        }
    }
}
