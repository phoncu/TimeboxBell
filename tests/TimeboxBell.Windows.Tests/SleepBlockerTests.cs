namespace TimeboxBell.Windows.Tests;

/// <summary>
/// Integrační testy proti skutečnému SetThreadExecutionState (bez administrátorských práv).
/// Stav vlákna se čte přes návratovou hodnotu – ta je vždy předchozí stav.
/// </summary>
public class SleepBlockerTests
{
    [Fact]
    public void Acquire_SetsFlags_Dispose_ClearsThem()
    {
        var blocker = SleepBlocker.Acquire();

        // Opětovné nastavení týchž příznaků nic nezmění a vrátí aktuální stav.
        var during = NativeMethods.SetThreadExecutionState(SleepBlocker.BlockingFlags);
        Assert.Equal(SleepBlocker.BlockingFlags, during);

        blocker.Dispose();

        Assert.Equal(ExecutionState.Continuous, CurrentState());
    }

    [Fact]
    public void ExceptionInsideUsing_StillRestoresNormalPower()
    {
        static void FailWhileBlocking()
        {
            using var blocker = SleepBlocker.Acquire();
            throw new InvalidOperationException("simulovaná chyba");
        }

        Assert.Throws<InvalidOperationException>(FailWhileBlocking);

        Assert.Equal(ExecutionState.Continuous, CurrentState());
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var blocker = SleepBlocker.Acquire();
        blocker.Dispose();
        blocker.Dispose();

        Assert.Equal(ExecutionState.Continuous, CurrentState());
    }

    [Fact]
    public void Dispose_FromOtherThread_Throws()
    {
        using var blocker = SleepBlocker.Acquire();

        Exception? error = null;
        var other = new Thread(() => error = Record.Exception(blocker.Dispose));
        other.Start();
        other.Join();

        Assert.IsType<InvalidOperationException>(error);
    }

    /// <summary>Vrátí aktuální stav vlákna; ES_CONTINUOUS samotný odpovídá normálnímu chování napájení.</summary>
    private static ExecutionState CurrentState() =>
        NativeMethods.SetThreadExecutionState(ExecutionState.Continuous);
}
