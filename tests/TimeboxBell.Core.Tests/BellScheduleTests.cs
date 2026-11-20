namespace TimeboxBell.Core.Tests;

public class BellScheduleTests
{
    [Fact]
    public void Default_MapsMinutesToBellTypes()
    {
        var expected = new Dictionary<int, BellType>
        {
            [0] = BellType.NewHalfHour,
            [23] = BellType.Warning,
            [25] = BellType.PomodoroEnd,
            [30] = BellType.NewHalfHour,
            [53] = BellType.Warning,
            [55] = BellType.PomodoroEnd,
        };

        for (var minute = 0; minute < 60; minute++)
        {
            BellType? want = expected.TryGetValue(minute, out var t) ? t : null;
            Assert.Equal(want, BellSchedule.Default.GetBellForMinute(minute));
        }
    }

    [Fact]
    public void Rules_AreSortedByMinute()
    {
        var schedule = new BellSchedule([new BellRule(40, BellType.Warning), new BellRule(5, BellType.NewHalfHour)]);

        Assert.Equal([5, 40], schedule.Rules.Select(r => r.Minute));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60)]
    public void Constructor_RejectsMinuteOutOfRange(int minute)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BellSchedule([new BellRule(minute, BellType.Warning)]));
    }

    [Fact]
    public void Constructor_RejectsDuplicateMinute()
    {
        Assert.Throws<ArgumentException>(() => new BellSchedule(
            [new BellRule(10, BellType.Warning), new BellRule(10, BellType.PomodoroEnd)]));
    }

    [Fact]
    public void EmptySchedule_NeverRings()
    {
        var schedule = new BellSchedule([]);

        Assert.All(Enumerable.Range(0, 60), m => Assert.Null(schedule.GetBellForMinute(m)));
    }
}
