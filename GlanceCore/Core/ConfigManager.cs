namespace GlanceCore.Core;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
public class WidgetState
{
    public double X { get; set; } = -1;
    public double Y { get; set; } = -1;
    public bool IsEnabled { get; set; } = false;
    public double Opacity { get; set; } = 1.0;
    public double Scale { get; set; } = 1.0;
    public bool IsAlwaysOnTop { get; set; } = true;
    public string FontFamily { get; set; } = "Segoe UI Variable";
    public double FontSize { get; set; } = 12.0;
    public string CustomData { get; set; } = "";
}

public class GlobalConfig
{
    public bool RunAtStartup { get; set; } = false;
    public bool LockWidgets { get; set; } = false;

    // English: Standards names for HubWindow logic
    public bool EnableShader { get; set; } = true;
    public bool DisableShaderForScreenshots { get; set; } = false;

    public Dictionary<string, WidgetState> Widgets { get; set; } = new();
}
public static class ConfigManager
{
    private const string Path = "glance_config.json";
    public static GlobalConfig Load()
    {
        if (!File.Exists(Path)) return new GlobalConfig();
        try { return JsonSerializer.Deserialize<GlobalConfig>(File.ReadAllText(Path)) ?? new GlobalConfig(); }
        catch { return new GlobalConfig(); }
    }
    public static void Save(GlobalConfig config) =>
        File.WriteAllText(Path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
}