namespace GlanceCore.Views;

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        this.Loaded += SplashWindow_Loaded;
    }

    private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 1. Ждем, пока отработает стартовая XAML-анимация (появление логотипа)
        await Task.Delay(1000);

        // 2. Начинаем загрузку ядра
        UpdateStatus("Loading core modules...", 30);
        await Task.Delay(600); // Имитация тяжелой загрузки для эстетики

        // Настоящая инициализация: читаем конфиги JSON
        GlanceCore.Core.WidgetHost.Initialize();

        UpdateStatus("Initializing Liquid Glass...", 70);
        await Task.Delay(700);

        UpdateStatus("Starting UI engine...", 100);
        await Task.Delay(500); // Даем прогресс-бару дойти до конца

        // 3. Плавное растворение экрана загрузки и запуск Хаба
        DoubleAnimation fadeOut = new DoubleAnimation(0, TimeSpan.FromSeconds(0.4));
        fadeOut.Completed += (s, ev) =>
        {
            var hub = new HubWindow();
            hub.Show();
            this.Close();
            GlanceCore.Core.MemoryOptimizer.Trim();
            // English: Force GC to collect Splash resources and animations memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        };
        this.BeginAnimation(OpacityProperty, fadeOut);
    }

    // Вспомогательный метод для плавной анимации прогресс-бара
    private void UpdateStatus(string text, double percent)
    {
        LoadingStatus.Text = text;

        DoubleAnimation anim = new DoubleAnimation(percent, TimeSpan.FromSeconds(0.4))
        {
            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
        };
        LoadingBar.BeginAnimation(RangeBase.ValueProperty, anim);
    }
}