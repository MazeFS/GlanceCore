
***

# GlanceCore SDK Developer Guide (v0.5.0)

GlanceCore supports the dynamic loading of third-party extensions in two formats:
1. **Widget Plugins (Widgets):** Custom windows with any functionality.
2. **Global Styles/Shaders (Skins):** Visual styles featuring custom HLSL pixel shaders, which automatically become available for selection across all widgets in the application.

---

## 🛠 Step 1: Project Setup in Visual Studio

All plugins are developed as separate Class Libraries (`.dll`) based on .NET 10.

1. Create a **WPF Class Library** project.
2. Set the target framework to **.NET 10**.
3. Open the `.csproj` file and configure it as follows:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="GlanceCore">
      <HintPath>..\..\GlanceCore\GlanceCore\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\GlanceCore.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

---

## 🧩 Step 2: Creating a Widget Plugin

Each plugin must have a single entry point — a class implementing the `GlanceCore.Plugins.IWidgetPlugin` interface.

### 1. Plugin Entry Point (`PluginEntry.cs`)

```csharp
namespace MyCustomPlugin;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GlanceCore.Plugins;
using GlanceCore.Core;

public class PluginEntry : IWidgetPlugin
{
    public string Id => "CustomWidget_01";
    public string Title => "Custom Widget";
    public string Description => "A custom extension widget";
    public string PreviewImagePath => ""; 
    public int DefaultWidth => 250;
    public Type WidgetWindowType => typeof(CustomWidgetWindow);

    public FrameworkElement? GetSettingsUI(WidgetState state, Action saveCallback)
    {
        var panel = new StackPanel();

        var label = new TextBlock
        {
            Text = "Widget Text",
            Foreground = Brushes.White,
            Opacity = 0.6,
            Margin = new Thickness(0, 10, 0, 5),
            FontSize = 12
        };
        panel.Children.Add(label);

        var input = new TextBox
        {
            Text = state.CustomData,
            Height = 30,
            Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 0, 8, 0),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        input.TextChanged += (s, e) =>
        {
            state.CustomData = input.Text;
            saveCallback();
        };

        panel.Children.Add(input);
        return panel;
    }
}
```

### 2. Widget Window XAML Markup (`CustomWidgetWindow.xaml`)

```xml
<widgets:BaseWidgetWindow x:Class="MyCustomPlugin.CustomWidgetWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:widgets="clr-namespace:GlanceCore.Widgets;assembly=GlanceCore"
        Title="CustomWidget" WindowStyle="None" Background="Transparent" AllowsTransparency="True"
        DataContext="{Binding RelativeSource={RelativeSource Self}}">

    <Grid x:Name="MainRoot" Width="250" Height="150" Margin="15">
        <Grid.Effect>
            <DropShadowEffect BlurRadius="25" ShadowDepth="8" Opacity="0.4" Color="Black"/>
        </Grid.Effect>

        <Border x:Name="BackgroundLayer" CornerRadius="{Binding WidgetCornerRadius}" BorderBrush="{Binding BorderColorBrush}" BorderThickness="1.5" ClipToBounds="True" Opacity="{Binding BgOpacity}">
            <Grid>
                <Border x:Name="GlassBase" CornerRadius="{Binding WidgetCornerRadius}">
                    <Border.Background>
                        <ImageBrush x:Name="WallpaperBrush" Stretch="Fill"/>
                    </Border.Background>
                </Border>
                <Border x:Name="MinimalBase" Background="{Binding BgColorBrush}" CornerRadius="{Binding WidgetCornerRadius}"/>
                <Border x:Name="GlossBevel" CornerRadius="{Binding WidgetCornerRadius}" BorderBrush="#35FFFFFF" BorderThickness="1.5" Margin="1" IsHitTestVisible="False">
                    <Border.Background>
                        <RadialGradientBrush Center="0.5,0" RadiusX="0.9" RadiusY="0.6">
                            <GradientStop Color="#25FFFFFF" Offset="0"/>
                            <GradientStop Color="Transparent" Offset="1"/>
                        </RadialGradientBrush>
                    </Border.Background>
                </Border>
            </Grid>
        </Border>

        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Margin="20" IsHitTestVisible="False">
            <TextBlock x:Name="TxtCustom" Text="Hello World" Foreground="{Binding TextColorBrush}" FontSize="{Binding FontSize}" FontWeight="Bold" Effect="{Binding TextGlowEffect}"/>
        </StackPanel>
    </Grid>
</widgets:BaseWidgetWindow>
```

### 3. Widget Window Code-Behind (`CustomWidgetWindow.xaml.cs`)

```csharp
namespace MyCustomPlugin;

using System;
using System.Windows;
using System.Windows.Controls;
using GlanceCore.Widgets;

public partial class CustomWidgetWindow : BaseWidgetWindow
{
    public CustomWidgetWindow()
    {
        InitializeComponent();
    }

    public override void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)
    {
        base.ApplySettings(state, isGlobalLocked, isGlobalShaderEnabled);
        
        if (TxtCustom != null && !string.IsNullOrEmpty(state.CustomData))
        {
            TxtCustom.Text = state.CustomData;
        }
    }

    private void CloseWidget_Click(object sender, RoutedEventArgs e) => GlanceCore.Core.WidgetHost.CloseWidgetExplicitly("CustomWidget_01");
}
```

---

## 🎨 Step 3: Creating a Global Skin/Shader Plugin

A skin plugin provides shader bytecode (compiled via `fxc.exe` into a `.ps` file) and a resource dictionary that modifies the visual appearance of all widgets in the system.

### 1. Skin Plugin Entry Point (`MySkinEntry.cs`)

```csharp
namespace MySkinPlugin;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Collections.Generic;
using GlanceCore.Plugins;

public class MySkinEntry : ISkinPlugin
{
    public string Id => "MyCustomNeonSkin";
    public string Name => "Cyberpunk Neon";
    public string PreviewImagePath => "/Resource/Skins/Skin_Neon.png";
    public string Color => "#FF00FF";

    public ResourceDictionary? GetResources()
    {
        var dict = new ResourceDictionary();
        dict.Add("ThemeText", new SolidColorBrush(Colors.Magenta));
        dict.Add("ThemeTextMuted", new SolidColorBrush(Color.FromArgb(160, 255, 0, 255)));
        dict.Add("BorderColorBrush", new SolidColorBrush(Colors.Magenta));
        
        var fx = new MyCustomShaderEffect();
        dict.Add("CustomShaderEffect", fx);
        return dict;
    }

    public ShaderEffect? GetEffect()
    {
        return new MyCustomShaderEffect();
    }
}
```

### 2. C# Shader Effect Wrapper (`MyCustomShaderEffect.cs`)

```csharp
namespace MySkinPlugin;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

public class MyCustomShaderEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(MyCustomShaderEffect), 0);
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(double), typeof(MyCustomShaderEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

    public MyCustomShaderEffect()
    {
        PixelShader = new PixelShader { UriSource = new Uri("pack://application:,,,/MySkinPlugin;component/MyCustomShader.ps") };
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(TimeProperty);
    }

    public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public double Time { get => (double)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }
}
```

---

## 💾 Step 4: Testing and Local Installation

1. Build your plugin project in **Release** mode:
   ```bash
   dotnet build -c Release
   ```
2. Copy the resulting `YourPlugin.dll` file from the output folder (`bin/Release/...`).
3. Navigate to the working directory of your GlanceCore installation.
4. Create a **`Plugins`** folder (in the same directory as `GlanceCore.exe`) and paste the copied `.dll` inside this folder.
5. Launch GlanceCore — your plugin will instantly appear in the Hub!

---

## 🌐 Step 5: Publishing to the Plugin Store

Plugin distribution is entirely decentralized and free, powered by the GitHub API:

1. Upload your compiled `.dll` file to the **Releases** section of your public GitHub repository. Copy the direct download link.
2. Submit a Pull Request to the **`store_test.json`** file in the main GlanceCore repository, appending your plugin's JSON block:
   ```json
   {
     "id": "YourUniqueId_01",
     "title": "Your Widget Name",
     "description": "Widget description here",
     "previewImage": "DIRECT_URL_TO_PREVIEW_PNG_ON_GITHUB",
     "downloadUrl": "DIRECT_URL_TO_DLL_IN_YOUR_GITHUB_RELEASES",
     "author": "YourName",
     "rating": 5.0,
     "category": "Essentials"
   }
   ```
3. Once your Pull Request is merged, your plugin will instantly become available for 1-click installation to all GlanceCore users!
