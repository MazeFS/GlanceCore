namespace GlanceCore.Views;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using GlanceCore.Core;
using System.Collections.Generic;

public partial class HubWindow : Window
{
    private bool _isLoaded = false;
    private string _editingWidgetId = "";
    private void TimeSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
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
    public HubWindow()
    {
        InitializeComponent();
        _isLoaded = false;

        // English: Bind the auto-generator to our restored registry
        if (WidgetsList != null)
        {
            WidgetsList.ItemsSource = Core.WidgetHost.AvailableWidgets;
        }

        // English: Sync standard toggles with saved configuration on startup
        // Замени строку "HardwareToggle.IsChecked = ..." на этот безопасный поиск:
        var hardwareToggle = this.FindName("HardwareToggle") as CheckBox;
        if (hardwareToggle != null)
        {
            hardwareToggle.IsChecked = WidgetHost.IsWidgetActive("Hardware_01");
        }

        var cfg = WidgetHost.CurrentConfig;
        if (TglAutoStart != null) TglAutoStart.IsChecked = cfg.RunAtStartup;
        if (TglLock != null) TglLock.IsChecked = cfg.LockWidgets;
        if (TglShader != null) TglShader.IsChecked = cfg.EnableShader;

        _isLoaded = true;
    }

    // --- WIDGET TOGGLES ---
    private void ToggleDynamicWidget_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || sender is not CheckBox cb || cb.Tag == null) return;

        string widgetId = cb.Tag.ToString()!;
        Core.WidgetHost.ToggleWidget(widgetId);
    }

    private void ToggleHardwareWidget_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        // English: Call the unified non-generic method
        Core.WidgetHost.ToggleWidget("Hardware_01");
    }

    // --- WIDGET CONFIGURATION ---
    private void BtnWidgetSettings_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            _editingWidgetId = btn.Tag.ToString()!;
            ConfigTitle.Text = $"Настройки: {_editingWidgetId.Replace("_01", "")}";

            _isLoaded = false;
            var cfg = Core.WidgetHost.CurrentConfig;
            var state = cfg.Widgets.GetValueOrDefault(_editingWidgetId, new Core.WidgetState());

            // 1. АДАПТАЦИЯ ПАНЕЛЕЙ: Показываем нужные настройки
            if (HardwareSettingsPanel != null) HardwareSettingsPanel.Visibility = _editingWidgetId == "Hardware_01" ? Visibility.Visible : Visibility.Collapsed;

            // ВОТ ЭТА СТРОКА ВКЛЮЧАЕТ НАСТРОЙКИ ЧАСОВ:
            if (TimeSettingsPanel != null) TimeSettingsPanel.Visibility = _editingWidgetId == "Time_01" ? Visibility.Visible : Visibility.Collapsed;

            if (_editingWidgetId == "Image_01") { BtnUploadImage.Visibility = Visibility.Visible; TypographySettingsPanel.Visibility = Visibility.Collapsed; }
            else { BtnUploadImage.Visibility = Visibility.Collapsed; TypographySettingsPanel.Visibility = Visibility.Visible; }

            // 2. ЗАГРУЗКА БАЗОВЫХ ЗНАЧЕНИЙ (Ползунки)
            if (SldOpacity != null) SldOpacity.Value = state.Opacity;
            if (SldScale != null) SldScale.Value = state.Scale;
            if (SldRadius != null) SldRadius.Value = state.CornerRadius;
            if (SldWidth != null) SldWidth.Value = state.CustomWidth > 0 ? state.CustomWidth : 220;
            if (SldHeight != null) SldHeight.Value = state.CustomHeight > 0 ? state.CustomHeight : 280;
            if (TglWidgetTopmost != null) TglWidgetTopmost.IsChecked = state.IsAlwaysOnTop;
            if (SldFontSize != null) SldFontSize.Value = state.FontSize;

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

            _isLoaded = true;
            WidgetsPanel.Visibility = Visibility.Collapsed;
            WidgetConfigPanel.Visibility = Visibility.Visible;
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

    private void BtnBackToWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetConfigPanel.Visibility = Visibility.Collapsed;
        WidgetsPanel.Visibility = Visibility.Visible;
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

                Core.ConfigManager.Save(cfg);
                ApplyCurrentWidgetSettings();
            }
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
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        ApplyCurrentWidgetSettings();
    }

    private void Topmost_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        ApplyCurrentWidgetSettings();
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        ApplyCurrentWidgetSettings();
    }

    private void ApplyCurrentWidgetSettings()
    {
        var cfg = Core.WidgetHost.CurrentConfig;
        if (!cfg.Widgets.ContainsKey(_editingWidgetId)) return;
        var state = cfg.Widgets[_editingWidgetId];
        if (SldRadius != null) state.CornerRadius = SldRadius.Value;
        if (SldWidth != null) state.CustomWidth = SldWidth.Value;
        if (SldHeight != null) state.CustomHeight = SldHeight.Value;
        if (SldOpacity != null) state.Opacity = SldOpacity.Value;
        if (SldScale != null) state.Scale = SldScale.Value;
        if (TglWidgetTopmost != null) state.IsAlwaysOnTop = TglWidgetTopmost.IsChecked == true;
        if (SldFontSize != null) state.FontSize = SldFontSize.Value;

        if (ComboFont != null)
        {
            state.FontFamily = (ComboFont.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Segoe UI Variable";
        }

        Core.WidgetHost.RefreshWidgetVisuals(_editingWidgetId);
    }

    private void SystemSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        var cfg = Core.WidgetHost.CurrentConfig;

        cfg.RunAtStartup = TglAutoStart.IsChecked == true;
        cfg.LockWidgets = TglLock.IsChecked == true;
        cfg.EnableShader = TglShader.IsChecked == true;

        Core.AutoStartManager.SetAutoStart(cfg.RunAtStartup);
        Core.WidgetHost.ApplySystemSettings();
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
            if (tab == "TabWidgets") { TabTitleText.Text = "Управление виджетами"; WidgetsPanel.Visibility = Visibility.Visible; }
            else if (tab == "TabStore") { TabTitleText.Text = "Магазин плагинов"; StorePanel.Visibility = Visibility.Visible; }
            else if (tab == "TabSettings") { TabTitleText.Text = "Настройки системы"; SettingsPanel.Visibility = Visibility.Visible; }


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

}

public class StyleItemModel
{
    public string Name { get; set; } = "";
    public string PreviewImage { get; set; } = "";
}