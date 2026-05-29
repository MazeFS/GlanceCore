namespace GlanceCore.Views;

using GlanceCore.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Net.Http;
using System.Text.Json;
using System.IO;


public class SkinItemModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Color { get; set; } = "";
    public string DisplayName => System.Windows.Application.Current.FindResource($"Lang_Style_{Id}") as string ?? Name;
}
public partial class HubWindow : Window
{

    private List<StoreItemModel> _fullStoreCatalog = new();
    private string _activeStoreCategory = "Essentials";
    private string _activeStoreSearch = "";
    private bool _isLoaded = false;
    private string _editingWidgetId = "";
    private bool _isUpdatingSize = false;
    private bool _isApplyingSettings = false;
    private void TimeSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            var state = cfg.Widgets[_editingWidgetId];

            state.ShowSeconds = TglShowSeconds.IsChecked == true;
            state.IsVerticalTime = TglVerticalTime.IsChecked == true;

            if (ComboTimeSeparator.SelectedItem is ComboBoxItem item)
                state.TimeSeparator = item.Content.ToString() ?? ":";

            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
        }
    }
    private void HubWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            e.Handled = true;
            BtnMenuToggle.IsChecked = !BtnMenuToggle.IsChecked;
            ToggleSidebar();
        }
    }

    private void HubWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && WidgetsPanel != null && WidgetsPanel.IsVisible && WidgetsPanel.IsMouseOver)
        {
            e.Handled = true;
            double step = e.Delta > 0 ? 0.1 : -0.1;
            double scale = Math.Clamp(WidgetsListScale.ScaleX + step, 0.6, 1.4);
            WidgetsListScale.ScaleX = scale;
            WidgetsListScale.ScaleY = scale;
        }
    }
    public HubWindow()
    {
        InitializeComponent();
        _isLoaded = false;

        var cfg = WidgetHost.CurrentConfig;

        if (WidgetsList != null)
        {
            UpdatePagination();
        }

        var hardwareToggle = this.FindName("HardwareToggle") as CheckBox;
        if (hardwareToggle != null)
        {
            hardwareToggle.IsChecked = WidgetHost.IsWidgetActive("Hardware_01");
        }

        if (TglAutoStart != null) TglAutoStart.IsChecked = cfg.RunAtStartup;
        if (TglLock != null) TglLock.IsChecked = cfg.LockWidgets;
        if (TglShader != null) TglShader.IsChecked = cfg.EnableShader;
        if (TglStreamerMode != null) TglStreamerMode.IsChecked = cfg.StreamerMode;
        if (TglGameMode != null) TglGameMode.IsChecked = cfg.GameMode;
        if (SldIdleFps != null) SldIdleFps.Value = cfg.IdleFps;
        if (SldMovingFps != null) SldMovingFps.Value = cfg.MovingFps;
        this.KeyDown += HubWindow_KeyDown;
        this.PreviewMouseWheel += HubWindow_PreviewMouseWheel;
        if (CarouselSkins != null)
        {
            CarouselSkins.ItemsSource = AvailableSkins;
        }

        if (CarouselThemes != null && cfg != null)
        {
            CarouselThemes.ItemsSource = AvailableHubThemes;
            foreach (HubThemeModel item in CarouselThemes.Items)
            {
                if (item != null && item.Id == cfg.HubTheme)
                {
                    CarouselThemes.SelectedItem = item;
                    break;
                }
            }
        }

        if (CarouselSkins != null)
        {
            CarouselSkins.ItemContainerGenerator.StatusChanged += (s, ev) =>
            {
                if (CarouselSkins?.ItemContainerGenerator?.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    Dispatcher.BeginInvoke(new Action(() => AnimateCarousel()), DispatcherPriority.Render);
                }
            };
        }


        if (ComboLang != null)
        {
            foreach (ComboBoxItem item in ComboLang.Items)
                if (item.Tag?.ToString() == cfg.Language) { item.IsSelected = true; break; }
        }
        if (SldStartupDelay != null) SldStartupDelay.Value = cfg?.StartupDelay ?? 0;
        _isLoaded = true;
    }

    // --- WIDGET TOGGLES ---
    private void ToggleDynamicWidget_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || sender is not CheckBox cb || cb.Tag == null) return;

        string widgetId = cb.Tag.ToString()!;
        Core.WidgetHost.ToggleWidget(widgetId);
    }
    private void AnimateCarousel()
    {
        if (CarouselSkins == null || CarouselSkins.Items.Count == 0) return;
        int selectedIndex = CarouselSkins.SelectedIndex;
        if (selectedIndex < 0) return;

        for (int i = 0; i < CarouselSkins.Items.Count; i++)
        {
            if (CarouselSkins.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem container)
            {
                int offset = i - selectedIndex;

                if (offset < -1) offset += 4;
                if (offset > 2) offset -= 4;

                double targetX = 0;
                double targetScale = 1.0;
                double targetOpacity = 1.0;
                int targetZIndex = 2;

                if (offset == 0)
                {
                    targetX = 0;
                    targetScale = 1.15;
                    targetOpacity = 1.0;
                    targetZIndex = 3;
                }
                else if (offset == 1)
                {
                    targetX = 145;
                    targetScale = 0.8;
                    targetOpacity = 0.45;
                    targetZIndex = 1;
                }
                else if (offset == -1)
                {
                    targetX = -145;
                    targetScale = 0.8;
                    targetOpacity = 0.45;
                    targetZIndex = 1;
                }
                else
                {
                    targetX = 0;
                    targetScale = 0.5;
                    targetOpacity = 0.0;
                    targetZIndex = 0;
                }

                System.Windows.Controls.Panel.SetZIndex(container, targetZIndex);

                if (container.RenderTransform is not TransformGroup group || group.Children.Count < 2)
                {
                    var g = new TransformGroup();
                    g.Children.Add(new TranslateTransform());
                    g.Children.Add(new ScaleTransform());
                    container.RenderTransform = g;
                    container.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                var tg = (TransformGroup)container.RenderTransform;
                var translate = (TranslateTransform)tg.Children[0];
                var scale = (ScaleTransform)tg.Children[1];

                var animX = new DoubleAnimation(targetX, TimeSpan.FromMilliseconds(450)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var animScaleX = new DoubleAnimation(targetScale, TimeSpan.FromMilliseconds(450)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var animScaleY = new DoubleAnimation(targetScale, TimeSpan.FromMilliseconds(450)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                var animOpacity = new DoubleAnimation(targetOpacity, TimeSpan.FromMilliseconds(450)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

                translate.BeginAnimation(TranslateTransform.XProperty, animX);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY);
                container.BeginAnimation(OpacityProperty, animOpacity);
            }
        }
    }
    private void ToggleHardwareWidget_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        // English: Call the unified non-generic method
        Core.WidgetHost.ToggleWidget("Hardware_01");
    }
    private void CarouselSkins_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        if (CarouselSkins.SelectedItem is SkinItemModel selectedSkin)
        {
            var cfg = Core.WidgetHost.CurrentConfig;
            if (cfg.Widgets.ContainsKey(_editingWidgetId))
            {
                cfg.Widgets[_editingWidgetId].SkinId = selectedSkin.Id;
                Core.ConfigManager.Save(cfg);
                Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
            }
        }
        AnimateCarousel();
    }

    // --- WIDGET CONFIGURATION ---
    private void BtnWidgetSettings_Click(object sender, RoutedEventArgs e)
    {
        if (BtnUninstallPlugin != null)
        {
            BtnUninstallPlugin.Visibility = (_editingWidgetId == "Quote_01" || _editingWidgetId == "Notes_01")
                ? Visibility.Visible : Visibility.Collapsed;
        }
        if (sender is Button btn && btn.Tag != null)
        {
            _isApplyingSettings = true;
            _editingWidgetId = btn.Tag.ToString()!;
            string localizedTitle = Application.Current.FindResource("Lang_Config_Title") as string ?? "Settings";
            ConfigTitle.Text = $"{localizedTitle}: {_editingWidgetId.Replace("_01", "")}";

            _isLoaded = false;
            var cfg = Core.WidgetHost.CurrentConfig;
            var state = cfg.Widgets.GetValueOrDefault(_editingWidgetId, new Core.WidgetState());
            if (CarouselSkins != null)
            {
                foreach (SkinItemModel item in CarouselSkins.Items)
                {
                    if (item.Id == state.SkinId) { CarouselSkins.SelectedItem = item; break; }
                }
            }
            // 1. АДАПТАЦИЯ ПАНЕЛЕЙ: Показываем нужные настройки
            if (HardwareSettingsPanel != null) HardwareSettingsPanel.Visibility = _editingWidgetId == "Hardware_01" ? Visibility.Visible : Visibility.Collapsed;
            if (TimeSettingsPanel != null) TimeSettingsPanel.Visibility = _editingWidgetId == "Time_01" ? Visibility.Visible : Visibility.Collapsed;
            if (AiSettingsPanel != null) AiSettingsPanel.Visibility = _editingWidgetId == "AI_01" ? Visibility.Visible : Visibility.Collapsed;
            if (DateSettingsPanel != null) DateSettingsPanel.Visibility = _editingWidgetId == "Date_01" ? Visibility.Visible : Visibility.Collapsed;
            if (WeatherSettingsPanel != null) WeatherSettingsPanel.Visibility = _editingWidgetId == "Weather_01" ? Visibility.Visible : Visibility.Collapsed;
            if (MediaSettingsPanel != null) MediaSettingsPanel.Visibility = _editingWidgetId == "Media_01" ? Visibility.Visible : Visibility.Collapsed;
            if (TglShowHardwareGraphs != null) TglShowHardwareGraphs.IsChecked = cfg.Widgets.GetValueOrDefault("Hardware_01")?.ShowHardwareGraphs ?? true;
            else if (_editingWidgetId == "Weather_01")
            {
                if (TxtWeatherCity != null) TxtWeatherCity.Text = state.WeatherCity;
            }
            if (_editingWidgetId == "Image_01") { BtnUploadImage.Visibility = Visibility.Visible; TypographySettingsPanel.Visibility = Visibility.Collapsed; }
            if (PluginSettingsContainer != null)
            {
                PluginSettingsContainer.Content = null;
                var widgetInfo = Core.WidgetHost.AvailableWidgets.FirstOrDefault(w => w.Id == _editingWidgetId);
                if (widgetInfo?.PluginInstance is GlanceCore.Plugins.IWidgetPlugin plugin)
                {
                    PluginSettingsContainer.Content = plugin.GetSettingsUI(state, () =>
                    {
                        Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
                    });
                }
            }

            // 2. ЗАГРУЗКА БАЗОВЫХ ЗНАЧЕНИЙ (Ползунки)
            else if (_editingWidgetId == "Date_01")
        {
            if (TglShowPlate != null) TglShowPlate.IsChecked = state.ShowPlate;
            if (TglShowDay != null) TglShowDay.IsChecked = state.ShowDayOfWeek;
            if (TglShowCalendarDate != null) TglShowCalendarDate.IsChecked = state.ShowDate;
            if (TglShowClockTime != null) TglShowClockTime.IsChecked = state.ShowTime;
        }
            
            _isUpdatingSize = true;
            if (SldWidth != null) SldWidth.Value = state.CustomWidth > 0 ? state.CustomWidth : 220;
            if (SldHeight != null) SldHeight.Value = state.CustomHeight > 0 ? state.CustomHeight : 280;
            if (TxtWidth != null) TxtWidth.Text = state.CustomWidth > 0 ? ((int)state.CustomWidth).ToString() : "220";
            if (TxtHeight != null) TxtHeight.Text = state.CustomHeight > 0 ? ((int)state.CustomHeight).ToString() : "280";
            _isUpdatingSize = true;
            if (TxtOpacity != null) TxtOpacity.Text = Math.Round(state.Opacity, 2).ToString();
            if (TxtScale != null) TxtScale.Text = Math.Round(state.Scale, 2).ToString();
            if (TxtRadius != null) TxtRadius.Text = state.CornerRadius.ToString();
            _isUpdatingSize = false;
            if (_editingWidgetId == "Image_01" || _editingWidgetId == "Date_01")
            {
                BtnUploadImage.Visibility = _editingWidgetId == "Image_01" ? Visibility.Visible : Visibility.Collapsed;
                TypographySettingsPanel.Visibility = _editingWidgetId == "Image_01" ? Visibility.Collapsed : Visibility.Visible;
                if (SldFontSize != null) SldFontSize.Visibility = Visibility.Collapsed;
                if (LblFontSize != null) LblFontSize.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnUploadImage.Visibility = Visibility.Collapsed;
                TypographySettingsPanel.Visibility = Visibility.Visible;
                if (SldFontSize != null) SldFontSize.Visibility = Visibility.Visible;
                if (LblFontSize != null) LblFontSize.Visibility = Visibility.Visible;
            }
            // Цвета
            try
            {
                var conv = new System.Windows.Media.BrushConverter();
                if (CircleTextColor != null) CircleTextColor.Fill = (System.Windows.Media.Brush)conv.ConvertFromString(state.TextColor)!;
                if (CircleBgColor != null) CircleBgColor.Fill = (System.Windows.Media.Brush)conv.ConvertFromString(state.BgColor)!;
            }
            catch { }

            // Шрифты
            if (ComboFont != null)
            {
                foreach (ComboBoxItem item in ComboFont.Items)
                    if (item.Content.ToString() == state.FontFamily) { item.IsSelected = true; break; }
            }
            else if (_editingWidgetId == "Date_01")
            {
                if (TglShowPlate != null) TglShowPlate.IsChecked = state.ShowPlate;
                if (TglShowDay != null) TglShowDay.IsChecked = state.ShowDayOfWeek;
                if (TglShowCalendarDate != null) TglShowCalendarDate.IsChecked = state.ShowDate;
                if (TglShowClockTime != null) TglShowClockTime.IsChecked = state.ShowTime;

                if (SldDateDayFontSize != null) SldDateDayFontSize.Value = state.DateDayFontSize;
                if (SldDateDateFontSize != null) SldDateDateFontSize.Value = state.DateDateFontSize;
                if (SldDateTimeFontSize != null) SldDateTimeFontSize.Value = state.DateTimeFontSize;
                try
                {
                    var conv = new System.Windows.Media.BrushConverter();
                    if (CircleDateDayColor != null && !string.IsNullOrEmpty(state.DateDayColor)) CircleDateDayColor.Fill = (Brush)conv.ConvertFromString(state.DateDayColor)!;
                    if (CircleDateDateColor != null && !string.IsNullOrEmpty(state.DateDateColor)) CircleDateDateColor.Fill = (Brush)conv.ConvertFromString(state.DateDateColor)!;
                    if (CircleDateTimeColor != null && !string.IsNullOrEmpty(state.DateTimeColor)) CircleDateTimeColor.Fill = (Brush)conv.ConvertFromString(state.DateTimeColor)!;
                }
                catch { }
            }

            // 3. ЗАГРУЗКА СПЕЦИФИЧНЫХ НАСТРОЕК (Hardware / Time)
            if (_editingWidgetId == "Hardware_01" && ListHardwareOrder != null)
            {
                ListHardwareOrder.Items.Clear();
                foreach (var item in state.HardwareOrder) ListHardwareOrder.Items.Add(item);
            }
            else if (_editingWidgetId == "Time_01")
            {
                if (TglShowSeconds != null) TglShowSeconds.IsChecked = state.ShowSeconds;
                if (TglVerticalTime != null) TglVerticalTime.IsChecked = state.IsVerticalTime;

                if (ComboTimeSeparator != null)
                {
                    foreach (ComboBoxItem item in ComboTimeSeparator.Items)
                    {
                        string targetSep = state.TimeSeparator == " " ? "Пробел" : state.TimeSeparator;
                        if (item.Content.ToString() == targetSep) { item.IsSelected = true; break; }
                    }
                }
            }
            else if (_editingWidgetId == "Media_01")
            {
                if (TglShowMediaTimer != null) TglShowMediaTimer.IsChecked = state.ShowMediaTimer;
            }
            _isLoaded = true;
            WidgetsPanel.Visibility = Visibility.Collapsed;
            WidgetConfigPanel.Visibility = Visibility.Visible;
            _isApplyingSettings = false;
        }
    }
    private void WeatherSettings_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            cfg.Widgets[_editingWidgetId].WeatherCity = TxtWeatherCity.Text;
            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetCustomData(_editingWidgetId);
        }
    }
    private void MediaSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            cfg.Widgets[_editingWidgetId].ShowMediaTimer = TglShowMediaTimer.IsChecked == true;
            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
        }
    }
    private void BtnBackToWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetConfigPanel.Visibility = Visibility.Collapsed;
        WidgetsPanel.Visibility = Visibility.Visible;
        if (SearchBoxBorder != null) SearchBoxBorder.Visibility = Visibility.Visible;
    }
    private void HardwareSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            var state = cfg.Widgets[_editingWidgetId];
            if (TglShowHardwareGraphs != null) state.ShowHardwareGraphs = TglShowHardwareGraphs.IsChecked == true;
            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
        }
    }
    private void DateSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            var state = cfg.Widgets[_editingWidgetId];
            state.ShowPlate = TglShowPlate.IsChecked == true;
            state.ShowDayOfWeek = TglShowDay.IsChecked == true;
            state.ShowDate = TglShowCalendarDate.IsChecked == true;
            state.ShowTime = TglShowClockTime.IsChecked == true;

            if (SldDateDayFontSize != null) state.DateDayFontSize = SldDateDayFontSize.Value;
            if (SldDateDateFontSize != null) state.DateDateFontSize = SldDateDateFontSize.Value;
            if (SldDateTimeFontSize != null) state.DateTimeFontSize = SldDateTimeFontSize.Value;

            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
        }
    }

    // --- МЕТОДЫ СОРТИРОВКИ ДАТЧИКОВ HARDWARE ---
    private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (ListHardwareOrder != null && ListHardwareOrder.SelectedIndex > 0)
        {
            int idx = ListHardwareOrder.SelectedIndex;
            var item = ListHardwareOrder.Items[idx];
            ListHardwareOrder.Items.RemoveAt(idx);
            ListHardwareOrder.Items.Insert(idx - 1, item);
            ListHardwareOrder.SelectedIndex = idx - 1;
            SaveHardwareOrder();
        }
    }

    private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (ListHardwareOrder != null && ListHardwareOrder.SelectedIndex >= 0 && ListHardwareOrder.SelectedIndex < ListHardwareOrder.Items.Count - 1)
        {
            int idx = ListHardwareOrder.SelectedIndex;
            var item = ListHardwareOrder.Items[idx];
            ListHardwareOrder.Items.RemoveAt(idx);
            ListHardwareOrder.Items.Insert(idx + 1, item);
            ListHardwareOrder.SelectedIndex = idx + 1;
            SaveHardwareOrder();
        }
    }
    private void ComboLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;
        if (ComboLang.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            string lang = item.Tag.ToString()!;
            var cfg = Core.WidgetHost.CurrentConfig;
            cfg.Language = lang;
            Core.ConfigManager.Save(cfg);
            App.ChangeLanguage(lang);
        }
    }
    private void TxtSearchWidgets_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentSearchQuery = TxtSearchWidgets.Text.ToLower();
        _currentPage = 1;
        UpdatePagination();
    }
    private void SaveHardwareOrder()
    {
        if (ListHardwareOrder == null) return;

        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            cfg.Widgets[_editingWidgetId].HardwareOrder = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Cast<string>(ListHardwareOrder.Items));
            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
        }
    }

    private void BtnUploadImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp",
            Title = "Выберите фото для виджета"
        };

        if (dlg.ShowDialog() == true)
        {
            var cfg = Core.WidgetHost.CurrentConfig;
            if (!cfg.Widgets.ContainsKey("Image_01")) cfg.Widgets["Image_01"] = new Core.WidgetState();

            cfg.Widgets["Image_01"].CustomData = dlg.FileName;
            Core.ConfigManager.Save(cfg);

            Core.WidgetHost.RefreshWidgetCustomData("Image_01");
        }
    }

    // --- COLOR PICKING ---
    private void BtnPickColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string target)
        {
            var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string hex = $"#FF{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";

                var cfg = Core.WidgetHost.CurrentConfig;
                // Защита от Null Reference (CS8602)
                if (cfg == null || cfg.Widgets == null || !cfg.Widgets.ContainsKey(_editingWidgetId)) return;

                var state = cfg.Widgets[_editingWidgetId];
                if (state == null) return;

                var converter = new System.Windows.Media.BrushConverter();
                var brush = converter.ConvertFromString(hex) as System.Windows.Media.Brush;
                if (brush == null) return;

                if (target == "Text")
                {
                    state.TextColor = hex;
                    if (CircleTextColor != null) CircleTextColor.Fill = brush;
                }
                else if (target == "Bg")
                {
                    state.BgColor = hex;
                    if (CircleBgColor != null) CircleBgColor.Fill = brush;
                }
                else if (target == "DateDay")
                {
                    state.DateDayColor = hex;
                    if (CircleDateDayColor != null) CircleDateDayColor.Fill = brush;
                }
                else if (target == "Border")
                {
                    state.BorderColor = hex;
                    if (CircleBorderColor != null) CircleBorderColor.Fill = brush;
                }
                else if (target == "DateDate")
                {
                    state.DateDateColor = hex;
                    if (CircleDateDateColor != null) CircleDateDateColor.Fill = brush;
                }
                else if (target == "DateTime")
                {
                    state.DateTimeColor = hex;
                    if (CircleDateTimeColor != null) CircleDateTimeColor.Fill = brush;
                }
                else if (target == "Bg")
                {
                    state.BgColor = hex;
                    if (CircleBgColor != null) CircleBgColor.Fill = brush;
                }

                Core.ConfigManager.Save(cfg);
                ApplyCurrentWidgetSettings();
            }
        }
    }
    private void TglShowBorder_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey(_editingWidgetId))
        {
            Core.ConfigManager.Save(cfg);
            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
        }
    }
    private void SetButtonEllipseColor(Button btn, string hex)
    {
        try
        {
            btn.ApplyTemplate();
            if (btn.Template.FindName("ColorCircle", btn) is System.Windows.Shapes.Ellipse circle)
                circle.Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!;
        }
        catch { }
    }

    // --- REAL-TIME SETTINGS APPLY ---
    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isUpdatingSize || _isApplyingSettings) return;

        _isUpdatingSize = true;
        if (TxtWidth != null && SldWidth != null) TxtWidth.Text = ((int)SldWidth.Value).ToString();
        if (TxtHeight != null && SldHeight != null) TxtHeight.Text = ((int)SldHeight.Value).ToString();
        if (TxtOpacity != null && SldOpacity != null) TxtOpacity.Text = Math.Round(SldOpacity.Value, 2).ToString();
        if (TxtScale != null && SldScale != null) TxtScale.Text = Math.Round(SldScale.Value, 2).ToString();
        if (TxtRadius != null && SldRadius != null) TxtRadius.Text = ((int)SldRadius.Value).ToString();
        _isUpdatingSize = false;

        ApplyCurrentWidgetSettings();
    }

    private void Topmost_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isApplyingSettings) return;
        ApplyCurrentWidgetSettings();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isApplyingSettings) return;
        ApplyCurrentWidgetSettings();
    }

    private void ApplyCurrentWidgetSettings()
    {
        var cfg = Core.WidgetHost.CurrentConfig;
        if (!cfg.Widgets.ContainsKey(_editingWidgetId)) return;
        var state = cfg.Widgets[_editingWidgetId];

        if (SldOpacity != null) state.Opacity = SldOpacity.Value;
        if (SldScale != null) state.Scale = SldScale.Value;
        if (TglWidgetTopmost != null) state.IsAlwaysOnTop = TglWidgetTopmost.IsChecked == true;
        if (SldFontSize != null) state.FontSize = SldFontSize.Value;
        if (SldRadius != null) state.CornerRadius = SldRadius.Value;
        if (TxtAiKey != null) TxtAiKey.Text = cfg.AiApiKey;
        if (TxtAiEndpoint != null) TxtAiEndpoint.Text = cfg.AiEndpoint;
        if (SldWidth != null) state.CustomWidth = SldWidth.Value;
        if (SldHeight != null) state.CustomHeight = SldHeight.Value;
        if (TglShowBorder != null) state.ShowBorder = TglShowBorder.IsChecked == true;

        if (ComboFont != null)
        {
            state.FontFamily = (ComboFont.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Segoe UI Variable";
        }

        Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
    }

    private void SystemSettings_Changed(object sender, RoutedEventArgs e)
    {

        if (!_isLoaded || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        cfg.RunAtStartup = TglAutoStart.IsChecked == true;
        cfg.LockWidgets = TglLock.IsChecked == true;
        cfg.StartupDelay = (int)SldStartupDelay.Value;
        cfg.EnableShader = TglShader.IsChecked == true;
        cfg.StreamerMode = TglStreamerMode.IsChecked == true;
        cfg.GameMode = TglGameMode.IsChecked == true;
        cfg.IdleFps = (int)SldIdleFps.Value;
        cfg.MovingFps = (int)SldMovingFps.Value;
        Core.AutoStartManager.SetAutoStart(cfg.RunAtStartup);
        Core.WidgetHost.ApplySystemSettings();
    }

    private void AiSettings_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (TxtAiEndpoint != null) cfg.AiEndpoint = TxtAiEndpoint.Text;
        if (TxtAiKey != null) cfg.AiApiKey = TxtAiKey.Text;
        if (TxtAiModel != null) cfg.AiModel = TxtAiModel.Text;
        if (TxtAiPrompt != null) cfg.AiSystemPrompt = TxtAiPrompt.Text;
        Core.ConfigManager.Save(cfg);
    }

    private void AiSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isUpdatingSize || _isApplyingSettings) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (SldAiTemp != null) cfg.AiTemperature = SldAiTemp.Value;
        Core.ConfigManager.Save(cfg);
    }

    // --- NAVIGATION AND WINDOW CONTROLS ---
    private void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(btn.Tag.ToString()!) { UseShellExecute = true }); }
            catch { }
        }
    }
    private void SizeInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId) || _isUpdatingSize || _isApplyingSettings) return;

        _isUpdatingSize = true;

        double ParseInput(string text, double fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            string normalized = text.Replace(",", ".");
            if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;
            return fallback;
        }

        if (SldWidth != null) SldWidth.Value = ParseInput(TxtWidth.Text, SldWidth.Value);
        if (SldHeight != null) SldHeight.Value = ParseInput(TxtHeight.Text, SldHeight.Value);
        if (SldOpacity != null) SldOpacity.Value = ParseInput(TxtOpacity.Text, SldOpacity.Value);
        if (SldScale != null) SldScale.Value = ParseInput(TxtScale.Text, SldScale.Value);
        if (SldRadius != null) SldRadius.Value = ParseInput(TxtRadius.Text, SldRadius.Value);

        _isUpdatingSize = false;

        ApplyCurrentWidgetSettings();
    }

    private int _currentPage = 1;
    private const int _itemsPerPage = 10;
    private string _currentSearchQuery = "";

    private void UpdatePagination()
    {
        if (WidgetsList == null || TxtPageInfo == null) return;
        var source = Core.WidgetHost.AvailableWidgets.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_currentSearchQuery))
            source = source.Where(w => w.Title.ToLower().Contains(_currentSearchQuery) || w.Description.ToLower().Contains(_currentSearchQuery));

        var list = source.ToList();
        int totalPages = (int)Math.Ceiling(list.Count / (double)_itemsPerPage);
        if (totalPages == 0) totalPages = 1;
        if (_currentPage > totalPages) _currentPage = totalPages;
        if (_currentPage < 1) _currentPage = 1;

        TxtPageInfo.Text = $"{_currentPage} / {totalPages}";
        WidgetsList.ItemsSource = list.Skip((_currentPage - 1) * _itemsPerPage).Take(_itemsPerPage).ToList();
    }

    private void BtnImportFont_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Font Files (*.ttf;*.otf)|*.ttf;*.otf",
            Title = "Выберите шрифт для импорта"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                string fontsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");
                if (!System.IO.Directory.Exists(fontsDir)) System.IO.Directory.CreateDirectory(fontsDir);

                string destPath = System.IO.Path.Combine(fontsDir, System.IO.Path.GetFileName(dlg.FileName));
                System.IO.File.Copy(dlg.FileName, destPath, true);

                var glyphTypeface = new System.Windows.Media.GlyphTypeface(new Uri(destPath));
                var culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

                if (glyphTypeface.FamilyNames != null && glyphTypeface.FamilyNames.TryGetValue(culture, out string? fontName) && fontName != null)
                {
                    var cfg = Core.WidgetHost.CurrentConfig;
                    if (cfg != null && cfg.Widgets != null && cfg.Widgets.ContainsKey(_editingWidgetId))
                    {
                        var state = cfg.Widgets[_editingWidgetId];
                        if (state != null)
                        {
                            state.FontFamily = $"./Fonts/#{fontName}";
                            Core.ConfigManager.Save(cfg);
                            Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
                        }
                    }
                }
            }
            catch { }
        }
    }
    private async void LoadStoreCatalogAsync()
    {
        if (StoreList == null) return;
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");

        try
        {
            List<StoreItemModel>? catalog = null;
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "GlanceCore/1.0");
                string url = "https://raw.githubusercontent.com/MazeFS/GlanceCore/main/store_test.json";
                var json = await http.GetStringAsync(url);
                catalog = JsonSerializer.Deserialize<List<StoreItemModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                File.AppendAllText(logPath, $"{DateTime.Now}: Downloaded catalog. Count: {catalog?.Count ?? 0}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"{DateTime.Now}: HTTP/JSON ERROR: {ex.Message}\nInner: {ex.InnerException?.Message}\n");
            }

            if (catalog == null)
            {
                catalog = new List<StoreItemModel>
                {
                    new StoreItemModel { Id = "Quote_01", Title = "Цитата дня", Description = "Вдохновение на стекле", PreviewImage = "/Resource/Skins/Skin_Retro.png", DownloadUrl = "https://github.com/MazeFS/GlanceCore/releases/download/v0.5.0/QuotePlugin.dll", Author = "MazeFS", Rating = 4.8, Category = "Essentials" },
                    new StoreItemModel { Id = "Notes_01", Title = "Стикеры", Description = "Заметки на стекле", PreviewImage = "/Resource/Skins/Skin_Minimalism.png", DownloadUrl = "", Author = "MazeFS", Rating = 4.5, Category = "Essentials" },
                    new StoreItemModel { Id = "Calculator_01", Title = "Калькулятор", Description = "Стеклянный калькулятор", PreviewImage = "/Resource/Skins/Skin_Neon.png", DownloadUrl = "", Author = "GlanceCommunity", Rating = 4.2, Category = "Premium" },
                    new StoreItemModel { Id = "NeonSkin_01", Title = "Neon Glow Skin", Description = "Неоновое оформление", PreviewImage = "/Resource/Skins/Skin_Neon.png", DownloadUrl = "", Author = "GlanceCore", Rating = 4.9, Category = "Skins" }
                };
                File.AppendAllText(logPath, $"{DateTime.Now}: Loaded fallback offline catalog. Count: {catalog.Count}\n");
            }

            Dispatcher.Invoke(() =>
            {
                _fullStoreCatalog = catalog;
                foreach (var item in _fullStoreCatalog)
                {
                    if (string.IsNullOrEmpty(item.Category)) item.Category = "Essentials";
                    item.IsDownloaded = Core.WidgetHost.AvailableWidgets.Any(w => w.Id == item.Id);
                }
                FilterStore();
                File.AppendAllText(logPath, $"{DateTime.Now}: Successfully filtered and applied ItemsSource.\n\n");
            });
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(logPath, $"{DateTime.Now}: FATAL GLOBAL STORE ERROR: {ex.Message}\nStack: {ex.StackTrace}\n\n");
            }
            catch { }
        }
    }

    private void FilterStore()
    {
        if (StoreList == null) return;

        var filtered = _fullStoreCatalog.AsEnumerable();

        if (!string.IsNullOrEmpty(_activeStoreCategory))
        {
            filtered = filtered.Where(item => item.Category == _activeStoreCategory);
        }

        if (!string.IsNullOrEmpty(_activeStoreSearch))
        {
            filtered = filtered.Where(item => item.Title.ToLower().Contains(_activeStoreSearch) || item.Description.ToLower().Contains(_activeStoreSearch));
        }

        StoreList.ItemsSource = filtered.ToList();
    }

    private void TabStoreCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            _activeStoreCategory = btn.Tag.ToString()!;

            TabStoreSkins.Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
            TabStorePremium.Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
            TabStoreEssentials.Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));

            btn.Foreground = Brushes.MediumPurple;

            FilterStore();
        }
    }

    private void TxtSearchStore_TextChanged(object sender, TextChangedEventArgs e)
    {
        _activeStoreSearch = TxtSearchStore.Text.ToLower();
        FilterStore();
    }
    private void BtnStoreInfo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is StoreItemModel item)
        {
            StoreDetailsGrid.DataContext = item;
            StoreMainGrid.Visibility = Visibility.Collapsed;
            StoreDetailsGrid.Visibility = Visibility.Visible;
        }
    }

    private void BtnStoreBack_Click(object sender, RoutedEventArgs e)
    {
        StoreMainGrid.Visibility = Visibility.Visible;
        StoreDetailsGrid.Visibility = Visibility.Collapsed;
    }
    private void BtnStoreFilter_Click(object sender, RoutedEventArgs e)
    {
    }


    private void FilterStore_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            string filter = btn.Tag.ToString()!;
        }
    }

    private async void BtnInstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is StoreItemModel item)
        {
            if (item.IsDownloaded) return;

            string url = item.DownloadUrl;
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                using var http = new HttpClient();
                var data = await http.GetByteArrayAsync(url);
                string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                if (!Directory.Exists(pluginsPath)) Directory.CreateDirectory(pluginsPath);

                string fileName = Path.GetFileName(url);
                string destPath = Path.Combine(pluginsPath, fileName);
                await File.WriteAllBytesAsync(destPath, data);

                var loaded = GlanceCore.Plugins.PluginManager.LoadPlugins();
                foreach (var p in loaded)
                {
                    if (!WidgetHost.AvailableWidgets.Any(w => w.Id == p.Id))
                    {
                        WidgetHost.AvailableWidgets.Add(p);
                    }
                }
                item.IsDownloaded = true;
            }
            catch { }
        }
    }

    private void BtnUninstallPlugin_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_editingWidgetId)) return;
        var widgetInfo = Core.WidgetHost.AvailableWidgets.FirstOrDefault(w => w.Id == _editingWidgetId);
        if (widgetInfo != null)
        {
            Core.WidgetHost.CloseWidgetExplicitly(_editingWidgetId);
            try
            {
                string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
                string dllName = $"{_editingWidgetId.Replace("_01", "")}Plugin.dll";
                string dllPath = Path.Combine(pluginsPath, dllName);
                if (File.Exists(dllPath))
                {
                    File.Move(dllPath, dllPath + ".deleted", true);
                }
            }
            catch { }

            Core.WidgetHost.AvailableWidgets.Remove(widgetInfo);
            var cfg = Core.WidgetHost.CurrentConfig;
            if (cfg.Widgets.ContainsKey(_editingWidgetId))
            {
                cfg.Widgets.Remove(_editingWidgetId);
                Core.ConfigManager.Save(cfg);
            }

            WidgetsPanel.Visibility = Visibility.Visible;
            WidgetConfigPanel.Visibility = Visibility.Collapsed;
        }
    }
    private void BtnPrevPage_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; UpdatePagination(); } }
    private void BtnNextPage_Click(object sender, RoutedEventArgs e) { _currentPage++; UpdatePagination(); }
    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;

        TabWidgets.IsChecked = false;
        TabStore.IsChecked = false;
        TabSettings.IsChecked = false;

        DoubleAnimation fadeOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.12));
        fadeOut.Completed += (s, ev) =>
        {
            if (WidgetsPanel != null) WidgetsPanel.Visibility = Visibility.Collapsed;
            if (StorePanel != null) StorePanel.Visibility = Visibility.Collapsed;
            if (SettingsPanel != null) SettingsPanel.Visibility = Visibility.Collapsed;
            if (WidgetConfigPanel != null) WidgetConfigPanel.Visibility = Visibility.Collapsed;

            TabTitleText.Text = "О программе (About)";
            AboutPanel.Visibility = Visibility.Visible;

            ActiveTabContent.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.2)));
        };
        ActiveTabContent.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || ActiveTabContent == null || sender is not RadioButton rb) return;

        if (PillTransform != null && MenuStack != null)
        {
            var transform = rb.TransformToAncestor(MenuStack);
            var point = transform.Transform(new Point(0, 0));
            PillTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation { To = point.Y, Duration = TimeSpan.FromSeconds(0.4), EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut } });
        }

        DoubleAnimation fadeOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.12));
        fadeOut.Completed += (s, ev) =>
        {
            if (WidgetsPanel != null) WidgetsPanel.Visibility = Visibility.Collapsed;
            if (StorePanel != null) StorePanel.Visibility = Visibility.Collapsed;
            if (SettingsPanel != null) SettingsPanel.Visibility = Visibility.Collapsed;
            if (WidgetConfigPanel != null) WidgetConfigPanel.Visibility = Visibility.Collapsed;
            if (AboutPanel != null) AboutPanel.Visibility = Visibility.Collapsed;

            string tab = rb.Name;
            if (tab == "TabWidgets")
            {
                TabTitleText.Text = Application.Current.FindResource("Lang_Title_Widgets") as string ?? "Widget Management";
                WidgetsPanel.Visibility = Visibility.Visible;
                if (SearchBoxBorder != null) SearchBoxBorder.Visibility = Visibility.Visible;
            }
            else if (tab == "TabStore")
            {
                TabTitleText.Text = Application.Current.FindResource("Lang_Title_Store") as string ?? "Plugin Store";
                StorePanel.Visibility = Visibility.Visible;
                if (SearchBoxBorder != null) SearchBoxBorder.Visibility = Visibility.Collapsed;
            }
            else if (tab == "TabSettings")
            {
                TabTitleText.Text = Application.Current.FindResource("Lang_Title_Settings") as string ?? "System Settings";
                SettingsPanel.Visibility = Visibility.Visible;
                if (SearchBoxBorder != null) SearchBoxBorder.Visibility = Visibility.Collapsed;
            }
            else if (tab == "TabStore")
            {
                TabTitleText.Text = Application.Current.FindResource("Lang_Title_Store") as string ?? "Plugin Store";
                StorePanel.Visibility = Visibility.Visible;
                if (SearchBoxBorder != null) SearchBoxBorder.Visibility = Visibility.Collapsed;
                LoadStoreCatalogAsync(); // Запуск загрузки!
            }

            ActiveTabContent.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.2)));
            var zoom = new DoubleAnimation(0.96, 1, TimeSpan.FromSeconds(0.3)) { EasingFunction = new BackEase { Amplitude = 0.3 } };
            ContentScale.BeginAnimation(ScaleTransform.ScaleXProperty, zoom);
            ContentScale.BeginAnimation(ScaleTransform.ScaleYProperty, zoom);
        };
        ActiveTabContent.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        if (Core.WidgetHost.CurrentConfig.Widgets.Values.All(w => !w.IsEnabled))
        {
            System.Windows.Application.Current.Shutdown();
        }
        else
        {
            this.Hide();
            Core.MemoryOptimizer.Trim();
        }
    }

    private void BtnMenuToggle_Click(object sender, RoutedEventArgs e) => ToggleSidebar();
    private void ToggleSidebar()
    {
        bool isCollapsed = BtnMenuToggle.IsChecked == true;
        Sidebar.BeginAnimation(WidthProperty, new DoubleAnimation { To = isCollapsed ? 0 : 220, Duration = TimeSpan.FromSeconds(0.4), EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
    }

    // Stub for unused event from old XAML to prevent compilation failure
    private void Style_Changed(object sender, SelectionChangedEventArgs e) { }

    public void CarouselThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CarouselThemes == null) return;
        if (CarouselThemes.SelectedItem is HubThemeModel selectedTheme)
        {
            if (selectedTheme == null) return;
            string theme = selectedTheme.Id;
            var cfg = Core.WidgetHost.CurrentConfig;
            if (cfg != null)
            {
                cfg.HubTheme = theme;
                Core.ConfigManager.Save(cfg);
                App.ChangeTheme(theme);
            }
        }
    }



    public System.Collections.ObjectModel.ObservableCollection<SkinItemModel> AvailableSkins { get; } = new()
    {
        new SkinItemModel { Id = "LiquidGlass", Name = "Liquid Glass", Image = "/Resource/Skins/Skin_LiquidGlass.png", Color = "#00BFFF" },
        new SkinItemModel { Id = "Minimalism", Name = "Минимализм", Image = "/Resource/Skins/Skin_Minimalism.png", Color = "#A0FFFFFF" },
        new SkinItemModel { Id = "Retro", Name = "Ретро 8-bit", Image = "/Resource/Skins/Skin_Retro.png", Color = "#FF8C00" },
        new SkinItemModel { Id = "Neon", Name = "Кибер-Неон", Image = "/Resource/Skins/Skin_Neon.png", Color = "#FF00FF" }
    };
    public System.Collections.ObjectModel.ObservableCollection<HubThemeModel> AvailableHubThemes { get; } = new()
    {
        new HubThemeModel { Id = "Original", Name = "Оригинал" },
        new HubThemeModel { Id = "Dark", Name = "Темная" },
        new HubThemeModel { Id = "Blue", Name = "Синяя" },
        new HubThemeModel { Id = "Light", Name = "Светлая" }
    };
}

public class HubThemeModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string DisplayName => System.Windows.Application.Current.FindResource($"Lang_Theme_{Id}") as string ?? Name;
}


public class StyleItemModel
{
    public string Name { get; set; } = "";
    public string PreviewImage { get; set; } = "";
}
