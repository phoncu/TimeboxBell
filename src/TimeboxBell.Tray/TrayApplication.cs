using TimeboxBell.Core;
using Timer = System.Windows.Forms.Timer;

namespace TimeboxBell.Tray;

/// <summary>Ikona v oznamovací oblasti s menu: další zvonění, pauza/pokračovat, ukončení.</summary>
internal sealed class TrayApplication : ApplicationContext
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    private readonly BellSchedule _schedule;
    private readonly TimeProvider _timeProvider;
    private readonly BellRunner _runner;
    private readonly bool _preventingSleep;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Timer _timer;
    private readonly Icon _runningIcon = TrayIcons.Create(paused: false);
    private readonly Icon _pausedIcon = TrayIcons.Create(paused: true);

    public TrayApplication(TimeboxBellSettings settings, TimeProvider timeProvider, ISoundOutput output, bool preventingSleep)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _schedule = settings.Schedule;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _preventingSleep = preventingSleep;

        var player = new BellPlayer(settings.Sounds, output);
        player.PlaybackFailed += OnPlaybackFailed;
        _runner = new BellRunner(new BellScheduler(_schedule, timeProvider), player);

        NextBellItem = new ToolStripMenuItem { Enabled = false };
        PauseItem = new ToolStripMenuItem(string.Empty, null, (_, _) => TogglePause());
        ExitItem = new ToolStripMenuItem("Ukončit", null, (_, _) => ExitThread());
        _menu = new ContextMenuStrip();
        _menu.Items.AddRange([NextBellItem, PauseItem, new ToolStripSeparator(), ExitItem]);
        _menu.Opening += (_, _) => UpdateTexts();

        _notifyIcon = new NotifyIcon { ContextMenuStrip = _menu, Icon = _runningIcon };

        _timer = new Timer { Interval = (int)CheckInterval.TotalMilliseconds };
        _timer.Tick += (_, _) => Tick();

        UpdateTexts();
    }

    internal ToolStripMenuItem NextBellItem { get; }

    internal ToolStripMenuItem PauseItem { get; }

    internal ToolStripMenuItem ExitItem { get; }

    internal bool IsPaused => _runner.IsPaused;

    internal string ToolTipText => _notifyIcon.Text;

    internal bool IsRunning => _timer.Enabled && _notifyIcon.Visible;

    /// <summary>Zobrazí ikonu a spustí pravidelnou kontrolu rozvrhu.</summary>
    public void Start()
    {
        _notifyIcon.Visible = true;
        _timer.Start();
    }

    /// <summary>Jedna kontrola rozvrhu (volá ji časovač; testy přímo).</summary>
    internal BellType? Tick()
    {
        var bell = _runner.Tick();
        UpdateTexts();
        return bell;
    }

    internal void ShowWarning(string message) =>
        _notifyIcon.ShowBalloonTip(10_000, "TimeboxBell", message, ToolTipIcon.Warning);

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _notifyIcon.Visible = false;
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
            _runningIcon.Dispose();
            _pausedIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private void TogglePause()
    {
        _runner.IsPaused = !_runner.IsPaused;
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        var next = _schedule.GetNext(_timeProvider.GetLocalNow().DateTime);
        var nextText = next is null ? "Rozvrh je prázdný" : $"Další zvonění: {next.At:HH:mm} {Describe(next.Type)}";

        NextBellItem.Text = _runner.IsPaused ? $"Pozastaveno ({nextText.ToLowerInvariant()})" : nextText;
        PauseItem.Text = _runner.IsPaused ? "Pokračovat" : "Pozastavit";
        _notifyIcon.Icon = _runner.IsPaused ? _pausedIcon : _runningIcon;

        var status = _runner.IsPaused ? "pozastaveno" : _preventingSleep ? "běží, PC se neuspí" : "běží";
        var tip = $"TimeboxBell – {status}\n{nextText}";
        _notifyIcon.Text = tip.Length <= 127 ? tip : tip[..127]; // limit NotifyIcon.Text
    }

    private void OnPlaybackFailed(PlaybackFailure failure) =>
        ShowWarning(failure.Path is null
            ? $"Pro {Describe(failure.Type)} není nastavený zvuk – pípnuto."
            : $"Zvuk {failure.Path} nešel přehrát – pípnuto.\n{failure.Error?.Message}");

    internal static string Describe(BellType type) => type switch
    {
        BellType.Warning => "varování",
        BellType.PomodoroEnd => "konec pomodora",
        BellType.NewHalfHour => "nová půlhodina",
        _ => type.ToString(),
    };
}
