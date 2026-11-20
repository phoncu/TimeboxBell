using System.Runtime.InteropServices;

namespace TimeboxBell.Windows.Tests;

public class AppIconTests
{
    private static readonly int[] RequiredSizes = [16, 24, 32, 48, 256];

    [Fact]
    public void IconFile_ContainsSizesFor16To256Px()
    {
        var sizes = ReadIconDirectory(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TimeboxBell.ico")), entrySize: 16);

        Assert.All(RequiredSizes, size => Assert.Contains(size, sizes));
    }

    [Theory]
    [InlineData("TimeboxBell.exe")]
    [InlineData("TimeboxBell.Console.exe")]
    public void Exe_HasEmbeddedAppIcon_WithAllSizes(string exeName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, exeName);
        Assert.True(File.Exists(path), $"Chybí {path}");

        var sizes = ReadIconDirectory(ReadFirstGroupIcon(path), entrySize: 14);

        Assert.All(RequiredSizes, size => Assert.Contains(size, sizes));
    }

    /// <summary>Velikosti z hlavičky ICO (soubor, položka 16 B) nebo RT_GROUP_ICON v .exe (položka 14 B).</summary>
    private static List<int> ReadIconDirectory(byte[] data, int entrySize)
    {
        Assert.Equal(1, BitConverter.ToUInt16(data, 2)); // typ: ikona
        var count = BitConverter.ToUInt16(data, 4);
        return Enumerable.Range(0, count).Select(i => data[6 + i * entrySize] is 0 ? 256 : (int)data[6 + i * entrySize]).ToList();
    }

    private static byte[] ReadFirstGroupIcon(string exePath)
    {
        const uint loadLibraryAsDatafile = 0x2;
        var module = LoadLibraryEx(exePath, IntPtr.Zero, loadLibraryAsDatafile);
        Assert.NotEqual(IntPtr.Zero, module);
        try
        {
            // Název prostředku hledáme uvnitř zpětného volání – textový název platí jen během výčtu.
            var resource = IntPtr.Zero;
            EnumResourceNames(module, RtGroupIcon, (m, t, n, _) => { resource = FindResource(m, n, t); return false; }, IntPtr.Zero);
            Assert.True(resource != IntPtr.Zero, $"{Path.GetFileName(exePath)} nemá vloženou ikonu.");

            var size = SizeofResource(module, resource);
            var pointer = LockResource(LoadResource(module, resource));
            var bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, (int)size);
            return bytes;
        }
        finally
        {
            FreeLibrary(module);
        }
    }

    private static readonly IntPtr RtGroupIcon = 14;

    private delegate bool EnumResNameProc(IntPtr module, IntPtr type, IntPtr name, IntPtr param);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr module);

    [DllImport("kernel32.dll")]
    private static extern bool EnumResourceNames(IntPtr module, IntPtr type, EnumResNameProc callback, IntPtr param);

    [DllImport("kernel32.dll")]
    private static extern IntPtr FindResource(IntPtr module, IntPtr name, IntPtr type);

    [DllImport("kernel32.dll")]
    private static extern uint SizeofResource(IntPtr module, IntPtr resource);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LockResource(IntPtr data);
}
