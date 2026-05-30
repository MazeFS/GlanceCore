namespace GlanceCore.Core;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class StoreItemModel : INotifyPropertyChanged
{
    private bool _isDownloaded = false;

    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string PreviewImage { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Author { get; set; } = "Unknown";
    public double Rating { get; set; } = 5.0;
    public string Category { get; set; } = "Essentials";
    public string RatingStars => new string('★', (int)Math.Round(Rating));

    public bool IsDownloaded
    {
        get => _isDownloaded;
        set { _isDownloaded = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}