using System.Media;
using TimeboxBell.Core;

namespace TimeboxBell.Windows;

/// <summary>
/// Přehrává .wav přes <see cref="SoundPlayer"/> (winmm PlaySound). Zvuk jde do výchozího výstupního
/// zařízení (např. sluchátek) s hlasitostí podle mixéru Windows.
/// </summary>
public sealed class WindowsSoundOutput : ISoundOutput
{
    private const int BeepFrequencyHz = 880;
    private const int BeepDurationMs = 300;

    private readonly object _gate = new();
    private SoundPlayer? _current;

    /// <inheritdoc />
    /// <remarks>
    /// Soubor se načte a ověří synchronně (chybějící soubor → <see cref="FileNotFoundException"/>,
    /// poškozený soubor → výjimka ze <see cref="SoundPlayer"/>), přehrávání pak běží na pozadí.
    /// Nové zvonění přeruší předchozí, pokud ještě hraje.
    /// </remarks>
    public void Play(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Zvukový soubor neexistuje: {path}", path);
        }

        var player = new SoundPlayer(path);
        try
        {
            player.Load();
            player.Play();
        }
        catch
        {
            player.Dispose();
            throw;
        }

        lock (_gate)
        {
            // Instance má žít, dokud zvuk hraje; drží se do dalšího zvonění.
            var previous = _current;
            _current = player;
            previous?.Dispose();
        }
    }

    /// <summary>Zastaví právě hrající zvuk (používají testy, aby nerušily okolí).</summary>
    internal void Stop()
    {
        lock (_gate)
        {
            _current?.Stop();
        }
    }

    /// <inheritdoc />
    public void Beep() => Console.Beep(BeepFrequencyHz, BeepDurationMs);
}
