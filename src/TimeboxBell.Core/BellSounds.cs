namespace TimeboxBell.Core;

/// <summary>Přiřazení zvukových souborů (.wav) k druhům zvonění.</summary>
public sealed class BellSounds
{
    private readonly Dictionary<BellType, string> _paths;

    public BellSounds(IReadOnlyDictionary<BellType, string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = new Dictionary<BellType, string>(paths);
    }

    /// <summary>Výchozí zvuky Windows: varování, konec pomodora a nová půlhodina mají každý jiný tón.</summary>
    public static BellSounds Default { get; } = new(new Dictionary<BellType, string>
    {
        [BellType.Warning] = @"C:\Windows\Media\Windows Notify Calendar.wav",
        [BellType.PomodoroEnd] = @"C:\Windows\Media\Alarm01.wav",
        [BellType.NewHalfHour] = @"C:\Windows\Media\Alarm05.wav",
    });

    /// <summary>Cesta ke zvuku pro daný druh zvonění, nebo <c>null</c>, pokud žádný není nastavený.</summary>
    public string? GetPath(BellType type) =>
        _paths.TryGetValue(type, out var path) && !string.IsNullOrWhiteSpace(path) ? path : null;
}
