namespace GlanceCore.Widgets.Hardware;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public partial class HardwareWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    // Нативные GDI функции для быстрого захвата экрана
    [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr d, int dx, int dy, int w, int h, IntPtr s, int sx, int sy, int r);
    [DllImport("user32.dll")] static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] static extern IntPtr GetWindowDC(IntPtr w);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr w, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX() { this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    private readonly DispatcherTimer _captureTimer;
    private readonly DispatcherTimer _statsTimer;

    private PerformanceCounter? _cpuCounter;
    private readonly List<PerformanceCounter> _gpuCounters = new();

    private readonly Queue<double> _cpuHistory = new(new double[30]);
    private readonly Queue<double> _gpuHistory = new(new double[30]);

    private string _cpuText = "0%"; public string CpuText { get => _cpuText; set { _cpuText = value; OnPropertyChanged(); } }
    private double _cpuValue = 0; public double CpuValue { get => _cpuValue; set { _cpuValue = value; OnPropertyChanged(); } }

    private string _gpuText = "0%"; public string GpuText { get => _gpuText; set { _gpuText = value; OnPropertyChanged(); } }
    private double _gpuValue = 0; public double GpuValue { get => _gpuValue; set { _gpuValue = value; OnPropertyChanged(); } }

    private string _tempText = "--°C"; public string TempText { get => _tempText; set { _tempText = value; OnPropertyChanged(); } }
    private string _gpuTempText = "--°C"; public string GpuTempText { get => _gpuTempText; set { _gpuTempText = value; OnPropertyChanged(); } }

    private string _ramText = "0%"; public string RamText { get => _ramText; set { _ramText = value; OnPropertyChanged(); } }
    private double _ramValue = 0; public double RamValue { get => _ramValue; set { _ramValue = value; OnPropertyChanged(); } }

    private PointCollection _cpuGraphPoints = new();
    public PointCollection CpuGraphPoints { get => _cpuGraphPoints; set { _cpuGraphPoints = value; OnPropertyChanged(); } }

    private PointCollection _gpuGraphPoints = new();
    public PointCollection GpuGraphPoints { get => _gpuGraphPoints; set { _gpuGraphPoints = value; OnPropertyChanged(); } }

    public HardwareWidget()
    {
        InitializeComponent();
        this.SizeChanged += HardwareWidget_SizeChanged;

        // Инициализируем таймер захвата, но НЕ запускаем его в конструкторе!
        _captureTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
        _captureTimer.Tick += (s, e) => UpdateRealtimeBackground();

        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
        _statsTimer.Tick += async (s, e) => await UpdateStatsAsync();

        Task.Run(() => {
            InitializeCounters();
            Dispatcher.Invoke(() => _statsTimer.Start());
        });
    }

    private void InitializeCounters()
    {
        try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpuCounter.NextValue(); } catch { }
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames();
            foreach (var instance in instances)
            {
                if (instance.EndsWith("engtype_pid3D"))
                    _gpuCounters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", instance));
            }
        }
        catch { }
    }

    private async Task UpdateStatsAsync()
    {
        var stats = await Task.Run(() => {
            double cpu = 0;
            double gpu = 0;
            double ram = 0;

            try { if (_cpuCounter != null) cpu = _cpuCounter.NextValue(); } catch { }
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus)) ram = memStatus.dwMemoryLoad;
            }
            catch { }
            try
            {
                double gpuSum = 0;
                foreach (var counter in _gpuCounters) { try { gpuSum += counter.NextValue(); } catch { } }
                gpu = Math.Min(gpuSum, 100);
            }
            catch { }

            return (cpu, gpu, ram);
        });

        CpuText = $"{(int)Math.Round(stats.cpu)}%"; CpuValue = stats.cpu;
        GpuText = $"{(int)Math.Round(stats.gpu)}%"; GpuValue = stats.gpu;
        RamText = $"{(int)stats.ram}%"; RamValue = stats.ram;

        TempText = "--°C";
        GpuTempText = "--°C";

        _cpuHistory.Enqueue(stats.cpu);
        if (_cpuHistory.Count > 30) _cpuHistory.Dequeue();
        var cpuPoints = new PointCollection();
        int xCpu = 0;
        foreach (var val in _cpuHistory) { cpuPoints.Add(new Point(xCpu, 100 - val)); xCpu += 10; }
        CpuGraphPoints = cpuPoints;

        _gpuHistory.Enqueue(stats.gpu);
        if (_gpuHistory.Count > 30) _gpuHistory.Dequeue();
        var gpuPoints = new PointCollection();
        int xGpu = 0;
        foreach (var val in _gpuHistory) { gpuPoints.Add(new Point(xGpu, 100 - val)); xGpu += 10; }
        GpuGraphPoints = gpuPoints;
    }

    public override void ApplyShaderSettings(bool enable)
    {
        _shaderShouldBeEnabled = enable;
        ApplySkinSpecificVisuals();
    }

    // Идеальная синхронизация жизненного цикла с WeatherWidget
    protected override void ApplySkinSpecificVisuals()
    {
        if (GlassBase == null || _captureTimer == null) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        _captureTimer.Stop();
        GlassBase.Effect = null;
        SetWindowDisplayAffinity(hwnd, WDA_NONE);
        WallpaperBrush.ImageSource = null;

        if (_shaderShouldBeEnabled)
        {
            if (CurrentStyle == Core.WidgetStyleType.Minimalism)
            {
                BackgroundLayer.CornerRadius = new CornerRadius(12);
                GlassBase.Visibility = Visibility.Collapsed;
                MinimalBase.Visibility = Visibility.Visible;
                GlossBevel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Liquid Glass
                BackgroundLayer.CornerRadius = WidgetCornerRadius;
                GlassBase.Visibility = Visibility.Visible;
                MinimalBase.Visibility = Visibility.Collapsed;
                GlossBevel.Visibility = Visibility.Visible;

                // СНАЧАЛА полностью скрываем окно от скриншотов
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

                // ЗАТЕМ вешаем эффект преломления
                GlassBase.Effect = new UI.Shaders.LiquidGlassEffect { Amount = -0.15 * BgOpacity };

                // И только в самом конце запускаем таймер захвата!
                _captureTimer.Start();
            }
        }
        else
        {
            GlassBase.Visibility = Visibility.Collapsed;
            MinimalBase.Visibility = Visibility.Visible;
            GlossBevel.Visibility = Visibility.Collapsed;
        }

        // Применяем порядок датчиков
        var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Hardware_01");
        if (state != null) ApplyOrder(state.HardwareOrder);
    }

    protected override void UpdateShaderIntensity()
    {
        if (GlassBase?.Effect is UI.Shaders.LiquidGlassEffect glass)
        {
            glass.Amount = -0.15 * BgOpacity;
        }
    }

    private void UpdateRealtimeBackground()
    {
        if (!this.IsVisible || MainRoot == null || MainRoot.ActualWidth <= 0 || MainRoot.ActualHeight <= 0) return;
        if (PresentationSource.FromVisual(MainRoot) == null) return;

        try
        {
            var p = MainRoot.PointToScreen(new Point(0, 0));
            int w = (int)MainRoot.ActualWidth;
            int h = (int)MainRoot.ActualHeight;

            IntPtr hDesk = GetDesktopWindow(); IntPtr hSrc = GetWindowDC(hDesk);
            IntPtr hDest = CreateCompatibleDC(hSrc); IntPtr hBmp = CreateCompatibleBitmap(hSrc, w, h);
            IntPtr hOld = SelectObject(hDest, hBmp);

            BitBlt(hDest, 0, 0, w, h, hSrc, (int)p.X, (int)p.Y, 0x00CC0020);

            SelectObject(hDest, hOld);
            var bmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            DeleteDC(hDest); ReleaseDC(hDesk, hSrc); DeleteObject(hBmp);

            bmp.Freeze();
            WallpaperBrush.ImageSource = bmp;
        }
        catch { }
    }

    private void HardwareWidget_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MainRoot == null) return;
        double h = MainRoot.ActualHeight;

        BlockRam.Visibility = h >= 270 ? Visibility.Visible : Visibility.Collapsed;
        BlockGpu.Visibility = h >= 190 ? Visibility.Visible : Visibility.Collapsed;
        BlockCpuTemp.Visibility = h >= 140 ? Visibility.Visible : Visibility.Collapsed;
        TitleText.Visibility = h >= 100 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ApplyOrder(List<string> order)
    {
        if (SensorsPanel == null) return;
        SensorsPanel.Children.Clear();
        foreach (var item in order)
        {
            if (item == "CPU" && BlockCpu != null) SensorsPanel.Children.Add(BlockCpu);
            if (item == "GPU" && BlockGpu != null) SensorsPanel.Children.Add(BlockGpu);
            if (item == "RAM" && BlockRam != null) SensorsPanel.Children.Add(BlockRam);
        }
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => Core.WidgetHost.CloseWidgetExplicitly("Hardware_01");

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer.Stop();
        _statsTimer.Stop();
        base.OnClosed(e);
    }
}