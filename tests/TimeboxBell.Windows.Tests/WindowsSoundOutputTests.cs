using TimeboxBell.Core;

namespace TimeboxBell.Windows.Tests;

public sealed class WindowsSoundOutputTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("TimeboxBell.Tests.").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Theory]
    [InlineData(BellType.Warning)]
    [InlineData(BellType.PomodoroEnd)]
    [InlineData(BellType.NewHalfHour)]
    public void DefaultSounds_ExistAndPlay(BellType type)
    {
        var path = BellSounds.Default.GetPath(type)!;
        Assert.True(File.Exists(path), $"Chybí výchozí zvuk {path}");

        var output = new WindowsSoundOutput();
        output.Play(path); // soubor se načte a ověří synchronně, výjimka by test shodila
        output.Stop();
    }

    [Fact]
    public void Play_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new WindowsSoundOutput().Play(Path.Combine(_dir, "neni.wav")));
    }

    [Theory]
    [InlineData(new byte[] { 0x74, 0x6F, 0x68, 0x6C, 0x65, 0x20, 0x6E, 0x65, 0x6E, 0x69 })] // text, ne WAV
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0 })] // useknutá hlavička RIFF
    [InlineData(new byte[0])] // prázdný soubor
    public void Play_CorruptFile_Throws(byte[] content)
    {
        var path = Path.Combine(_dir, "poskozeny.wav");
        File.WriteAllBytes(path, content);

        Assert.ThrowsAny<Exception>(() => new WindowsSoundOutput().Play(path));
    }

    [Fact]
    public void BellPlayer_WithCorruptFile_FallsBackToBeep()
    {
        var path = Path.Combine(_dir, "poskozeny.wav");
        File.WriteAllBytes(path, [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0]);
        var player = new BellPlayer(
            new BellSounds(new Dictionary<BellType, string> { [BellType.Warning] = path }), new WindowsSoundOutput());

        Assert.False(player.Play(BellType.Warning));
    }
}
