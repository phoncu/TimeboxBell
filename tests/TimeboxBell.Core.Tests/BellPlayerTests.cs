namespace TimeboxBell.Core.Tests;

public class BellPlayerTests
{
    private static readonly BellSounds Sounds = new(new Dictionary<BellType, string>
    {
        [BellType.Warning] = "warning.wav",
        [BellType.PomodoroEnd] = "end.wav",
        [BellType.NewHalfHour] = "half.wav",
    });

    [Theory]
    [InlineData(BellType.Warning, "warning.wav")]
    [InlineData(BellType.PomodoroEnd, "end.wav")]
    [InlineData(BellType.NewHalfHour, "half.wav")]
    public void Play_PlaysFileForBellType(BellType type, string expectedPath)
    {
        var output = new FakeSoundOutput();
        var player = new BellPlayer(Sounds, output);

        Assert.True(player.Play(type));

        Assert.Equal([expectedPath], output.Played);
        Assert.Equal(0, output.Beeps);
    }

    public static TheoryData<Exception> PlaybackErrors => new()
    {
        new FileNotFoundException("chybí"),
        new InvalidOperationException("poškozená hlavička WAV"),
        new IOException("nelze přehrát"),
        new UnauthorizedAccessException("přístup odepřen"),
    };

    [Theory]
    [MemberData(nameof(PlaybackErrors))]
    public void Play_WhenFileFails_BeepsAndReportsFailure(Exception error)
    {
        var output = new FakeSoundOutput { PlayError = error };
        var player = new BellPlayer(Sounds, output);
        var failures = new List<PlaybackFailure>();
        player.PlaybackFailed += failures.Add;

        Assert.False(player.Play(BellType.PomodoroEnd));

        Assert.Equal(1, output.Beeps);
        Assert.Equal([new PlaybackFailure(BellType.PomodoroEnd, "end.wav", error)], failures);
    }

    [Fact]
    public void Play_WhenNoSoundConfigured_Beeps()
    {
        var output = new FakeSoundOutput();
        var player = new BellPlayer(new BellSounds(new Dictionary<BellType, string> { [BellType.Warning] = " " }), output);

        Assert.False(player.Play(BellType.Warning));
        Assert.False(player.Play(BellType.NewHalfHour));

        Assert.Empty(output.Played);
        Assert.Equal(2, output.Beeps);
    }

    [Fact]
    public void Play_WhenBeepAndHandlerAlsoFail_DoesNotThrow()
    {
        var output = new FakeSoundOutput { PlayError = new IOException(), BeepError = new InvalidOperationException() };
        var player = new BellPlayer(Sounds, output);
        player.PlaybackFailed += _ => throw new InvalidOperationException("chyba v obsluze");

        Assert.False(player.Play(BellType.Warning));
    }

    [Fact]
    public void DefaultSounds_AreDistinctWindowsMediaFiles()
    {
        var paths = Enum.GetValues<BellType>().Select(BellSounds.Default.GetPath).ToList();

        Assert.All(paths, p => Assert.StartsWith(@"C:\Windows\Media\", p));
        Assert.Equal(paths.Count, paths.Distinct().Count());
    }

    private sealed class FakeSoundOutput : ISoundOutput
    {
        public List<string> Played { get; } = [];

        public int Beeps { get; private set; }

        public Exception? PlayError { get; init; }

        public Exception? BeepError { get; init; }

        public void Play(string path)
        {
            if (PlayError is not null)
            {
                throw PlayError;
            }

            Played.Add(path);
        }

        public void Beep()
        {
            Beeps++;
            if (BeepError is not null)
            {
                throw BeepError;
            }
        }
    }
}
