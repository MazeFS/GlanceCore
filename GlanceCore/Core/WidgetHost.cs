namespace GlanceCore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using GlanceCore.Widgets;
using GlanceCore.Widgets.Hardware;
using GlanceCore.Widgets.Media;
using GlanceCore.Widgets.ImageFrame;
using GlanceCore.Widgets.Weather;
using GlanceCore.Widgets.Time;

public static class WidgetHost
{
    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int pquns);

    private static GlobalConfig _config = new();
    private static readonly Dictionary<string, Window> _activeWidgets = new();
    private static DispatcherTimer? _gameModeTimer;
    private static bool _isGameModeActive = false;
    public static List<GlanceCore.Plugins.ISkinPlugin> GlobalCustomSkins { get; } = new();
    public static ObservableCollection<WidgetInfo> AvailableWidgets { get; } = new();
    public static GlobalConfig CurrentConfig => _config;

    public static void Initialize()
    {
        _config = ConfigManager.Load();
        RegisterBuiltInWidgets();

        var externalPlugins = GlanceCore.Plugins.PluginManager.LoadPlugins();
        foreach (var plugin in externalPlugins)
        {
            AvailableWidgets.Add(plugin);
        }

        _gameModeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _gameModeTimer.Tick += (s, e) => CheckGameMode();
        _gameModeTimer.Start();
    }
    private static void CheckGameMode()
    {
        if (!_config.GameMode) return;

        SHQueryUserNotificationState(out int state);
        bool isGaming = (state == 5 || state == 4);

        if (isGaming != _isGameModeActive)
        {
            _isGameModeActive = isGaming;
            foreach (var kvp in _activeWidgets)
            {
                if (kvp.Value is BaseWidgetWindow w && !w.Topmost)
                {
                    w.ApplyShaderSettings(!isGaming && _config.EnableShader);
                }
            }
        }
    }

    private static void RegisterBuiltInWidgets()
    {
        AvailableWidgets.Clear();
        AvailableWidgets.Add(new WidgetInfo { Id = "Date_01", Title = "Дата и время", Description = "Календарь", PreviewImage = "/Resource/ScreenShots/HardwarePreview.png", Width = 250, WidgetType = typeof(GlanceCore.Widgets.Date.DateWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "AI_01", Title = "AI Ассистент", Description = "API и Локальные модели", PreviewImage = "/Resource/ScreenShots/HardwarePreview.png", Width = 250, WidgetType = typeof(GlanceCore.Widgets.AI.AIAssistantWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "Hardware_01", Title = "Система", Description = "Hardware Monitor", PreviewImage = "/Resource/ScreenShots/HardwarePreview.png", Width = 250, WidgetType = typeof(HardwareWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "Media_01", Title = "Медиа-плеер", Description = "Музыка и видео", PreviewImage = "/Resource/ScreenShots/MediaPreview.png", Width = 250, WidgetType = typeof(MediaWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "Image_01", Title = "Фоторамка", Description = "Ваше фото", PreviewImage = "/Resource/ScreenShots/ImagePreview.png", Width = 250, WidgetType = typeof(ImageFrameWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "Weather_01", Title = "Погода", Description = "Open-Meteo", PreviewImage = "/Resource/ScreenShots/WeatherPreview.png", Width = 250, WidgetType = typeof(WeatherWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "Time_01", Title = "Часы", Description = "Текущее время", PreviewImage = "/Resource/ScreenShots/HardwarePreview.png", Width = 250, WidgetType = typeof(TimeWidget) });
        AvailableWidgets.Add(new WidgetInfo { Id = "Notes_01", Title = "Стикеры", Description = "Заметки на стекле", PreviewImage = "/Resource/ScreenShots/ImagePreview.png", Width = 250, WidgetType = typeof(GlanceCore.Widgets.StickyNotes.StickyNotesWidget) });
        
    }
    public static void RestoreActiveWidgets()
    {
        foreach (var widget in AvailableWidgets)
        {
            if (IsWidgetActive(widget.Id)) ToggleWidget(widget.Id);
        }
    }

    public static bool IsWidgetActive(string id) => _config.Widgets.ContainsKey(id) && _config.Widgets[id].IsEnabled;

    public static void ToggleWidget(string widgetId)
    {
        var info = AvailableWidgets.FirstOrDefault(w => w.Id == widgetId);
        if (info == null) return;

        if (_activeWidgets.TryGetValue(widgetId, out var widget))
        {
            _config.Widgets[widgetId].X = widget.Left;
            _config.Widgets[widgetId].Y = widget.Top;
            _config.Widgets[widgetId].IsEnabled = false;
            widget.Close();
            _activeWidgets.Remove(widgetId);
        }
        else
        {
            widget = (Window)Activator.CreateInstance(info.WidgetType)!;
            if (!_config.Widgets.ContainsKey(widgetId)) _config.Widgets[widgetId] = new WidgetState { IsEnabled = true };
            else _config.Widgets[widgetId].IsEnabled = true;

            var state = _config.Widgets[widgetId];
            if (state.X != -1) { widget.Left = state.X; widget.Top = state.Y; }
            widget.LocationChanged += (s, e) => { state.X = widget.Left; state.Y = widget.Top; };

            widget.Show();
            _activeWidgets.Add(widgetId, widget);

            if (widget is BaseWidgetWindow baseW)
                baseW.ApplySettings(state, _config.LockWidgets, _config.EnableShader);
        }
        ConfigManager.Save(_config);
        info.NotifyStateChanged();
        MemoryOptimizer.Trim();
    }

    public static void ApplySystemSettings()
    {
        foreach (var kvp in _activeWidgets)
        {
            if (kvp.Value is BaseWidgetWindow widget)
            {
                var state = _config.Widgets[kvp.Key];
                widget.ApplySettings(state, _config.LockWidgets, _config.EnableShader);
            }
        }
        ConfigManager.Save(_config);
    }

    public static void RefreshWidgetVisuals(string id)
    {
        if (!_config.Widgets.ContainsKey(id)) return;
        if (_activeWidgets.TryGetValue(id, out var window) && window is BaseWidgetWindow widget)
        {
            widget.ApplySettings(_config.Widgets[id], _config.LockWidgets, _config.EnableShader);
        }
        ConfigManager.Save(_config);
    }

    public static void RefreshWidgetCustomData(string id)
    {
        if (_activeWidgets.TryGetValue(id, out var window) && window is BaseWidgetWindow widget)
            widget.RefreshCustomData();
    }

    public static void CloseWidgetExplicitly(string widgetId)
    {
        if (_activeWidgets.TryGetValue(widgetId, out var widget))
        {
            _config.Widgets[widgetId].X = widget.Left;
            _config.Widgets[widgetId].Y = widget.Top;
            _config.Widgets[widgetId].IsEnabled = false;

            widget.Close();
            _activeWidgets.Remove(widgetId);
            ConfigManager.Save(_config);

            AvailableWidgets.FirstOrDefault(w => w.Id == widgetId)?.NotifyStateChanged();

            bool isHubOpen = System.Windows.Application.Current.Windows.Cast<Window>().Any(w => w is Views.HubWindow && w.IsVisible);
            if (_activeWidgets.Count == 0 && !isHubOpen)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
    public static void Shutdown()
    {
        foreach (var kvp in _activeWidgets)
        {
            _config.Widgets[kvp.Key].X = kvp.Value.Left;
            _config.Widgets[kvp.Key].Y = kvp.Value.Top;
        }
        ConfigManager.Save(_config);
    }
}