Here is the English translation of the documentation, formatted and ready to be copied:



\*\*\*



\# GlanceCore SDK Developer Guide (v0.5.0)



GlanceCore supports the dynamic loading of third-party extensions in two formats:

1\. \*\*Widget Plugins (Widgets):\*\* Custom windows with any functionality.

2\. \*\*Global Styles/Shaders (Skins):\*\* Visual styles featuring custom HLSL pixel shaders, which automatically become available for selection across all widgets in the application.



\---



\## 🛠 Step 1: Project Setup in Visual Studio



All plugins are developed as separate Class Libraries (`.dll`) based on .NET 10.



1\. Create a \*\*WPF Class Library\*\* project.

2\. Set the target framework to \*\*.NET 10\*\*.

3\. Open the `.csproj` file and configure it as follows:



```xml

<Project Sdk="Microsoft.NET.Sdk">

&#x20; <PropertyGroup>

&#x20;   <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>

&#x20;   <UseWPF>true</UseWPF>

&#x20;   <Nullable>enable</Nullable>

&#x20;   <ImplicitUsings>enable</ImplicitUsings>

&#x20; </PropertyGroup>

&#x20; <ItemGroup>

&#x20;   <Reference Include="GlanceCore">

&#x20;     <HintPath>..\\..\\GlanceCore\\GlanceCore\\bin\\Release\\net10.0-windows10.0.19041.0\\win-x64\\publish\\GlanceCore.dll</HintPath>

&#x20;     <Private>false</Private>

&#x20;   </Reference>

&#x20; </ItemGroup>

</Project>

```



\---



\## 🧩 Step 2: Creating a Widget Plugin



Each plugin must have a single entry point — a class implementing the `GlanceCore.Plugins.IWidgetPlugin` interface.



\### 1. Plugin Entry Point (`PluginEntry.cs`)



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

&#x20;   public string Id => "CustomWidget\_01";

&#x20;   public string Title => "Custom Widget";

&#x20;   public string Description => "A custom extension widget";

&#x20;   public string PreviewImagePath => ""; 

&#x20;   public int DefaultWidth => 250;

&#x20;   public Type WidgetWindowType => typeof(CustomWidgetWindow);



&#x20;   public FrameworkElement? GetSettingsUI(WidgetState state, Action saveCallback)

&#x20;   {

&#x20;       var panel = new StackPanel();



&#x20;       var label = new TextBlock

&#x20;       {

&#x20;           Text = "Widget Text",

&#x20;           Foreground = Brushes.White,

&#x20;           Opacity = 0.6,

&#x20;           Margin = new Thickness(0, 10, 0, 5),

&#x20;           FontSize = 12

&#x20;       };

&#x20;       panel.Children.Add(label);



&#x20;       var input = new TextBox

&#x20;       {

&#x20;           Text = state.CustomData,

&#x20;           Height = 30,

&#x20;           Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),

&#x20;           Foreground = Brushes.White,

&#x20;           BorderThickness = new Thickness(0),

&#x20;           Padding = new Thickness(8, 0, 8, 0),

&#x20;           VerticalContentAlignment = VerticalAlignment.Center

&#x20;       };

&#x20;       

&#x20;       input.TextChanged += (s, e) =>

&#x20;       {

&#x20;           state.CustomData = input.Text;

&#x20;           saveCallback();

&#x20;       };



&#x20;       panel.Children.Add(input);

&#x20;       return panel;

&#x20;   }

}

```



\### 2. Widget Window XAML Markup (`CustomWidgetWindow.xaml`)



```xml

<widgets:BaseWidgetWindow x:Class="MyCustomPlugin.CustomWidgetWindow"

&#x20;       xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"

&#x20;       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"

&#x20;       xmlns:widgets="clr-namespace:GlanceCore.Widgets;assembly=GlanceCore"

&#x20;       Title="CustomWidget" WindowStyle="None" Background="Transparent" AllowsTransparency="True"

&#x20;       DataContext="{Binding RelativeSource={RelativeSource Self}}">



&#x20;   <Grid x:Name="MainRoot" Width="250" Height="150" Margin="15">

&#x20;       <Grid.Effect>

&#x20;           <DropShadowEffect BlurRadius="25" ShadowDepth="8" Opacity="0.4" Color="Black"/>

&#x20;       </Grid.Effect>



&#x20;       <Border x:Name="BackgroundLayer" CornerRadius="{Binding WidgetCornerRadius}" BorderBrush="{Binding BorderColorBrush}" BorderThickness="1.5" ClipToBounds="True" Opacity="{Binding BgOpacity}">

&#x20;           <Grid>

&#x20;               <Border x:Name="GlassBase" CornerRadius="{Binding WidgetCornerRadius}">

&#x20;                   <Border.Background>

&#x20;                       <ImageBrush x:Name="WallpaperBrush" Stretch="Fill"/>

&#x20;                   </Border.Background>

&#x20;               </Border>

&#x20;               <Border x:Name="MinimalBase" Background="{Binding BgColorBrush}" CornerRadius="{Binding WidgetCornerRadius}"/>

&#x20;               <Border x:Name="GlossBevel" CornerRadius="{Binding WidgetCornerRadius}" BorderBrush="#35FFFFFF" BorderThickness="1.5" Margin="1" IsHitTestVisible="False">

&#x20;                   <Border.Background>

&#x20;                       <RadialGradientBrush Center="0.5,0" RadiusX="0.9" RadiusY="0.6">

&#x20;                           <GradientStop Color="#25FFFFFF" Offset="0"/>

&#x20;                           <GradientStop Color="Transparent" Offset="1"/>

&#x20;                       </RadialGradientBrush>

&#x20;                   </Border.Background>

&#x20;               </Border>

&#x20;           </Grid>

&#x20;       </Border>



&#x20;       <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Margin="20" IsHitTestVisible="False">

&#x20;           <TextBlock x:Name="TxtCustom" Text="Hello World" Foreground="{Binding TextColorBrush}" FontSize="{Binding FontSize}" FontWeight="Bold" Effect="{Binding TextGlowEffect}"/>

&#x20;       </StackPanel>

&#x20;   </Grid>

</widgets:BaseWidgetWindow>

```



\### 3. Widget Window Code-Behind (`CustomWidgetWindow.xaml.cs`)



```csharp

namespace MyCustomPlugin;



using System;

using System.Windows;

using System.Windows.Controls;

using GlanceCore.Widgets;



public partial class CustomWidgetWindow : BaseWidgetWindow

{

&#x20;   public CustomWidgetWindow()

&#x20;   {

&#x20;       InitializeComponent();

&#x20;   }



&#x20;   public override void ApplySettings(Core.WidgetState state, bool isGlobalLocked, bool isGlobalShaderEnabled)

&#x20;   {

&#x20;       base.ApplySettings(state, isGlobalLocked, isGlobalShaderEnabled);

&#x20;       

&#x20;       if (TxtCustom != null \&\& !string.IsNullOrEmpty(state.CustomData))

&#x20;       {

&#x20;           TxtCustom.Text = state.CustomData;

&#x20;       }

&#x20;   }



&#x20;   private void CloseWidget\_Click(object sender, RoutedEventArgs e) => GlanceCore.Core.WidgetHost.CloseWidgetExplicitly("CustomWidget\_01");

}

```



\---



\## 🎨 Step 3: Creating a Global Skin/Shader Plugin



A skin plugin provides shader bytecode (compiled via `fxc.exe` into a `.ps` file) and a resource dictionary that modifies the visual appearance of all widgets in the system.



\### 1. Skin Plugin Entry Point (`MySkinEntry.cs`)



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

&#x20;   public string Id => "MyCustomNeonSkin";

&#x20;   public string Name => "Cyberpunk Neon";

&#x20;   public string PreviewImagePath => "/Resource/Skins/Skin\_Neon.png";

&#x20;   public string Color => "#FF00FF";



&#x20;   public ResourceDictionary? GetResources()

&#x20;   {

&#x20;       var dict = new ResourceDictionary();

&#x20;       dict.Add("ThemeText", new SolidColorBrush(Colors.Magenta));

&#x20;       dict.Add("ThemeTextMuted", new SolidColorBrush(Color.FromArgb(160, 255, 0, 255)));

&#x20;       dict.Add("BorderColorBrush", new SolidColorBrush(Colors.Magenta));

&#x20;       

&#x20;       var fx = new MyCustomShaderEffect();

&#x20;       dict.Add("CustomShaderEffect", fx);

&#x20;       return dict;

&#x20;   }



&#x20;   public ShaderEffect? GetEffect()

&#x20;   {

&#x20;       return new MyCustomShaderEffect();

&#x20;   }

}

```



\### 2. C# Shader Effect Wrapper (`MyCustomShaderEffect.cs`)



```csharp

namespace MySkinPlugin;



using System;

using System.Windows;

using System.Windows.Media;

using System.Windows.Media.Effects;



public class MyCustomShaderEffect : ShaderEffect

{

&#x20;   public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(MyCustomShaderEffect), 0);

&#x20;   public static readonly DependencyProperty TimeProperty = DependencyProperty.Register("Time", typeof(double), typeof(MyCustomShaderEffect), new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));



&#x20;   public MyCustomShaderEffect()

&#x20;   {

&#x20;       PixelShader = new PixelShader { UriSource = new Uri("pack://application:,,,/MySkinPlugin;component/MyCustomShader.ps") };

&#x20;       UpdateShaderValue(InputProperty);

&#x20;       UpdateShaderValue(TimeProperty);

&#x20;   }



&#x20;   public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }

&#x20;   public double Time { get => (double)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

}

```



\---



\## 💾 Step 4: Testing and Local Installation



1\. Build your plugin project in \*\*Release\*\* mode:

&#x20;  ```bash

&#x20;  dotnet build -c Release

&#x20;  ```

2\. Copy the resulting `YourPlugin.dll` file from the output folder (`bin/Release/...`).

3\. Navigate to the working directory of your GlanceCore installation.

4\. Create a \*\*`Plugins`\*\* folder (in the same directory as `GlanceCore.exe`) and paste the copied `.dll` inside this folder.

5\. Launch GlanceCore — your plugin will instantly appear in the Hub!



\---



\## 🌐 Step 5: Publishing to the Plugin Store



Plugin distribution is entirely decentralized and free, powered by the GitHub API:



1\. Upload your compiled `.dll` file to the \*\*Releases\*\* section of your public GitHub repository. Copy the direct download link.

2\. Submit a Pull Request to the \*\*`store\_test.json`\*\* file in the main GlanceCore repository, appending your plugin's JSON block:

&#x20;  ```json

&#x20;  {

&#x20;    "id": "YourUniqueId\_01",

&#x20;    "title": "Your Widget Name",

&#x20;    "description": "Widget description here",

&#x20;    "previewImage": "DIRECT\_URL\_TO\_PREVIEW\_PNG\_ON\_GITHUB",

&#x20;    "downloadUrl": "DIRECT\_URL\_TO\_DLL\_IN\_YOUR\_GITHUB\_RELEASES",

&#x20;    "author": "YourName",

&#x20;    "rating": 5.0,

&#x20;    "category": "Essentials"

&#x20;  }

&#x20;  ```

3\. Once your Pull Request is merged, your plugin will instantly become available for 1-click installation to all GlanceCore users!

