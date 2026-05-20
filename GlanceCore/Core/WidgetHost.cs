namespace GlanceCore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GlanceCore.Widgets;
using GlanceCore.Widgets.Hardware;
using GlanceCore.Widgets.Media;
using GlanceCore.Widgets.ImageFrame;
using GlanceCore.Widgets.Weather;

public static class WidgetHost
{
    private static GlobalConfig _config = new();
    private static readonly Dictionary<string, Window> _activeWidgets = new();

    // English: Global collection for Hub's ItemsControl binding
    public static ObservableCollection<WidgetInfo> AvailableWidgets { get; } = new();
    public static GlobalConfig CurrentConfig => _config;

    public static void Initialize()
    {
        _config = ConfigManager.Load();
        RegisterBuiltInWidgets();
    }

    private static void RegisterBuiltInWidgets()
    {
        AvailableWidgets.Clear();

        // 1. Hardware Monitor
        AvailableWidgets.Add(new WidgetInfo
        {
            Id = "Hardware_01",
            Title = "Система",
            Description = "Hardware Monitor",
            PreviewImage = "/Resource/ScreenShots/HardwarePreview.png",
            Width = 250, // БЫЛО 280, СТАЛО 220
            WidgetType = typeof(HardwareWidget)
        });

        // 2. Media Player
        AvailableWidgets.Add(new WidgetInfo {
            Id = "Media_01",
            Title = "Медиа-плеер",
            Description = "Музыка и видео",
            PreviewImage = "/Resource/ScreenShots/MediaPreview.png",
            Width = 250,
            WidgetType = typeof(MediaWidget)
        });

        // 3. Image Frame
        AvailableWidgets.Add(new WidgetInfo {
            Id = "Image_01",
            Title = "Фоторамка",
            Description = "Ваше фото",
            PreviewImage = "/Resource/ScreenShots/ImagePreview.png",
            Width = 250,
            WidgetType = typeof(ImageFrameWidget)
        });

        // 4. Weather
        AvailableWidgets.Add(new WidgetInfo {
            Id = "Weather_01",
            Title = "Погода",
            Description = "Open-Meteo",
            PreviewImage = "/Resource/ScreenShots/WeatherPreview.png",
            Width = 250,
            WidgetType = typeof(WeatherWidget)
        });
    }

    // English: Restore widgets that were open in the last session
    public static void RestoreActiveWidgets()
    {
        foreach (var widget in AvailableWidgets)
        {
            if (IsWidgetActive(widget.Id))
            {
                ToggleWidget(widget.Id);
            }
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

            if (!_config.Widgets.ContainsKey(widgetId))
                _config.Widgets[widgetId] = new WidgetState { IsEnabled = true };
            else
                _config.Widgets[widgetId].IsEnabled = true;

            var state = _config.Widgets[widgetId];

            if (state.X != -1) { widget.Left = state.X; widget.Top = state.Y; }

            widget.LocationChanged += (s, e) => {
                state.X = widget.Left;
                state.Y = widget.Top;
            };

            widget.Show();
            _activeWidgets.Add(widgetId, widget);

            // English: Pass the state object!
            if (widget is BaseWidgetWindow baseW)
                baseW.ApplySettings(state, _config.LockWidgets, _config.EnableShader);
        }
        ConfigManager.Save(_config);
        info.NotifyStateChanged();
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

    // English: NEW Clean method! Hub updates the config directly, then calls this.
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
        {
            widget.RefreshCustomData();
        }
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

            // English: If it was the last widget and Hub is hidden, shutdown the app to free RAM
            if (_activeWidgets.Count == 0)
            {
                if (Application.Current is App app && !app.IsHubVisible)
                {
                    Application.Current.Shutdown();
                }
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