namespace GlanceCore.Widgets.Hardware;

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;

public partial class HardwareWidget : GlanceCore.Widgets.BaseWidgetWindow, INotifyPropertyChanged
{
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr d, int dx, int dy, int w, int h, IntPtr s, int sx, int sy, int r);
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr w);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr w, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    private readonly Computer _computer;
    private readonly DispatcherTimer _statsTimer;
    private readonly DispatcherTimer _captureTimer;

    private string _cpuText = "0%"; public string CpuText { get => _cpuText; set { _cpuText = value; OnPropertyChanged(); } }
    private double _cpuValue = 0; public double CpuValue { get => _cpuValue; set { _cpuValue = value; OnPropertyChanged(); } }
    private string _gpuText = "0%"; public string GpuText { get => _gpuText; set { _gpuText = value; OnPropertyChanged(); } }
    private double _gpuValue = 0; public double GpuValue { get => _gpuValue; set { _gpuValue = value; OnPropertyChanged(); } }

    public HardwareWidget()
    {
        InitializeComponent();
        _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
        try { _computer.Open(); } catch { }

        // English: Ensure capture timer is RUNNING
        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();
        _captureTimer.Start();

        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _statsTimer.Tick += async (s, e) => await UpdateStatsAsync();
        _statsTimer.Start();

        this.Loaded += (s, e) => {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowDisplayAffinity(hwnd, 0x00000011);
        };
    }

    public override void ApplyShaderSettings(bool enable)
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        if (enable)
        {
            // 1. Включаем невидимость
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

            // 2. Включаем шейдер
            if (GlassBase.Effect == null)
                GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = -0.05 }; // Для Media поставь -0.15

            // 3. Запускаем захват экрана
            _captureTimer.Start();
        }
        else
        {
            // 1. Останавливаем захват экрана (чтобы не было туннеля)
            _captureTimer.Stop();

            // 2. Убираем шейдер
            GlassBase.Effect = null;

            // 3. Разрешаем Windows видеть окно для скриншота
            SetWindowDisplayAffinity(hwnd, WDA_NONE);

            // 4. Очищаем фон, чтобы он стал прозрачным/черным для чистого скрина
            WallpaperBrush.ImageSource = null;
        }
    }
    private void UpdateRealtimeBackground()
    {
        if (!this.IsVisible || this.ActualWidth <= 0) return;
        try
        {
            var p = this.PointToScreen(new Point(0, 0));
            IntPtr hDesk = GetDesktopWindow(); IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc); IntPtr hBmp = CreateCompatibleBitmap(hSrc, (int)ActualWidth, (int)ActualHeight);
            IntPtr hOld = SelectObject(hDest, hBmp);
            BitBlt(hDest, 0, 0, (int)ActualWidth, (int)ActualHeight, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);
            SelectObject(hDest, hOld);
            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Freeze();
            DeleteDC(hDest); ReleaseDC(hDesk, hSrc); DeleteObject(hBmp);
            WallpaperBrush.ImageSource = bmp;
        }
        catch { }
    }

    private async Task UpdateStatsAsync()
    {
        var stats = await Task.Run(() => {
            double cpu = 0, gpu = 0;
            foreach (var hw in _computer.Hardware)
            {
                hw.Update();
                var load = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Total") || hw.Sensors.Count() == 1));
                if (load?.Value != null)
                {
                    if (hw.HardwareType == HardwareType.Cpu) cpu = (double)load.Value;
                    else if (hw.HardwareType.ToString().Contains("Gpu")) gpu = (double)load.Value;
                }
            }
            return (cpu, gpu);
        });
        CpuText = $"{(int)stats.cpu}%"; CpuValue = stats.cpu;
        GpuText = $"{(int)stats.gpu}%"; GpuValue = stats.gpu;
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => GlanceCore.Core.WidgetHost.CloseWidgetExplicitly("Hardware_01");
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}