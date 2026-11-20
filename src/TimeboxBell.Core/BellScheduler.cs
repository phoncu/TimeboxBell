namespace TimeboxBell.Core;

/// <summary>
/// Rozhoduje, jestli má v tuto chvíli zaznít zvonění. Volající ho volá pravidelně (např. každých 5 s).
/// </summary>
/// <remarks>
/// <para>Dívá se jen na minutu, která právě běží podle místního času. Zameškaná zvonění (spánek,
/// zastavený program) se proto nedohánějí.</para>
/// <para>Každá minuta místního času (datum + hodina + minuta na nástěnných hodinách) zazvoní nejvýš
/// jednou. Díky tomu nevznikne dvojí zvonění při opakované kontrole v téže minutě, po probuzení
/// uprostřed minuty, po ručním posunu hodin zpět ani v hodině, která se při přechodu na zimní čas
/// opakuje.</para>
/// <para>Třída není vláknově bezpečná; volejte ji z jednoho vlákna (časovače).</para>
/// </remarks>
public sealed class BellScheduler
{
    /// <summary>Kolik posledních zazvoněných minut si plánovač pamatuje (při výchozím rozvrhu ~10 hodin).</summary>
    internal const int HistoryCapacity = 64;

    private readonly BellSchedule _schedule;
    private readonly TimeProvider _timeProvider;
    private readonly Queue<DateTime> _rungOrder = new();
    private readonly HashSet<DateTime> _rung = new();

    public BellScheduler(BellSchedule schedule, TimeProvider timeProvider)
    {
        _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Zjistí, jestli má teď zaznít zvonění. Vrátí jeho druh, nebo <c>null</c>. Kladná odpověď se
    /// pro tutéž minutu místního času vrátí jen jednou.
    /// </summary>
    public BellType? Check()
    {
        var local = _timeProvider.GetLocalNow();
        var bell = _schedule.GetBellForMinute(local.Minute);
        if (bell is null)
        {
            return null;
        }

        // Klíč je místní čas bez posunu vůči UTC – opakovaná hodina při přechodu na zimní čas
        // má tytéž klíče jako její první průchod, takže nezazvoní podruhé.
        var dt = local.DateTime;
        var key = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Unspecified);
        if (!_rung.Add(key))
        {
            return null;
        }

        _rungOrder.Enqueue(key);
        if (_rungOrder.Count > HistoryCapacity)
        {
            _rung.Remove(_rungOrder.Dequeue());
        }

        return bell;
    }
}
