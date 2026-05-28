namespace GlanceCore;

using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GlanceCore.Core;
using System.Drawing;
using WinForms = System.Windows.Forms;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private Views.HubWindow? _hubWindow;
    private static Mutex? _mutex;
    public bool IsHubVisible => _hubWindow != null && _hubWindow.IsVisible;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException(e.ExceptionObject as Exception);
        this.DispatcherUnhandledException += (s, e) => { LogException(e.Exception); e.Handled = true; };
        TaskScheduler.UnobservedTaskException += (s, e) => { LogException(e.Exception); e.SetObserved(); };
    }

    private static void LogException(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
            File.AppendAllText(logPath, $"{DateTime.Now}: CRITICAL EXCEPTION: {ex.GetType().Name} - {ex.Message}\nStack: {ex.StackTrace}\nInner: {ex.InnerException?.Message}\n\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(false, "GlanceCore_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            SendShowHubCommand();
            System.Windows.Application.Current.Shutdown();
            return;
        }

        StartNamedPipeServer();

        base.OnStartup(e);

        WidgetHost.Initialize();

        string theme = WidgetHost.CurrentConfig.HubTheme;
        if (string.IsNullOrEmpty(theme)) theme = "Original";
        ChangeTheme(theme);

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

    private static void StartNamedPipeServer()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream("GlanceCore_Pipe", PipeDirection.In, 1);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server);
                    if (await reader.ReadLineAsync() == "SHOW_HUB")
                    {
                        Current.Dispatcher.Invoke(() => { if (Current is App app) app.ShowHub(); });
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText("debug_log.txt", $"{DateTime.Now}: SERVER PIPE ERROR: {ex.Message}\n"); } catch { }
                    await Task.Delay(3000);
                }
            }
        });
    }

    private static void SendShowHubCommand()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", "GlanceCore_Pipe", PipeDirection.Out);
            client.Connect(500);
            using var writer = new StreamWriter(client);
            writer.WriteLine("SHOW_HUB");
            writer.Flush();
        }
        catch (Exception ex)
        {
            try { File.AppendAllText("debug_log.txt", $"{DateTime.Now}: CLIENT PIPE ERROR: {ex.Message}\n"); } catch { }
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

    public static void ChangeTheme(string themeName)
    {
        var newDict = new ResourceDictionary { Source = new Uri($"/UI/Dictionaries/Themes/{themeName}.xaml", UriKind.Relative) };
        var oldDict = Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Themes/"));

        if (oldDict != null) Current.Resources.MergedDictionaries.Remove(oldDict);
        Current.Resources.MergedDictionaries.Add(newDict);
    }
}