namespace GlanceCore.Core;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class WidgetInfo : INotifyPropertyChanged
{
    public string Id { get; set; } = "";
    public string DisplayTitle => System.Windows.Application.Current.FindResource($"Lang_Widget_Title_{Id}") as string ?? Title;
    public string DisplayDescription => System.Windows.Application.Current.FindResource($"Lang_Widget_Desc_{Id}") as string ?? Description;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string PreviewImage { get; set; } = "";
    public int Width { get; set; } = 180;  // 280 для широких, 180 для квадратных
    public Type WidgetType { get; set; } = null!; // Тип окна для авто-создания

    // English: Real-time UI binding for the toggle switch
    public bool IsActive
    {
        get => WidgetHost.IsWidgetActive(Id);
        set { OnPropertyChanged(); }
    }

    public void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(DisplayDescription));
    }
    public object? PluginInstance { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}