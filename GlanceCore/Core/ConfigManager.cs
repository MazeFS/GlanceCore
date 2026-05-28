namespace GlanceCore.Core;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public enum WidgetStyleType { LiquidGlass, Minimalism, Retro }

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
    public double CustomWidth { get; set; } = -1;
    public double CustomHeight { get; set; } = -1;
    public string SkinId { get; set; } = "LiquidGlass";

    public string TextColor { get; set; } = "#FFFFFF";
    public string BgColor { get; set; } = "#00000000";
    public string BorderColor { get; set; } = "#40FFFFFF";
    public string CustomData { get; set; } = "";

    public double CornerRadius { get; set; } = 24.0;
    public List<string> HardwareOrder { get; set; } = new List<string> { "CPU", "GPU", "RAM" };
    public bool ShowDayOfWeek { get; set; } = true;
    public bool ShowDate { get; set; } = true;
    public bool ShowTime { get; set; } = true;
    public bool ShowPlate { get; set; } = false;
    public string TimeSeparator { get; set; } = ":";
    public bool ShowSeconds { get; set; } = false;
    public double DateDayFontSize { get; set; } = 12.0;
    public double DateDateFontSize { get; set; } = 12.0;
    public double DateTimeFontSize { get; set; } = 20.0;
    public bool ShowBorder { get; set; } = true;
    public string DateDayColor { get; set; } = "#FFFFFF";
    public string DateDateColor { get; set; } = "#FFFFFF";
    public string DateTimeColor { get; set; } = "#FFFFFF";
    public bool IsVerticalTime { get; set; } = false;
    public bool ShowMediaTimer { get; set; } = true;
    public bool ShowHardwareGraphs { get; set; } = true;
    public string WeatherCity { get; set; } = "";
}

public class GlobalConfig
{
    public bool RunAtStartup { get; set; } = false;
    public bool LockWidgets { get; set; } = false;
    public bool EnableShader { get; set; } = true;
    public bool StreamerMode { get; set; } = false;
    public bool GameMode { get; set; } = true;
    public string Language { get; set; } = "EN";
    public string HubTheme { get; set; } = "Original";
    public int IdleFps { get; set; } = 15;
    public int MovingFps { get; set; } = 60;
    public string AiEndpoint { get; set; } = "https://api.openai.com/v1";
    public string AiApiKey { get; set; } = "";
    public string AiModel { get; set; } = "llama3";
    public string AiSystemPrompt { get; set; } = "You are a helpful assistant.";
    public double AiTemperature { get; set; } = 0.7;

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