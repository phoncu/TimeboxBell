namespace TimeboxBell.Core;

/// <summary>Nejbližší plánované zvonění.</summary>
/// <param name="At">Místní čas (nástěnné hodiny) začátku minuty, ve které zazní.</param>
/// <param name="Type">Druh zvonění.</param>
public sealed record UpcomingBell(DateTime At, BellType Type);
