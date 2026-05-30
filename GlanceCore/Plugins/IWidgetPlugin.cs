namespace GlanceCore.Plugins;

using GlanceCore.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Effects;

public interface IWidgetPlugin
{
    string Id { get; }
    string Title { get; }
    string Description { get; }
    string PreviewImagePath { get; }
    int DefaultWidth { get; }
    Type WidgetWindowType { get; }
    FrameworkElement? GetSettingsUI(GlanceCore.Core.WidgetState state, Action saveCallback);
    List<SkinItemModel>? GetCustomSkins() => null;
    ResourceDictionary? GetSkinResources(string skinId) => null;
}

public interface ISkinPlugin
{
    string Id { get; }
    string Name { get; }
    string PreviewImagePath { get; }
    string Color { get; }
    ResourceDictionary? GetResources();
    ShaderEffect? GetEffect();
}