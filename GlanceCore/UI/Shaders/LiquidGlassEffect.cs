namespace GlanceCore.UI.Shaders;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

public class LiquidGlassEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty("Input", typeof(LiquidGlassEffect), 0);

    public static readonly DependencyProperty AmountProperty =
        DependencyProperty.Register("Amount", typeof(double), typeof(LiquidGlassEffect),
            new UIPropertyMetadata(0.08, PixelShaderConstantCallback(0)));

    public LiquidGlassEffect()
    {
        // English: The most reliable path for resources in .NET 10
        PixelShader = new PixelShader
        {
            UriSource = new Uri("pack://application:,,,/UI/Shaders/LiquidGlass.ps")
        };
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(AmountProperty);
    }

    public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public double Amount { get => (double)GetValue(AmountProperty); set => SetValue(AmountProperty, value); }
}