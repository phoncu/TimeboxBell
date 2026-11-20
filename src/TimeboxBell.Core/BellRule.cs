namespace TimeboxBell.Core;

/// <summary>Pravidlo rozvrhu: v minutě <paramref name="Minute"/> každé hodiny zazní zvonění <paramref name="Type"/>.</summary>
/// <param name="Minute">Minuta v hodině, 0–59, podle místního času.</param>
/// <param name="Type">Druh zvonění.</param>
public sealed record BellRule(int Minute, BellType Type);
