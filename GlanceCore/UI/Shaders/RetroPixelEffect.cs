namespace GlanceCore.UI.Shaders;

using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media;

public class RetroPixelEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(RetroPixelEffect), 0);
    public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register("PixelSize", typeof(double), typeof(RetroPixelEffect), new UIPropertyMetadata(0.015, PixelShaderConstantCallback(0)));
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(double), typeof(RetroPixelEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(1)));

    public RetroPixelEffect()
    {
        PixelShader = new PixelShader { UriSource = new Uri("pack://application:,,,/UI/Shaders/RetroPixel.ps") };
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(PixelSizeProperty);
        UpdateShaderValue(TimeProperty);
    }

    public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public double PixelSize { get => (double)GetValue(PixelSizeProperty); set => SetValue(PixelSizeProperty, value); }
    public double Time { get => (double)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }
}