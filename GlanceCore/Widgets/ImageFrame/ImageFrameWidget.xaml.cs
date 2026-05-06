namespace GlanceCore.Widgets.ImageFrame;

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using GlanceCore.Core;

public partial class ImageFrameWidget : BaseWidgetWindow
{
    public ImageFrameWidget()
    {
        InitializeComponent();
        this.Loaded += (s, e) => RefreshCustomData();
    }

    // English: Triggered by WidgetHost when user selects a new image in Hub
    public override void RefreshCustomData()
    {
        var state = WidgetHost.CurrentConfig.Widgets.GetValueOrDefault("Image_01");
        if (state != null && !string.IsNullOrEmpty(state.CustomData))
        {
            ApplyImage(state.CustomData);
        }
    }

    private void ApplyImage(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // RAM Optimization
                bitmap.EndInit();
                bitmap.Freeze();

                DisplayedImage.Source = bitmap;
            }
        }
        catch { /* Ignore corrupted files */ }
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => WidgetHost.CloseWidgetExplicitly("Image_01");
}