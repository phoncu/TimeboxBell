using System.Text;
using TimeboxBell.Core;
using TimeboxBell.Windows;

namespace TimeboxBell.ConsoleApp;

internal static class Program
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var schedule = BellSchedule.Default;
        var sounds = BellSounds.Default;

        switch (args)
        {
            case []:
                return Run(schedule, sounds);
            case ["--list-schedule"]:
                ListSchedule(schedule, sounds);
                return 0;
            case ["--help" or "-h" or "/?"]:
                PrintUsage(Console.Out);
                return 0;
            default:
                Console.Error.WriteLine($"Neznámý argument: {string.Join(' ', args)}");
                PrintUsage(Console.Error);
                return 2;
        }
    }

    private static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("Použití: TimeboxBell.Console [--list-schedule | --help]");
        writer.WriteLine("  bez argumentů      zvoní podle rozvrhu a brání uspání počítače; Ctrl+C ukončí");
        writer.WriteLine("  --list-schedule    vypíše rozvrh zvonění a skončí");
    }

    private static void ListSchedule(BellSchedule schedule, BellSounds sounds)
    {
        Console.WriteLine("Rozvrh zvonění (opakuje se každou hodinu):");
        foreach (var rule in schedule.Rules)
        {
            Console.WriteLine(
                $"  :{rule.Minute:00}  {rule.Type,-12} {Describe(rule.Type),-16} {sounds.GetPath(rule.Type) ?? "(pípnutí)"}");
        }
    }

    private static int Run(BellSchedule schedule, BellSounds sounds)
    {
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // ukončíme se sami, aby proběhlo uvolnění blokování spánku
            stop.Cancel();
        };

        var scheduler = new BellScheduler(schedule, TimeProvider.System);
        var player = new BellPlayer(sounds, new WindowsSoundOutput());
        player.PlaybackFailed += f => Console.WriteLine(f.Path is null
            ? $"  ! Pro {f.Type} není nastavený zvuk – pípnuto."
            : $"  ! Zvuk {f.Path} nešel přehrát ({f.Error?.Message}) – pípnuto.");

        // Blokování patří vláknu – celá smyčka proto běží synchronně v hlavním vlákně.
        SleepBlocker? blocker = null;
        try
        {
            try
            {
                blocker = SleepBlocker.Acquire();
                Console.WriteLine("TimeboxBell běží, počítač se neuspí a obrazovka nezhasne. Ctrl+C ukončí.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"! {ex.Message} Zvonit se bude, ale počítač se může uspat.");
            }

            PrintNext(schedule, DateTimeOffset.Now);
            while (!stop.IsCancellationRequested)
            {
                if (scheduler.Check() is { } bell)
                {
                    var now = DateTimeOffset.Now;
                    Console.WriteLine($"{now:HH:mm:ss}  {Describe(bell)}");
                    player.Play(bell);
                    PrintNext(schedule, now);
                }

                stop.Token.WaitHandle.WaitOne(CheckInterval);
            }
        }
        finally
        {
            blocker?.Dispose();
            Console.WriteLine("Ukončeno, normální chování napájení obnoveno.");
        }

        return 0;
    }

    private static void PrintNext(BellSchedule schedule, DateTimeOffset now)
    {
        var minuteStart = now.AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
        for (var i = 1; i <= 60; i++)
        {
            var at = minuteStart.AddMinutes(i);
            if (schedule.GetBellForMinute(at.Minute) is { } type)
            {
                Console.WriteLine($"          další: {at:HH:mm} {Describe(type)}");
                return;
            }
        }
    }

    private static string Describe(BellType type) => type switch
    {
        BellType.Warning => "varování",
        BellType.PomodoroEnd => "konec pomodora",
        BellType.NewHalfHour => "nová půlhodina",
        _ => type.ToString(),
    };
}
