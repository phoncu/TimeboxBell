using System.Security.Principal;

namespace TimeboxBell.Windows.Tests;

public class SingleInstanceTests
{
    private static string UniqueName() => $@"Local\TimeboxBell.Tests.{Guid.NewGuid():N}";

    [Fact]
    public void SecondAcquire_WhileFirstHeld_ReturnsNull()
    {
        var name = UniqueName();
        using var first = SingleInstance.TryAcquire(name);

        Assert.NotNull(first);
        Assert.Null(SingleInstance.TryAcquire(name));
    }

    [Fact]
    public void AfterDispose_CanAcquireAgain()
    {
        var name = UniqueName();
        SingleInstance.TryAcquire(name)!.Dispose();

        using var again = SingleInstance.TryAcquire(name);

        Assert.NotNull(again);
    }

    [Fact]
    public void SecondAcquire_FromOtherThread_ReturnsNull()
    {
        var name = UniqueName();
        using var first = SingleInstance.TryAcquire(name);

        SingleInstance? fromOther = null;
        var other = new Thread(() => fromOther = SingleInstance.TryAcquire(name));
        other.Start();
        other.Join();

        Assert.Null(fromOther);
    }

    [Fact]
    public void Dispose_FromOtherThread_ReleasesInstance()
    {
        var name = UniqueName();
        var first = SingleInstance.TryAcquire(name)!;

        var other = new Thread(first.Dispose);
        other.Start();
        other.Join();

        using var again = SingleInstance.TryAcquire(name);
        Assert.NotNull(again);
    }

    [Fact]
    public void DifferentNames_AreIndependent()
    {
        using var a = SingleInstance.TryAcquire(UniqueName());
        using var b = SingleInstance.TryAcquire(UniqueName());

        Assert.NotNull(a);
        Assert.NotNull(b);
    }

    [Fact]
    public void DefaultName_IsPerUserAndSessionLocal()
    {
        var sid = WindowsIdentity.GetCurrent().User!.Value;

        Assert.StartsWith(@"Local\", SingleInstance.DefaultName);
        Assert.EndsWith(sid, SingleInstance.DefaultName);
    }
}
