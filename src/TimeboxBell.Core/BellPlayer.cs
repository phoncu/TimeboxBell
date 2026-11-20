namespace TimeboxBell.Core;

/// <summary>Informace o zvonění, jehož zvuk se nepodařilo přehrát.</summary>
/// <param name="Type">Druh zvonění.</param>
/// <param name="Path">Cesta ke zvuku, nebo <c>null</c>, pokud pro druh žádný zvuk není nastavený.</param>
/// <param name="Error">Výjimka z přehrávání, nebo <c>null</c>, pokud zvuk není nastavený.</param>
public sealed record PlaybackFailure(BellType Type, string? Path, Exception? Error);

/// <summary>
/// Přehraje zvuk pro daný druh zvonění. Když zvuk chybí nebo nejde přehrát, pípne – zvonění tak
/// vždy nějak zazní a program kvůli zvuku nespadne.
/// </summary>
public sealed class BellPlayer
{
    private readonly BellSounds _sounds;
    private readonly ISoundOutput _output;

    public BellPlayer(BellSounds sounds, ISoundOutput output)
    {
        _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>Vyvolá se, když zvuk nešel přehrát a místo něj se pípalo.</summary>
    public event Action<PlaybackFailure>? PlaybackFailed;

    /// <summary>Přehraje zvuk zvonění. Vrátí <c>true</c>, pokud zazněl zvuk, <c>false</c>, pokud se jen pípalo.</summary>
    public bool Play(BellType type)
    {
        var path = _sounds.GetPath(type);
        Exception? error = null;
        if (path is not null)
        {
            try
            {
                _output.Play(path);
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        try
        {
            _output.Beep();
        }
        catch (Exception)
        {
            // Ani pípnutí nejde (např. bez zvukového zařízení) – zvonění nesmí shodit program.
        }

        try
        {
            PlaybackFailed?.Invoke(new PlaybackFailure(type, path, error));
        }
        catch (Exception)
        {
            // Chyba v obsluze hlášení nesmí shodit program.
        }

        return false;
    }
}
