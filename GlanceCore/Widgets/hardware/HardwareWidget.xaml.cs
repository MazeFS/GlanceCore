using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using GlanceCore.Widgets;
using GlanceCore.UI.Shaders;

namespace GlanceCore.Widgets.Hardware;

public partial class HardwareWidget : BaseWidgetWindow, INotifyPropertyChanged
{
    // --- WIN32 API DECLARATIONS ---

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, int rop);

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    // --- FIELDS ---

    private readonly Computer _computer;
    private readonly DispatcherTimer _statsTimer;
    private readonly DispatcherTimer _captureTimer;

    // --- BINDINGS ---

    private string _cpuText = "0%"; public string CpuText { get => _cpuText; set { _cpuText = value; OnPropertyChanged(); } }
    private double _cpuValue = 0; public double CpuValue { get => _cpuValue; set { _cpuValue = value; OnPropertyChanged(); } }
    private string _gpuText = "0%"; public string GpuText { get => _gpuText; set { _gpuText = value; OnPropertyChanged(); } }
    private double _gpuValue = 0; public double GpuValue { get => _gpuValue; set { _gpuValue = value; OnPropertyChanged(); } }

    public HardwareWidget()
    {
        InitializeComponent();

        _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
        try { _computer.Open(); } catch { /* Admin rights handled by manifest */ }

        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();

        // English: Hardware polling timer
        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _statsTimer.Tick += async (s, e) => await UpdateStatsAsync();
        _statsTimer.Start();
    }

    protected override void ApplyShaderSettings(bool disableShaderForScreenshots)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (!disableShaderForScreenshots)
        {
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
            Task.Delay(300).ContinueWith(_ => Dispatcher.Invoke(() => GlassBase.Effect = new LiquidGlassEffect { Amount = 0.08 }));
            _captureTimer.Start();
        }
        else
        {
            SetWindowDisplayAffinity(hwnd, 0); // WDA_NONE
            GlassBase.Effect = null;
            _captureTimer.Stop();
        }
    }

    private void UpdateRealtimeBackground()
    {
        if (!this.IsVisible || this.ActualWidth <= 0 || this.ActualHeight <= 0) return;

        try
        {
            // Явно указываем пространство имен для Point
            System.Windows.Point p = this.PointToScreen(new System.Windows.Point(0, 0));
            int w = (int)this.ActualWidth;
            int h = (int)this.ActualHeight;


            IntPtr hDesk = GetDesktopWindow();
            IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc);
            IntPtr hBmp = CreateCompatibleBitmap(hSrc, w, h);
            IntPtr hOld = SelectObject(hDest, hBmp);

            BitBlt(hDest, 0, 0, w, h, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);

            // English: Use options to prevent WPF from caching every single frame in RAM
            var bmpSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBmp,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            bmpSource.Freeze();

            DeleteDC(hDest);
            ReleaseDC(hDesk, hSrc);
            DeleteObject(hBmp);

            WallpaperBrush.ImageSource = bmpSource;

            // English: Aggressive per-frame cleanup suggestion for the GC
            bmpSource = null;
        }
        catch { }
    }

    private async Task UpdateStatsAsync()
    {
        var stats = await Task.Run(() =>
        {
            double cpu = 0, gpu = 0;
            foreach (var hw in _computer.Hardware)
            {
                hw.Update();

                // English: Look for CPU Load
                if (hw.HardwareType == HardwareType.Cpu)
                {
                    // English: Specifically look for the "Total" sensor to avoid 0% on individual cores
                    var sensor = hw.Sensors.FirstOrDefault(s =>
                        s.SensorType == SensorType.Load &&
                        (s.Name.Contains("Total") || hw.Sensors.Count() == 1));

                    if (sensor?.Value != null) cpu = (double)sensor.Value;
                }

                // English: Look for GPU Load
                if (hw.HardwareType.ToString().Contains("Gpu"))
                {
                    var sensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                    if (sensor?.Value != null) gpu = (double)sensor.Value;
                }
            }
            return (cpu, gpu);
        });

        // English: Apply values to properties (triggers UI update via INotifyPropertyChanged)
        CpuText = $"{(int)stats.cpu}%";
        CpuValue = stats.cpu;
        GpuText = $"{(int)stats.gpu}%";
        GpuValue = stats.gpu;
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => this.Close();

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer.Stop();
        _statsTimer.Stop();
        _computer.Close();
        base.OnClosed(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}