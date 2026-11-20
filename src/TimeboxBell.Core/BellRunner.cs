namespace TimeboxBell.Core;

/// <summary>
/// Spojuje plánovač a přehrávač a umí zvonění pozastavit. Volající volá <see cref="Tick"/> pravidelně
/// (např. každých 5 s) z jednoho vlákna.
/// </summary>
public sealed class BellRunner
{
    private readonly BellScheduler _scheduler;
    private readonly BellPlayer _player;

    public BellRunner(BellScheduler scheduler, BellPlayer player)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _player = player ?? throw new ArgumentNullException(nameof(player));
    }

    /// <summary>Při pauze nezazní žádné zvonění. Po pokračování se zameškaná zvonění nepřehrají.</summary>
    public bool IsPaused { get; set; }

    /// <summary>Zkontroluje rozvrh a případně zazvoní. Vrátí druh zvonění, které zaznělo, jinak <c>null</c>.</summary>
    public BellType? Tick()
    {
        // Plánovač se ptá i během pauzy: zvonění, jehož minuta v pauze nastala, se tím označí jako
        // vyřízené a po pokračování ve stejné minutě už nezazní.
        var bell = _scheduler.Check();
        if (bell is null || IsPaused)
        {
            return null;
        }

        _player.Play(bell.Value);
        return bell;
    }
}
