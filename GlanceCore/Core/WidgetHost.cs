namespace GlanceCore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GlanceCore.Widgets;

public static class WidgetHost
{
    private static GlobalConfig _config = new();
    private static readonly Dictionary<string, Window> _activeWidgets = new();

    // English: The global registry of all available widgets
    public static ObservableCollection<WidgetInfo> AvailableWidgets { get; } = new();

    public static GlobalConfig CurrentConfig => _config;

    public static void Initialize()
    {
        _config = ConfigManager.Load();
        RegisterBuiltInWidgets();
    }

    private static void RegisterBuiltInWidgets()
    {
        // English: Prevent duplication on re-initialization
        AvailableWidgets.Clear();

        // 1. Hardware Monitor
        AvailableWidgets.Add(new WidgetInfo
        {
            Id = "Hardware_01",
            Title = "Система",
            Description = "Hardware Monitor",
            PreviewImage = "pack://application:,,,/Resource/ScreenShots/HardwarePrieview.png",
            Width = 250,
            WidgetType = typeof(Widgets.Hardware.HardwareWidget)
        });

        AvailableWidgets.Add(new WidgetInfo
        {
            Id = "Media_01",
            Title = "Медиа-плеер",
            Description = "Музыка и видео",
            PreviewImage = "pack://application:,,,/Resource/ScreenShots/MediaPrieview.png", // Закинешь потом картинку
            Width = 250, // Квадратная плитка
            WidgetType = typeof(Widgets.Media.MediaWidget)
        });

        // English: Remove or comment this if you don't want the placeholder
        //AvailableWidgets.Add(new WidgetInfo
        //{
        //    Id = "Weather_01",
        //    Title = "Погода",
        //    Description = "Open-Meteo",
        //    PreviewImage = "/GlanceCore;component/Resources/WeatherPreview.png",
        //    Width = 180,
        //    WidgetType = typeof(Window)
        //});
    }

    public static void RestoreActiveWidgets()
    {
        foreach (var widget in AvailableWidgets)
            if (IsWidgetActive(widget.Id)) ToggleWidget(widget.Id);
    }

    public static bool IsWidgetActive(string id) => _config.Widgets.ContainsKey(id) && _config.Widgets[id].IsEnabled;

    // English: Data-Driven toggle (creates window by its Type)
    public static void ToggleWidget(string widgetId)
    {
        var info = AvailableWidgets.FirstOrDefault(w => w.Id == widgetId);
        if (info == null || info.WidgetType == typeof(Window)) return; // Игнорируем заглушки

        if (_activeWidgets.TryGetValue(widgetId, out var widget))
        {
            widget.Close();
            _activeWidgets.Remove(widgetId);
            _config.Widgets[widgetId].IsEnabled = false;
        }
        else
        {
            // English: Auto-spawn window using Reflection
            widget = (Window)Activator.CreateInstance(info.WidgetType)!;

            if (!_config.Widgets.ContainsKey(widgetId))
                _config.Widgets[widgetId] = new WidgetState { IsEnabled = true };
            else
                _config.Widgets[widgetId].IsEnabled = true;

            var state = _config.Widgets[widgetId];
            if (state.X > 0) { widget.Left = state.X; widget.Top = state.Y; }

            widget.LocationChanged += (s, e) => { state.X = widget.Left; state.Y = widget.Top; };

            widget.Show();
            _activeWidgets.Add(widgetId, widget);

            if (widget is BaseWidgetWindow baseW)
                baseW.ApplySettings(state.Opacity, state.Scale, _config.LockWidgets, state.IsAlwaysOnTop, state.FontFamily, state.FontSize, _config.DisableShaderForScreenshots);
        }
        ConfigManager.Save(_config);
        info.NotifyStateChanged(); // Update Hub UI Switch
    }

    public static void ApplySystemSettings()
    {
        foreach (var kvp in _activeWidgets)
        {
            if (kvp.Value is BaseWidgetWindow widget)
            {
                var state = _config.Widgets[kvp.Key];
                // FIXED: Added missing fontFamily and fontSize arguments
                widget.ApplySettings(
                    state.Opacity,
                    state.Scale,
                    _config.LockWidgets,
                    state.IsAlwaysOnTop,
                    state.FontFamily,
                    state.FontSize,
                    _config.DisableShaderForScreenshots);
            }
        }
        ConfigManager.Save(_config);
    }

    public static void UpdateWidgetVisuals(string id, double opacity, double scale, bool isTopmost, string fontFamily, double fontSize)
    {
        if (!_config.Widgets.ContainsKey(id)) return;

        var s = _config.Widgets[id];
        s.Opacity = opacity;
        s.Scale = scale;
        s.IsAlwaysOnTop = isTopmost;
        s.FontFamily = fontFamily;
        s.FontSize = fontSize;

        if (_activeWidgets.TryGetValue(id, out var window) && window is BaseWidgetWindow widget)
        {
            widget.ApplySettings(opacity, scale, _config.LockWidgets, isTopmost, fontFamily, fontSize, _config.DisableShaderForScreenshots);
        }

        ConfigManager.Save(_config);
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
