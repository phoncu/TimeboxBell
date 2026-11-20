namespace TimeboxBell.Core;

/// <summary>Nastavení programu (soubor appsettings.json vedle .exe).</summary>
/// <param name="Schedule">Minuty a druhy zvonění.</param>
/// <param name="Sounds">Zvuky pro jednotlivé druhy zvonění.</param>
/// <param name="PreventSleep">Jestli program brání uspání počítače a zhasnutí obrazovky.</param>
public sealed record TimeboxBellSettings(BellSchedule Schedule, BellSounds Sounds, bool PreventSleep)
{
    /// <summary>Výchozí nastavení, když soubor nebo klíč chybí.</summary>
    public static TimeboxBellSettings Default { get; } = new(BellSchedule.Default, BellSounds.Default, PreventSleep: true);
}
