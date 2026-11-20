using System.Security.Principal;

namespace TimeboxBell.Windows;

/// <summary>
/// Hlídá, aby u přihlášeného uživatele zvonila jen jedna instance TimeboxBell – sdílí ji aplikace
/// v oznamovací oblasti i konzolová aplikace.
/// </summary>
/// <remarks>
/// <para>Používá pojmenovaný mutex, jehož jméno obsahuje SID uživatele, takže jiný uživatel na stejném
/// PC si může pustit vlastní instanci. Nevyžaduje administrátorská práva.</para>
/// <para>Mutex se nevlastní, jen se drží jeho handle – nezáleží tedy na vlákně. Když proces skončí
/// (i pádem nebo zabitím), Windows handle zavřou, objekt zanikne a program jde spustit znovu.</para>
/// </remarks>
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>Výchozí jméno mutexu pro aktuálního uživatele.</summary>
    public static string DefaultName { get; } =
        $@"Local\TimeboxBell.SingleInstance.{WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName}";

    /// <summary>
    /// Pokusí se stát jedinou instancí. Vrátí držák, který je potřeba nechat žít po celou dobu běhu,
    /// nebo <see langword="null"/>, když už jiná instance běží.
    /// </summary>
    /// <param name="name">Jméno mutexu; výchozí je <see cref="DefaultName"/> (jiné jméno jen pro testy).</param>
    public static SingleInstance? TryAcquire(string? name = null)
    {
        var mutex = new Mutex(initiallyOwned: false, name ?? DefaultName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstance(mutex);
        }

        mutex.Dispose();
        return null;
    }

    /// <summary>Uvolní místo pro další instanci.</summary>
    public void Dispose() => _mutex.Dispose();
}
