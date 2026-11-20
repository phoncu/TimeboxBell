using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TimeboxBell.Windows;

/// <summary>
/// Dokud žije, brání uspání počítače a zhasnutí obrazovky (SetThreadExecutionState s ES_CONTINUOUS).
/// Nevyžaduje administrátorská práva.
/// </summary>
/// <remarks>
/// <para>Požadavek patří vláknu, které ho vytvořilo, a musí se uvolnit ve stejném vlákně – proto
/// <see cref="Acquire"/> i <see cref="Dispose"/> volejte z jednoho dlouho žijícího vlákna (hlavní
/// vlákno konzole, vlákno UI). Když vlákno nebo proces skončí (i pádem), Windows požadavek zruší
/// samy, takže napájení se vždy vrátí do normálu.</para>
/// <para>Podle dokumentace Microsoftu požadavek nezastaví spořič obrazovky ani zamčení obrazovky
/// nastavené politikou – ty se neobcházejí.</para>
/// </remarks>
public sealed class SleepBlocker : IDisposable
{
    internal const ExecutionState BlockingFlags =
        ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired;

    private readonly int _threadId;
    private bool _disposed;

    private SleepBlocker(int threadId) => _threadId = threadId;

    /// <summary>Začne bránit uspání a zhasnutí obrazovky pro aktuální vlákno.</summary>
    /// <exception cref="InvalidOperationException">Windows požadavek odmítly.</exception>
    public static SleepBlocker Acquire()
    {
        if (NativeMethods.SetThreadExecutionState(BlockingFlags) == 0)
        {
            throw new InvalidOperationException(
                "Nepodařilo se zablokovat spánek (SetThreadExecutionState).",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        return new SleepBlocker(Environment.CurrentManagedThreadId);
    }

    /// <summary>Obnoví normální chování napájení. Opakované volání nic nedělá.</summary>
    /// <exception cref="InvalidOperationException">Volání z jiného vlákna, než které požadavek vytvořilo.</exception>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Environment.CurrentManagedThreadId != _threadId)
        {
            throw new InvalidOperationException("SleepBlocker se musí uvolnit ve vlákně, které ho vytvořilo.");
        }

        NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);
        _disposed = true;
    }
}

[Flags]
internal enum ExecutionState : uint
{
    SystemRequired = 0x00000001,
    DisplayRequired = 0x00000002,
    Continuous = 0x80000000,
}

internal static class NativeMethods
{
    /// <summary>Nastaví stav vlákna a vrátí předchozí stav; 0 znamená chybu.</summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern ExecutionState SetThreadExecutionState(ExecutionState flags);
}
