namespace TimeboxBell.Core.Tests;

public class BellSchedulerTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(23, BellType.Warning)]
    [InlineData(53, BellType.Warning)]
    [InlineData(25, BellType.PomodoroEnd)]
    [InlineData(55, BellType.PomodoroEnd)]
    [InlineData(0, BellType.NewHalfHour)]
    [InlineData(30, BellType.NewHalfHour)]
    public void Check_ReturnsBellForScheduledMinute(int minute, BellType expected)
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, minute, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        Assert.Equal(expected, scheduler.Check());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(22)]
    [InlineData(24)]
    [InlineData(26)]
    [InlineData(29)]
    [InlineData(54)]
    [InlineData(59)]
    public void Check_ReturnsNullOutsideSchedule(int minute)
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, minute, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        Assert.Null(scheduler.Check());
    }

    [Fact]
    public void WholeDaySimulation_EveryBellRingsExactlyOnceWithCorrectType()
    {
        var start = new DateTime(2026, 11, 20, 0, 0, 0);
        var clock = TestClock.At(start);
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        var rings = clock.Run(scheduler, TimeSpan.FromDays(1), Step);

        var expected = new List<(DateTime, BellType)>();
        for (var hour = 0; hour < 24; hour++)
        {
            foreach (var rule in BellSchedule.Default.Rules)
            {
                expected.Add((start.AddHours(hour).AddMinutes(rule.Minute), rule.Type));
            }
        }

        Assert.Equal(144, expected.Count);
        Assert.Equal(expected, rings.Select(r => (r.Local.DateTime, r.Type)));
    }

    [Fact]
    public void RepeatedChecksInSameMinute_RingOnlyOnce()
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, 25, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        Assert.Equal(BellType.PomodoroEnd, scheduler.Check());
        Assert.Null(scheduler.Check());
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Null(scheduler.Check());
        clock.Advance(TimeSpan.FromSeconds(29));
        Assert.Null(scheduler.Check());
    }

    [Fact]
    public void WakeFromSleep_DoesNotReplayMissedBells()
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, 10, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);
        Assert.Null(scheduler.Check());

        // Spánek přes :23, :25, :30, :53 a :55.
        clock.SetLocal(new DateTime(2026, 11, 20, 10, 58, 10));
        var rings = clock.Run(scheduler, TimeSpan.FromMinutes(3), Step);

        Assert.Equal([(new DateTime(2026, 11, 20, 11, 0, 0), BellType.NewHalfHour)],
            rings.Select(r => (r.Local.DateTime, r.Type)));
    }

    [Fact]
    public void WakeInsideBellMinuteThatDidNotRing_RingsOnce()
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, 10, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);
        Assert.Null(scheduler.Check());

        clock.SetLocal(new DateTime(2026, 11, 20, 10, 25, 40));
        var rings = clock.Run(scheduler, TimeSpan.FromSeconds(60), Step);

        Assert.Equal([BellType.PomodoroEnd], rings.Select(r => r.Type));
    }

    [Fact]
    public void WakeInsideBellMinuteThatAlreadyRang_DoesNotRingAgain()
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, 25, 5));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);
        Assert.Equal(BellType.PomodoroEnd, scheduler.Check());

        // Krátký spánek, probuzení ve stejné minutě.
        clock.SetLocal(new DateTime(2026, 11, 20, 10, 25, 45));

        Assert.Null(scheduler.Check());
    }

    [Fact]
    public void ClockMovedBack_DoesNotRingTwice()
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, 20, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);
        var first = clock.Run(scheduler, TimeSpan.FromMinutes(7), Step); // 10:20–10:27

        clock.SetLocal(new DateTime(2026, 11, 20, 10, 22, 30)); // ruční posun o ~5 min zpět
        var second = clock.Run(scheduler, TimeSpan.FromMinutes(9), Step); // 10:22:30–10:31:30

        Assert.Equal([BellType.Warning, BellType.PomodoroEnd], first.Select(r => r.Type));
        Assert.Equal([(new DateTime(2026, 11, 20, 10, 30, 0), BellType.NewHalfHour)],
            second.Select(r => (r.Local.DateTime, r.Type)));
    }

    [Fact]
    public void FallBackToStandardTime_RepeatedHourDoesNotRingTwice()
    {
        // 25. 10. 2026 ve 3:00 CEST se hodiny vrací na 2:00 CET – hodina 2:00–2:59 proběhne dvakrát.
        var clock = TestClock.At(new DateTimeOffset(2026, 10, 25, 1, 56, 0, TimeSpan.FromHours(2)));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        var rings = clock.Run(scheduler, TimeSpan.FromMinutes(134), Step); // 1:56 CEST až 3:10 CET

        Assert.Equal(
            [
                (new DateTime(2026, 10, 25, 2, 0, 0), BellType.NewHalfHour),
                (new DateTime(2026, 10, 25, 2, 23, 0), BellType.Warning),
                (new DateTime(2026, 10, 25, 2, 25, 0), BellType.PomodoroEnd),
                (new DateTime(2026, 10, 25, 2, 30, 0), BellType.NewHalfHour),
                (new DateTime(2026, 10, 25, 2, 53, 0), BellType.Warning),
                (new DateTime(2026, 10, 25, 2, 55, 0), BellType.PomodoroEnd),
                (new DateTime(2026, 10, 25, 3, 0, 0), BellType.NewHalfHour),
            ],
            rings.Select(r => (r.Local.DateTime, r.Type)));
        Assert.All(rings.Take(6), r => Assert.Equal(TimeSpan.FromHours(2), r.Local.Offset)); // první průchod (CEST)
    }

    [Fact]
    public void SpringForwardToSummerTime_SkippedHourDoesNotRingAndNothingDoubles()
    {
        // 29. 3. 2026 ve 2:00 CET se hodiny posouvají na 3:00 CEST.
        var clock = TestClock.At(new DateTime(2026, 3, 29, 1, 56, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        var rings = clock.Run(scheduler, TimeSpan.FromMinutes(34), Step); // 1:56 CET až 3:30 CEST

        Assert.Equal(
            [
                (new DateTime(2026, 3, 29, 3, 0, 0), BellType.NewHalfHour),
                (new DateTime(2026, 3, 29, 3, 23, 0), BellType.Warning),
                (new DateTime(2026, 3, 29, 3, 25, 0), BellType.PomodoroEnd),
            ],
            rings.Select(r => (r.Local.DateTime, r.Type)));
    }

    [Fact]
    public void CustomSchedule_IsUsedInsteadOfDefault()
    {
        var schedule = new BellSchedule([new BellRule(10, BellType.Warning), new BellRule(45, BellType.NewHalfHour)]);
        var clock = TestClock.At(new DateTime(2026, 11, 20, 10, 0, 0));
        var scheduler = new BellScheduler(schedule, clock);

        var rings = clock.Run(scheduler, TimeSpan.FromHours(1), Step);

        Assert.Equal(
            [
                (new DateTime(2026, 11, 20, 10, 10, 0), BellType.Warning),
                (new DateTime(2026, 11, 20, 10, 45, 0), BellType.NewHalfHour),
            ],
            rings.Select(r => (r.Local.DateTime, r.Type)));
    }

    [Fact]
    public void LongRun_HistoryStaysBoundedAndBellsKeepRinging()
    {
        var clock = TestClock.At(new DateTime(2026, 11, 20, 0, 0, 0));
        var scheduler = new BellScheduler(BellSchedule.Default, clock);

        var rings = clock.Run(scheduler, TimeSpan.FromDays(3), TimeSpan.FromSeconds(30));

        Assert.Equal(3 * 24 * 6, rings.Count);
        Assert.Equal(rings.Count, rings.Select(r => r.Local.DateTime).Distinct().Count());
    }

    [Fact]
    public void Constructor_RejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => new BellScheduler(null!, TimeProvider.System));
        Assert.Throws<ArgumentNullException>(() => new BellScheduler(BellSchedule.Default, null!));
    }
}
