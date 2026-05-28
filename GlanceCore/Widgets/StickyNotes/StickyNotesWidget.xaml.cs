namespace GlanceCore.Widgets.StickyNotes;

using System;
using System.Windows.Controls;
using System.Windows;

public partial class StickyNotesWidget : GlanceCore.Widgets.BaseWidgetWindow
{
    private bool _isLoaded = false;

    public StickyNotesWidget()
    {
        InitializeComponent();
    }

    public override void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)
    {
        base.ApplySettings(state, isGlobalLocked, isGlobalShaderEnabled);

        if (!_isLoaded)
        {
            TxtNotes.Text = state.CustomData;
            _isLoaded = true;
        }
    }

    private void TxtNotes_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isLoaded) return;
        var cfg = Core.WidgetHost.CurrentConfig;
        if (cfg.Widgets.ContainsKey("Notes_01"))
        {
            cfg.Widgets["Notes_01"].CustomData = TxtNotes.Text;
            Core.ConfigManager.Save(cfg);
        }
    }
}