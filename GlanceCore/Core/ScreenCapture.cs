namespace GlanceCore.Core;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

public static class ScreenCapture
{
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, int rop);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    public static BitmapSource CaptureRegion(int x, int y, int width, int height)
    {
        IntPtr hDestPos = GetDesktopWindow();
        IntPtr hSrcDC = GetWindowDC(hDestPos);

        using (Bitmap bmp = new Bitmap(width, height))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hDestDC = g.GetHdc();
                BitBlt(hDestDC, 0, 0, width, height, hSrcDC, x, y, 0x00CC0020); // SRCCOPY
                g.ReleaseHdc(hDestDC);
            }
            ReleaseDC(hDestPos, hSrcDC);

            var bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bitmapSource = BitmapSource.Create(bmp.Width, bmp.Height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, bitmapData.Scan0, bitmapData.Stride * bmp.Height, bitmapData.Stride);
            bmp.UnlockBits(bitmapData);
            return bitmapSource;
        }
    }
}