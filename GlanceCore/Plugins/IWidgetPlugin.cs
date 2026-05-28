namespace GlanceCore.Plugins;

using System;
using System.Windows;

public interface IWidgetPlugin
{
    string Id { get; }
    string Title { get; }
    string Description { get; }
    string PreviewImagePath { get; }
    int DefaultWidth { get; }
    Type WidgetWindowType { get; }

    FrameworkElement? GetSettingsUI();
}