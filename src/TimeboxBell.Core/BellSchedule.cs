namespace TimeboxBell.Core;

/// <summary>Rozvrh zvonění zarovnaný na hodiny: pro každou minutu v hodině nejvýš jedno pravidlo.</summary>
public sealed class BellSchedule
{
    private readonly BellType?[] _byMinute = new BellType?[60];

    public BellSchedule(IEnumerable<BellRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var list = new List<BellRule>();
        foreach (var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule, nameof(rules));
            if (rule.Minute is < 0 or > 59)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rules), rule.Minute, $"Minuta musí být v rozsahu 0–59, je {rule.Minute}.");
            }

            if (_byMinute[rule.Minute] is not null)
            {
                throw new ArgumentException($"Minuta {rule.Minute} je v rozvrhu vícekrát.", nameof(rules));
            }

            _byMinute[rule.Minute] = rule.Type;
            list.Add(rule);
        }

        Rules = list.OrderBy(r => r.Minute).ToArray();
    }

    /// <summary>Pravidla seřazená podle minuty.</summary>
    public IReadOnlyList<BellRule> Rules { get; }

    /// <summary>Výchozí rozvrh: :23/:53 varování, :25/:55 konec pomodora, :00/:30 nová půlhodina.</summary>
    public static BellSchedule Default { get; } = new(
    [
        new BellRule(0, BellType.NewHalfHour),
        new BellRule(23, BellType.Warning),
        new BellRule(25, BellType.PomodoroEnd),
        new BellRule(30, BellType.NewHalfHour),
        new BellRule(53, BellType.Warning),
        new BellRule(55, BellType.PomodoroEnd),
    ]);

    /// <summary>
    /// Najde nejbližší zvonění po minutě, ve které leží <paramref name="localNow"/> (místní čas na
    /// nástěnných hodinách). Vrátí <c>null</c>, pokud je rozvrh prázdný.
    /// </summary>
    public UpcomingBell? GetNext(DateTime localNow)
    {
        var minuteStart = new DateTime(
            localNow.Year, localNow.Month, localNow.Day, localNow.Hour, localNow.Minute, 0, localNow.Kind);
        for (var i = 1; i <= 60; i++)
        {
            var at = minuteStart.AddMinutes(i);
            if (_byMinute[at.Minute] is { } type)
            {
                return new UpcomingBell(at, type);
            }
        }

        return null;
    }

    /// <summary>Vrátí druh zvonění pro danou minutu v hodině, nebo <c>null</c>, pokud v ní nic nezvoní.</summary>
    public BellType? GetBellForMinute(int minute)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minute);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minute, 59);
        return _byMinute[minute];
    }
}
