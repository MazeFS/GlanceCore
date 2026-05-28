namespace GlanceCore.Widgets.Hardware;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

public partial class HardwareWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys; public ulong ullAvailPhys;
        public ulong ullTotalPageFile; public ulong ullAvailPageFile; public ulong ullTotalVirtual;
        public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX() { this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    private readonly DispatcherTimer _statsTimer;
    private PerformanceCounter? _cpuCounter;
    private readonly Dictionary<string, PerformanceCounter> _activeGpuCounters = new();
    private readonly Queue<double> _cpuHistory = new(new double[30]);
    private readonly Queue<double> _gpuHistory = new(new double[30]);

    private string _cpuText = "0%"; public string CpuText { get => _cpuText; set { _cpuText = value; OnPropertyChanged(); } }
    private double _cpuValue = 0; public double CpuValue { get => _cpuValue; set { _cpuValue = value; OnPropertyChanged(); } }
    private string _gpuText = "0%"; public string GpuText { get => _gpuText; set { _gpuText = value; OnPropertyChanged(); } }
    private double _gpuValue = 0; public double GpuValue { get => _gpuValue; set { _gpuValue = value; OnPropertyChanged(); } }
    private string _ramText = "0%"; public string RamText { get => _ramText; set { _ramText = value; OnPropertyChanged(); } }
    private double _ramValue = 0; public double RamValue { get => _ramValue; set { _ramValue = value; OnPropertyChanged(); } }

    private PointCollection _cpuGraphPoints = new(); public PointCollection CpuGraphPoints { get => _cpuGraphPoints; set { _cpuGraphPoints = value; OnPropertyChanged(); } }
    private PointCollection _gpuGraphPoints = new(); public PointCollection GpuGraphPoints { get => _gpuGraphPoints; set { _gpuGraphPoints = value; OnPropertyChanged(); } }

    public HardwareWidget()
    {
        InitializeComponent();
        this.SizeChanged += HardwareWidget_SizeChanged;

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
    }

    private async Task UpdateStatsAsync()
    {
        var stats = await Task.Run(() => {
            double cpu = 0, gpu = 0, ram = 0;
            try { if (_cpuCounter != null) cpu = _cpuCounter.NextValue(); } catch { }
            try { var mem = new MEMORYSTATUSEX(); if (GlobalMemoryStatusEx(mem)) ram = mem.dwMemoryLoad; } catch { }
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var currentInstances = category.GetInstanceNames().Where(i => i.EndsWith("engtype_pid3D")).ToList();
                var staleKeys = _activeGpuCounters.Keys.Where(k => !currentInstances.Contains(k)).ToList();
                foreach (var key in staleKeys)
                {
                    _activeGpuCounters[key].Dispose();
                    _activeGpuCounters.Remove(key);
                }
                double gpuSum = 0;
                foreach (var instance in currentInstances)
                {
                    if (!_activeGpuCounters.TryGetValue(instance, out var counter))
                    {
                        try
                        {
                            counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                            counter.NextValue();
                            _activeGpuCounters[instance] = counter;
                        }
                        catch { }
                    }
                    else
                    {
                        try { gpuSum += counter.NextValue(); } catch { }
                    }
                }
                gpu = Math.Min(gpuSum, 100);
            }
            catch { }
            return (cpu, gpu, ram);
        });

        CpuText = $"{(int)Math.Round(stats.cpu)}%"; CpuValue = stats.cpu;
        GpuText = $"{(int)Math.Round(stats.gpu)}%"; GpuValue = stats.gpu;
        RamText = $"{(int)stats.ram}%"; RamValue = stats.ram;

        _cpuHistory.Enqueue(stats.cpu); if (_cpuHistory.Count > 30) _cpuHistory.Dequeue();
        var cpuPts = new PointCollection(); int x = 0;
        foreach (var val in _cpuHistory) { cpuPts.Add(new Point(x, 100 - val)); x += 10; }
        CpuGraphPoints = cpuPts;

        _gpuHistory.Enqueue(stats.gpu); if (_gpuHistory.Count > 30) _gpuHistory.Dequeue();
        var gpuPts = new PointCollection(); int xG = 0;
        foreach (var val in _gpuHistory) { gpuPts.Add(new Point(xG, 100 - val)); xG += 10; }
        GpuGraphPoints = gpuPts;
    }

    public override void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)
    {
        base.ApplySettings(state, isGlobalLocked, isGlobalShaderEnabled);
        if (state != null)
        {
            if (GridCpuGraph != null) GridCpuGraph.Visibility = state.ShowHardwareGraphs ? Visibility.Visible : Visibility.Collapsed;
            if (BarCpu != null) BarCpu.Visibility = state.ShowHardwareGraphs ? Visibility.Collapsed : Visibility.Visible;
            if (GridGpuGraph != null) GridGpuGraph.Visibility = state.ShowHardwareGraphs ? Visibility.Visible : Visibility.Collapsed;
            if (BarGpu != null) BarGpu.Visibility = state.ShowHardwareGraphs ? Visibility.Collapsed : Visibility.Visible;

            ApplyOrder(state.HardwareOrder);
        }
    }

    private void HardwareWidget_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MainRoot == null) return;
        double h = MainRoot.ActualHeight;
        var BlockRam = FindName("BlockRam") as UIElement;
        var BlockGpu = FindName("BlockGpu") as UIElement;
        var TitleText = FindName("TitleText") as UIElement;

        if (BlockRam != null) BlockRam.Visibility = h >= 270 ? Visibility.Visible : Visibility.Collapsed;
        if (BlockGpu != null) BlockGpu.Visibility = h >= 190 ? Visibility.Visible : Visibility.Collapsed;
        if (TitleText != null) TitleText.Visibility = h >= 100 ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void ApplySkinSpecificVisuals()
    {
        base.ApplySkinSpecificVisuals();
        var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Hardware_01");
        if (state != null) ApplyOrder(state.HardwareOrder);
    }

    public void ApplyOrder(List<string> order)
    {
        var panel = FindName("SensorsPanel") as System.Windows.Controls.StackPanel;
        if (panel == null) return;
        panel.Children.Clear();
        foreach (var item in order)
        {
            if (item == "CPU" && FindName("BlockCpu") is UIElement c) panel.Children.Add(c);
            if (item == "GPU" && FindName("BlockGpu") is UIElement g) panel.Children.Add(g);
            if (item == "RAM" && FindName("BlockRam") is UIElement r) panel.Children.Add(r);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _statsTimer.Stop();
        foreach (var counter in _activeGpuCounters.Values) counter.Dispose();
        base.OnClosed(e);
    }
}