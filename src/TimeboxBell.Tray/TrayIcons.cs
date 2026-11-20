using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TimeboxBell.Tray;

/// <summary>Ikony kreslené za běhu: ciferník – zelený, když zvonění běží, šedý při pauze.</summary>
internal static class TrayIcons
{
    public static Icon Create(bool paused)
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var face = new SolidBrush(paused ? Color.FromArgb(128, 128, 128) : Color.FromArgb(46, 125, 50));
            g.FillEllipse(face, 1, 1, size - 3, size - 3);

            using var hand = new Pen(Color.White, 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            var c = (size - 1) / 2f;
            if (paused)
            {
                // Symbol pauzy.
                g.DrawLine(hand, c - 4, c - 7, c - 4, c + 7);
                g.DrawLine(hand, c + 4, c - 7, c + 4, c + 7);
            }
            else
            {
                // Ručičky na :25 – konec pomodora.
                g.DrawLine(hand, c, c, c, c - 10);
                g.DrawLine(hand, c, c, c + 4, c + 8);
            }
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone(); // vlastní kopie, aby šel nativní handle hned uvolnit
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
