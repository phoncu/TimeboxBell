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

        switch (args)
        {
            case [] or ["--list-schedule"]:
                break;
            case ["--help" or "-h" or "/?"]:
                PrintUsage(Console.Out);
                return 0;
            default:
                Console.Error.WriteLine($"Neznámý argument: {string.Join(' ', args)}");
                PrintUsage(Console.Error);
                return 2;
        }

        TimeboxBellSettings settings;
        try
        {
            settings = SettingsLoader.Load(SettingsLoader.DefaultPath);
        }
        catch (SettingsException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (args is ["--list-schedule"])
        {
            ListSchedule(settings);
            return 0;
        }

        return Run(settings);
    }

    private static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("Použití: TimeboxBell.Console [--list-schedule | --help]");
        writer.WriteLine("  bez argumentů      zvoní podle rozvrhu a brání uspání počítače; Ctrl+C ukončí");
        writer.WriteLine("  --list-schedule    vypíše rozvrh zvonění a skončí");
    }

    private static void ListSchedule(TimeboxBellSettings settings)
    {
        var source = File.Exists(SettingsLoader.DefaultPath) ? SettingsLoader.DefaultPath : "výchozí (soubor nenalezen)";
        Console.WriteLine($"Nastavení: {source}");
        Console.WriteLine("Rozvrh zvonění (opakuje se každou hodinu):");
        foreach (var rule in settings.Schedule.Rules)
        {
            Console.WriteLine(
                $"  :{rule.Minute:00}  {rule.Type,-12} {Describe(rule.Type),-16} {settings.Sounds.GetPath(rule.Type) ?? "(pípnutí)"}");
        }

        Console.WriteLine($"Blokování spánku: {(settings.PreventSleep ? "zapnuto" : "vypnuto")}");
    }

    private static int Run(TimeboxBellSettings settings)
    {
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // ukončíme se sami, aby proběhlo uvolnění blokování spánku
            stop.Cancel();
        };

        var schedule = settings.Schedule;
        var player = new BellPlayer(settings.Sounds, new WindowsSoundOutput());
        player.PlaybackFailed += f => Console.WriteLine(f.Path is null
            ? $"  ! Pro {f.Type} není nastavený zvuk – pípnuto."
            : $"  ! Zvuk {f.Path} nešel přehrát ({f.Error?.Message}) – pípnuto.");
        var runner = new BellRunner(new BellScheduler(schedule, TimeProvider.System), player);

        // Blokování patří vláknu – celá smyčka proto běží synchronně v hlavním vlákně.
        SleepBlocker? blocker = null;
        try
        {
            if (!settings.PreventSleep)
            {
                Console.WriteLine("TimeboxBell běží (blokování spánku je v nastavení vypnuté). Ctrl+C ukončí.");
            }
            else
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
            }

            PrintNext(schedule, DateTimeOffset.Now);
            while (!stop.IsCancellationRequested)
            {
                if (runner.Tick() is { } bell)
                {
                    var now = DateTimeOffset.Now;
                    Console.WriteLine($"{now:HH:mm:ss}  {Describe(bell)}");
                    PrintNext(schedule, now);
                }

                stop.Token.WaitHandle.WaitOne(CheckInterval);
            }
        }
        finally
        {
            if (blocker is not null)
            {
                blocker.Dispose();
                Console.WriteLine("Ukončeno, normální chování napájení obnoveno.");
            }
            else
            {
                Console.WriteLine("Ukončeno.");
            }
        }

        return 0;
    }

    private static void PrintNext(BellSchedule schedule, DateTimeOffset now)
    {
        if (schedule.GetNext(now.DateTime) is { } next)
        {
            Console.WriteLine($"          další: {next.At:HH:mm} {Describe(next.Type)}");
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
