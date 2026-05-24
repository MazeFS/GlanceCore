namespace GlanceCore;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Drawing;
using System.Reflection;
using System.Threading;
using GlanceCore.Core;
using WinForms = System.Windows.Forms;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private Views.HubWindow? _hubWindow;
    private static Mutex? _mutex;
    public bool IsHubVisible => _hubWindow != null && _hubWindow.IsVisible;

    public App()
    {
        // РЕГИСТРАЦИЯ ГЛОБАЛЬНЫХ ПЕРЕХВАТЧИКОВ ДЛЯ ДИАГНОСТИКИ СБОЕВ
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(false, "GlanceCore_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        try
        {
            WidgetHost.Initialize();
            WidgetHost.RestoreActiveWidgets();

            _trayIcon = new WinForms.NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true,
                Text = "GlanceCore"
            };

            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Настройки (Hub)", null, (s, ev) => ShowHub());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Выход", null, (s, ev) => FullShutdown());

            _trayIcon.ContextMenuStrip = contextMenu;
            _trayIcon.MouseDoubleClick += (s, ev) => { if (ev.Button == WinForms.MouseButtons.Left) ShowHub(); };

            if (e.Args.Contains("--autostart"))
                MemoryOptimizer.Trim();
            else
                new Views.SplashWindow().Show();
        }
        catch (Exception ex)
        {
            // Перехватываем ошибку на этапе OnStartup
            LogException(ex, "Критический сбой инициализации OnStartup");
        }
    }

    public void ShowHub()
    {
        if (_hubWindow == null || !_hubWindow.IsLoaded)
        {
            _hubWindow = new Views.HubWindow();
            _hubWindow.Show();
        }
        else
        {
            _hubWindow.WindowState = WindowState.Normal;
            _hubWindow.Activate();
        }
    }

    private void FullShutdown()
    {
        WidgetHost.Shutdown();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        System.Windows.Application.Current.Shutdown();
    }

    // --- СИСТЕМА ЗАПИСИ КРИТИЧЕСКИХ ОШИБОК ---
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogException(e.ExceptionObject as Exception, "Критический сбой домена .NET");
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception, "Сбой главного потока UI интерфейса");
        e.Handled = true; // Пытаемся предотвратить бесшумное закрытие
    }

    private void LogException(Exception? ex, string context)
    {
        if (ex == null) return;
        try
        {
            // Путь к файлу лога в папке с установленной программой
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
            string errorText = $"[{DateTime.Now}] Context: {context}\nException: {ex.ToString()}\n\n";

            File.AppendAllText(logPath, errorText);

            // Показываем красивое и информативное окно ошибки
            MessageBox.Show(
                $"Программа GlanceCore столкнулась с системной ошибкой и будет закрыта!\n\n" +
                $"Тип ошибки: {ex.GetType().Name}\n" +
                $"Описание: {ex.Message}\n\n" +
                $"Подробный отчет сохранен в файл:\n{logPath}",
                "Критическая ошибка GlanceCore",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { }
    }
}