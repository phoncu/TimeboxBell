namespace TimeboxBell.Core;

/// <summary>Zvukový výstup. Skutečná implementace je v TimeboxBell.Windows, v testech se nahrazuje falešnou.</summary>
public interface ISoundOutput
{
    /// <summary>Spustí přehrávání .wav souboru. Pokud soubor chybí nebo nejde přehrát, vyhodí výjimku.</summary>
    void Play(string path);

    /// <summary>Záložní pípnutí, když zvuk nejde přehrát.</summary>
    void Beep();
}
