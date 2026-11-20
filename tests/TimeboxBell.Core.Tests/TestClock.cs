namespace TimeboxBell.Core.Tests;

/// <summary>
/// Falešné hodiny pro testy. Na rozdíl od FakeTimeProvider umí jít i dozadu (ruční posun hodin).
/// </summary>
internal sealed class TestClock : TimeProvider
{
    /// <summary>Středoevropský čas (CET/CEST) – nezávislý na časovém pásmu stroje, kde testy běží.</summary>
    public static readonly TimeZoneInfo CentralEurope = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    private DateTimeOffset _utcNow;

    private TestClock(DateTimeOffset now) => _utcNow = now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => CentralEurope;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>Hodiny nastavené na daný okamžik.</summary>
    public static TestClock At(DateTimeOffset now) => new(now);

    /// <summary>Hodiny nastavené na daný místní (středoevropský) čas.</summary>
    public static TestClock At(DateTime local) => new(ToOffset(local));

    public void Advance(TimeSpan by) => _utcNow += by;

    /// <summary>Přestaví hodiny na daný místní čas (i dozadu).</summary>
    public void SetLocal(DateTime local) => _utcNow = ToOffset(local).ToUniversalTime();

    /// <summary>Volá <see cref="BellScheduler.Check"/> po krocích, dokud neuplyne <paramref name="duration"/>.</summary>
    public List<(DateTimeOffset Local, BellType Type)> Run(BellScheduler scheduler, TimeSpan duration, TimeSpan step)
    {
        var rings = new List<(DateTimeOffset, BellType)>();
        var end = _utcNow + duration;
        while (_utcNow < end)
        {
            if (scheduler.Check() is { } bell)
            {
                rings.Add((GetLocalNow(), bell));
            }

            Advance(step);
        }

        return rings;
    }

    private static DateTimeOffset ToOffset(DateTime local)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, CentralEurope.GetUtcOffset(local));
    }
}
