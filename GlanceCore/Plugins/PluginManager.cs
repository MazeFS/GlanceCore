namespace GlanceCore.Plugins;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using GlanceCore.Core;

public static class PluginManager
{
    public static List<WidgetInfo> LoadPlugins()
    {
        var plugins = new List<WidgetInfo>();
        string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
            return plugins;
        }

        foreach (var dll in Directory.GetFiles(pluginsPath, "*.dll"))
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);

                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IWidgetPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        var plugin = (IWidgetPlugin)Activator.CreateInstance(type)!;
                        plugins.Add(new WidgetInfo
                        {
                            Id = plugin.Id,
                            Title = plugin.Title,
                            Description = plugin.Description,
                            PreviewImage = plugin.PreviewImagePath,
                            Width = plugin.DefaultWidth,
                            WidgetType = plugin.WidgetWindowType,
                            PluginInstance = plugin
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText("debug_log.txt", $"{DateTime.Now}: PLUGIN LOAD ERROR ({Path.GetFileName(dll)}): {ex.Message}\nInner: {ex.InnerException?.Message}\n\n");
                }
                catch { }
            }
        }

        return plugins;
    }
}