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
    private readonly System.Collections.Generic.Queue<double> _cpuHistory = new(new double[30]); // Храним 30 последних значений
    private System.Windows.Media.PointCollection _cpuGraphPoints = new();
    public System.Windows.Media.PointCollection CpuGraphPoints { get => _cpuGraphPoints; set { _cpuGraphPoints = value; OnPropertyChanged(); } }
    private readonly Computer _computer;
    private readonly DispatcherTimer _statsTimer;
    private readonly DispatcherTimer _captureTimer;
    private string _tempText = "--°C"; public string TempText { get => _tempText; set { _tempText = value; OnPropertyChanged(); } }
    private string _gpuTempText = "--°C"; public string GpuTempText { get => _gpuTempText; set { _gpuTempText = value; OnPropertyChanged(); } }
    private string _ramText = "0%"; public string RamText { get => _ramText; set { _ramText = value; OnPropertyChanged(); } }
    private double _ramValue = 0; public double RamValue { get => _ramValue; set { _ramValue = value; OnPropertyChanged(); } }
    private string _cpuText = "0%"; public string CpuText { get => _cpuText; set { _cpuText = value; OnPropertyChanged(); } }
    private double _cpuValue = 0; public double CpuValue { get => _cpuValue; set { _cpuValue = value; OnPropertyChanged(); } }
    private string _gpuText = "0%"; public string GpuText { get => _gpuText; set { _gpuText = value; OnPropertyChanged(); } }
    private double _gpuValue = 0; public double GpuValue { get => _gpuValue; set { _gpuValue = value; OnPropertyChanged(); } }

    private readonly System.Collections.Generic.Queue<double> _gpuHistory = new(new double[30]);
    private System.Windows.Media.PointCollection _gpuGraphPoints = new();
    public System.Windows.Media.PointCollection GpuGraphPoints { get => _gpuGraphPoints; set { _gpuGraphPoints = value; OnPropertyChanged(); } }
    public HardwareWidget()
    {
        InitializeComponent();
        this.SizeChanged += HardwareWidget_SizeChanged;
        _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true };
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
    protected override void UpdateShaderIntensity()
    {
        if (GlassBase?.Effect is UI.Shaders.LiquidGlassEffect glass)
        {
            // Чем меньше прозрачность, тем слабее искажение. 
            // При Opacity = 0 искажение полностью исчезнет.
            glass.Amount = -0.15 * BgOpacity;
        }
    }
    private void HardwareWidget_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MainRoot == null) return;

        // Получаем текущую высоту виджета
        double h = MainRoot.ActualHeight;

        // Логика адаптивности: чем меньше высота, тем больше блоков скрываем
        // Пороги высоты подобраны под отступы XAML
        BlockRam.Visibility = h >= 270 ? Visibility.Visible : Visibility.Collapsed;
        BlockGpuTemp.Visibility = h >= 230 ? Visibility.Visible : Visibility.Collapsed;
        BlockGpu.Visibility = h >= 190 ? Visibility.Visible : Visibility.Collapsed;
        BlockCpuTemp.Visibility = h >= 140 ? Visibility.Visible : Visibility.Collapsed;

        // Если высота совсем маленькая (меньше 100), скрываем даже заголовок SYSTEM
        TitleText.Visibility = h >= 100 ? Visibility.Visible : Visibility.Collapsed;
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
    protected override void ApplySkinSpecificVisuals()
    {
        base.ApplySkinSpecificVisuals();
        var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Hardware_01");
        if (state != null) ApplySwap(state.SwapCpuGpu);
    }
    public void ApplySwap(bool swap)
    {
        System.Windows.Controls.Grid.SetRow(BlockCpu, swap ? 1 : 0);
        System.Windows.Controls.Grid.SetRow(BlockGpu, swap ? 0 : 1);
    }
    private async Task UpdateStatsAsync()
    {
        // 1. СБОР ДАННЫХ (Фоновый поток)
        var stats = await Task.Run(() => {
            double cpu = 0, gpu = 0, cpuTemp = 0, gpuTemp = 0, ram = 0;
            foreach (var hw in _computer.Hardware)
            {
                hw.Update();

                // Нагрузка
                var load = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Total") || hw.Sensors.Count() == 1));
                if (load?.Value != null)
                {
                    if (hw.HardwareType == HardwareType.Cpu) cpu = (double)load.Value;
                    else if (hw.HardwareType.ToString().Contains("Gpu")) gpu = (double)load.Value;
                    else if (hw.HardwareType == HardwareType.Memory) ram = (double)load.Value;
                }

                // Температуры (Агрессивный поиск)
                var temp = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature &&
                    (s.Name.Contains("Core") || s.Name.Contains("Package") || s.Name.Contains("Tctl") || s.Name.Contains("CCD")));

                if (temp == null) temp = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);

                if (temp?.Value != null)
                {
                    if (hw.HardwareType == HardwareType.Cpu || hw.HardwareType == HardwareType.Motherboard)
                        cpuTemp = (double)temp.Value;
                    else if (hw.HardwareType.ToString().Contains("Gpu"))
                        gpuTemp = (double)temp.Value;
                }
            }
            return (cpu, gpu, cpuTemp, gpuTemp, ram);
        });
        // 2. ОБНОВЛЕНИЕ ТЕКСТА И ПОЛОСОК (Тут переменная stats уже существует!)
        CpuText = $"{(int)stats.cpu}%"; CpuValue = stats.cpu;
        GpuText = $"{(int)stats.gpu}%"; GpuValue = stats.gpu;
        RamText = $"{(int)stats.ram}%"; RamValue = stats.ram;
        TempText = stats.cpuTemp > 0 ? $"{(int)stats.cpuTemp}°C" : "--°C";
        GpuTempText = stats.gpuTemp > 0 ? $"{(int)stats.gpuTemp}°C" : "--°C";

        // 3. ГРАФИК CPU
        _cpuHistory.Enqueue(stats.cpu);
        if (_cpuHistory.Count > 30) _cpuHistory.Dequeue();

        var cpuPoints = new System.Windows.Media.PointCollection();
        int xCpu = 0;
        foreach (var val in _cpuHistory)
        {
            cpuPoints.Add(new System.Windows.Point(xCpu, 100 - val));
            xCpu += 10;
        }
        CpuGraphPoints = cpuPoints;

        // 4. ГРАФИК GPU
        _gpuHistory.Enqueue(stats.gpu);
        if (_gpuHistory.Count > 30) _gpuHistory.Dequeue();

        var gpuPoints = new System.Windows.Media.PointCollection();
        int xGpu = 0;
        foreach (var val in _gpuHistory)
        {
            gpuPoints.Add(new System.Windows.Point(xGpu, 100 - val));
            xGpu += 10;
        }
        GpuGraphPoints = gpuPoints;
    }
    private void CloseWidget_Click(object sender, RoutedEventArgs e) => GlanceCore.Core.WidgetHost.CloseWidgetExplicitly("Hardware_01");
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}