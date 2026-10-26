using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;

namespace NetPulse;

/// <summary>
/// Renders a small directional rate icon. Windows may scale tray icons to
/// 16x16, so the value is intentionally abbreviated and the tooltip provides
/// both complete readings.
/// </summary>
public static class TrayIconRenderer
{
    private const int IconSize = 32;

    public static Icon CreateIcon(double bytesPerSecond, bool isDownload)
    {
        using var bitmap = new Bitmap(IconSize, IconSize);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(Color.Transparent);

            using var path = CreateRoundedRectangle(new Rectangle(1, 1, 30, 30), 6);
            using var background = new SolidBrush(Color.FromArgb(248, 29, 35, 44));
            graphics.FillPath(background, path);

            var accent = isDownload
                ? Color.FromArgb(100, 181, 246)
                : Color.FromArgb(112, 214, 160);

            using var arrowFont = new Font("Segoe UI Symbol", 10f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var rateFont = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var arrowBrush = new SolidBrush(accent);
            using var rateBrush = new SolidBrush(Color.White);

            var arrow = isDownload ? "↓" : "↑";
            var rate = RateFormatter.FormatIconRate(bytesPerSecond);
            var arrowSize = graphics.MeasureString(arrow, arrowFont);
            graphics.DrawString(arrow, arrowFont, arrowBrush,
                (IconSize - arrowSize.Width) / 2f, 1f);

            var rateSize = graphics.MeasureString(rate, rateFont);
            graphics.DrawString(rate, rateFont, rateBrush,
                Math.Max(1f, (IconSize - rateSize.Width) / 2f), 16f);
        }

        var nativeHandle = bitmap.GetHicon();
        try
        {
            // Clone before releasing the temporary HICON; NotifyIcon owns the clone.
            return (Icon)Icon.FromHandle(nativeHandle).Clone();
        }
        finally
        {
            _ = DestroyIcon(nativeHandle);
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
