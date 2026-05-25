namespace GlanceCore;

using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using GlanceCore.Core;
using WinForms = System.Windows.Forms;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private Views.HubWindow? _hubWindow;
    private static Mutex? _mutex;
    public bool IsHubVisible => _hubWindow != null && _hubWindow.IsVisible;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. ПУНКТ 2: ЗАЩИТА ОТ ДУБЛИКАТОВ + NAMED PIPES
        _mutex = new Mutex(true, "GlanceCore_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            SendShowHubCommand();
            System.Windows.Application.Current.Shutdown();
            return;
        }

        StartNamedPipeServer();

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

        // 2. ПУНКТ 1: ИСПРАВЛЕНИЕ АВТОЗАПУСКА (Никаких окон, только трей и виджеты)
        if (e.Args.Contains("--autostart"))
            MemoryOptimizer.Trim();
        else
            new Views.SplashWindow().Show();
    }

    private static void StartNamedPipeServer()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream("GlanceCore_Pipe", PipeDirection.In);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server);
                    if (await reader.ReadLineAsync() == "SHOW_HUB")
                    {
                        Current.Dispatcher.Invoke(() => { if (Current is App app) app.ShowHub(); });
                    }
                }
                catch { }
            }
        });
    }

    private static void SendShowHubCommand()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", "GlanceCore_Pipe", PipeDirection.Out);
            client.Connect(1000);
            using var writer = new StreamWriter(client);
            writer.WriteLine("SHOW_HUB");
            writer.Flush();
        }
        catch { }
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
            _hubWindow.Topmost = true; 
            _hubWindow.Topmost = false;
            _hubWindow.Focus();
        }
    }

    private void FullShutdown()
    {
        WidgetHost.Shutdown();
        if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
        System.Windows.Application.Current.Shutdown();
    }
}