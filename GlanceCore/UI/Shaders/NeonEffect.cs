namespace GlanceCore.UI.Shaders;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

public class NeonEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(NeonEffect), 0);
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(double), typeof(NeonEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));
    public static readonly DependencyProperty NeonColorProperty = DependencyProperty.Register("NeonColor", typeof(Color), typeof(NeonEffect), new UIPropertyMetadata(Colors.Magenta, PixelShaderConstantCallback(1)));

    public NeonEffect()
    {
        PixelShader = new PixelShader { UriSource = new Uri("pack://application:,,,/UI/Shaders/Neon.ps") };
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(TimeProperty);
        UpdateShaderValue(NeonColorProperty);
    }

    public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public double Time { get => (double)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }
    public Color NeonColor { get => (Color)GetValue(NeonColorProperty); set => SetValue(NeonColorProperty, value); }
}