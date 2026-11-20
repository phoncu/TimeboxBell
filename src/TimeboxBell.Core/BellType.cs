namespace TimeboxBell.Core;

/// <summary>Druh zvonění. Každý druh bude mít později vlastní zvuk.</summary>
public enum BellType
{
    /// <summary>Varování, že se blíží konec slotu (výchozí :23 a :53).</summary>
    Warning,

    /// <summary>Konec pomodora, začíná pauza (výchozí :25 a :55).</summary>
    PomodoroEnd,

    /// <summary>Začátek nové půlhodiny (výchozí :00 a :30).</summary>
    NewHalfHour,
}
