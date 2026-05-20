namespace GlanceCore;

using System;
using System.Linq;
using System.Windows;
using System.Drawing;
using System.Reflection;
using System.Threading; // ДЛЯ MUTEX
using GlanceCore.Core;
using WinForms = System.Windows.Forms;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private Views.HubWindow? _hubWindow;
    private static Mutex? _mutex; // ЗАЩИТА ОТ ДУБЛИКАТОВ
    public bool IsHubVisible => _hubWindow != null && _hubWindow.IsVisible;
    protected override void OnStartup(StartupEventArgs e)
    {
        // ИНИЦИАЛИЗАЦИЯ MUTEX (Защита от двойного запуска)
        _mutex = new Mutex(true, "GlanceCore_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }

        base.OnStartup(e);

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
        // Явно указываем системный выход WPF
        System.Windows.Application.Current.Shutdown();
    }
}