using TimeboxBell.Core;
using TimeboxBell.Windows;

namespace TimeboxBell.Tray;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();

        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            MessageBox.Show(
                "TimeboxBell už běží (ikona v oznamovací oblasti nebo konzolová aplikace).\n\nDruhá instance by zvonila podruhé, proto se nespustí.",
                "TimeboxBell",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 3;
        }

        TimeboxBellSettings settings;
        try
        {
            settings = SettingsLoader.Load(SettingsLoader.DefaultPath);
        }
        catch (SettingsException ex)
        {
            MessageBox.Show(
                $"{ex.Message}\n\nOpravte soubor a spusťte TimeboxBell znovu.",
                "TimeboxBell – chyba v nastavení",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }

        // Blokování spánku patří vláknu – získává i uvolňuje se ve vlákně UI, které běží celou dobu.
        SleepBlocker? blocker = null;
        string? sleepWarning = null;
        if (settings.PreventSleep)
        {
            try
            {
                blocker = SleepBlocker.Acquire();
            }
            catch (InvalidOperationException ex)
            {
                sleepWarning = $"{ex.Message} Zvonit se bude, ale počítač se může uspat.";
            }
        }

        try
        {
            using var app = new TrayApplication(settings, TimeProvider.System, new WindowsSoundOutput(), blocker is not null);
            app.Start();
            if (sleepWarning is not null)
            {
                app.ShowWarning(sleepWarning);
            }

            Application.Run(app);
        }
        finally
        {
            blocker?.Dispose();
        }

        return 0;
    }
}
