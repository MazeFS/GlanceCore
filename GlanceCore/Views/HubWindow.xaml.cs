namespace GlanceCore.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using GlanceCore.Core;

public partial class HubWindow : Window
{
    // English: Open links in default browser (Socials, Boosty, BuyMeACoffee)
    private void OpenLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(btn.Tag.ToString()!) { UseShellExecute = true }); }
            catch { }
        }
    }

    // English: Show About Section
    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;

        // Deselect standard tabs
        TabWidgets.IsChecked = false;
        TabStore.IsChecked = false;
        TabSettings.IsChecked = false;

        DoubleAnimation fadeOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.12));
        fadeOut.Completed += (s, ev) =>
        {
            WidgetsPanel.Visibility = Visibility.Collapsed;
            StorePanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            WidgetConfigPanel.Visibility = Visibility.Collapsed;

            TabTitleText.Text = "О программе (About)";
            AboutPanel.Visibility = Visibility.Visible;

            ActiveTabContent.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.2)));
        };
        ActiveTabContent.BeginAnimation(OpacityProperty, fadeOut);
    }

    // English: Update Tab_Checked to handle the About panel cleanup
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
            WidgetsPanel.Visibility = Visibility.Collapsed;
            StorePanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            WidgetConfigPanel.Visibility = Visibility.Collapsed;
            if (AboutPanel != null) AboutPanel.Visibility = Visibility.Collapsed; // Hide About panel

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
    private bool _isLoaded = false;
    private string _editingWidgetId = "";

    public HubWindow()
    {
        InitializeComponent();

        // English: Bind the auto-generator to our registry
        WidgetsList.ItemsSource = Core.WidgetHost.AvailableWidgets;

        var cfg = Core.WidgetHost.CurrentConfig;
        TglAutoStart.IsChecked = cfg.RunAtStartup;
        TglLock.IsChecked = cfg.LockWidgets;
        TglShader.IsChecked = cfg.DisableShaderForScreenshots;

        _isLoaded = true;
    }

    // English: Universal toggle handler for all widgets
    private void ToggleDynamicWidget_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || sender is not CheckBox cb || cb.Tag == null) return;

        string widgetId = cb.Tag.ToString()!;
        Core.WidgetHost.ToggleWidget(widgetId);
    }

    private void BtnWidgetSettings_Click(object sender, RoutedEventArgs e)
    {

        if (sender is Button btn && btn.Tag != null)
        {
            _editingWidgetId = btn.Tag.ToString()!;
            ConfigTitle.Text = $"Настройки: {_editingWidgetId.Replace("_01", "")}";

            _isLoaded = false;
            var state = Core.WidgetHost.CurrentConfig.Widgets.GetValueOrDefault(_editingWidgetId, new Core.WidgetState());

            SldOpacity.Value = state.Opacity;
            SldScale.Value = state.Scale;
            SldFontSize.Value = state.FontSize;
            foreach (ComboBoxItem item in ComboFont.Items)
                if (item.Content.ToString() == state.FontFamily) item.IsSelected = true;
            _isLoaded = true;
            // English: Accessing the newly named checkbox
            if (TglWidgetTopmost != null)
                TglWidgetTopmost.IsChecked = state.IsAlwaysOnTop;

            _isLoaded = true;

            WidgetsPanel.Visibility = Visibility.Collapsed;
            WidgetConfigPanel.Visibility = Visibility.Visible;
        }
    }


    private void BtnBackToWidgets_Click(object sender, RoutedEventArgs e)
    {
        WidgetConfigPanel.Visibility = Visibility.Collapsed;
        WidgetsPanel.Visibility = Visibility.Visible;
    }

    // --- REAL-TIME SLIDERS ---

    // English: Event for double-based sliders (Opacity, Scale)
    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        ApplyCurrentWidgetSettings();
    }

    // English: Event for the Topmost checkbox
    private void Topmost_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || string.IsNullOrEmpty(_editingWidgetId)) return;
        ApplyCurrentWidgetSettings();
    }

    // English: Centralized logic to update and save widget settings
    private void ApplyCurrentWidgetSettings()
    {
        var cfg = Core.WidgetHost.CurrentConfig;
        if (!cfg.Widgets.ContainsKey(_editingWidgetId)) return;
        var state = cfg.Widgets[_editingWidgetId];

        state.Opacity = SldOpacity.Value;
        state.Scale = SldScale.Value;
        state.IsAlwaysOnTop = TglWidgetTopmost.IsChecked == true;
        state.FontSize = SldFontSize.Value;
        state.FontFamily = (ComboFont.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Segoe UI Variable";

        // English: Send all 6 parameters to the host
        Core.WidgetHost.UpdateWidgetVisuals(_editingWidgetId, state.Opacity, state.Scale, state.IsAlwaysOnTop, state.FontFamily, state.FontSize);
    }
    private void SystemSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        WidgetHost.CurrentConfig.RunAtStartup = TglAutoStart.IsChecked == true;
        WidgetHost.CurrentConfig.LockWidgets = TglLock.IsChecked == true;
        WidgetHost.CurrentConfig.DisableShaderForScreenshots = TglShader.IsChecked == true;
        AutoStartManager.SetAutoStart(WidgetHost.CurrentConfig.RunAtStartup);
        WidgetHost.ApplySystemSettings();
    }



    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
    private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Hide();
    private void BtnMenuToggle_Click(object sender, RoutedEventArgs e) => ToggleSidebar();
    private void ToggleSidebar()
    {
        bool isCollapsed = BtnMenuToggle.IsChecked == true;
        Sidebar.BeginAnimation(WidthProperty, new DoubleAnimation { To = isCollapsed ? 0 : 220, Duration = TimeSpan.FromSeconds(0.4), EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut } });
    }
}