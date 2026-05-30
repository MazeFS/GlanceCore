Вот полный текст документации для создания файла **`docs/developer_guide_ru.md`**. Скопируй этот блок целиком. Все примеры кода и разметки полностью зачищены от комментариев согласно правилу №15.

***

# Руководство разработчика GlanceCore SDK (v0.5.0)

GlanceCore поддерживает динамическую загрузку сторонних расширений в двух форматах:
1. **Виджеты-плагины (Widgets):** Кастомные окна с любой функциональностью.
2. **Глобальные стили/шейдеры (Skins):** Стили оформления с собственными пиксельными HLSL-шейдерами, которые автоматически становятся доступны для выбора во всех виджетах приложения.

---

## 🛠 Шаг 1: Подготовка проекта в Visual Studio

Все плагины разрабатываются в виде отдельных библиотек классов (.dll) на базе .NET 10.

1. Создайте проект **WPF Class Library (Библиотека классов WPF)**.
2. Укажите версию платформы **.NET 10**.
3. Откройте файл проекта `.csproj` и настройте его конфигурацию:

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

## 🧩 Шаг 2: Создание Виджета-плагина

Каждый плагин должен иметь одну точку входа — класс, реализующий интерфейс `GlanceCore.Plugins.IWidgetPlugin`.

### 1. Точка входа плагина (`PluginEntry.cs`)

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
            Text = "Текст виджета",
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

### 2. XAML-разметка окна виджета (`CustomWidgetWindow.xaml`)

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

### 3. C#-код окна виджета (`CustomWidgetWindow.xaml.cs`)

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

## 🎨 Шаг 3: Создание глобального плагина Скинов/Шейдеров

Плагин стиля поставляет скомпилированный в `fxc.exe` байт-код шейдера (`.ps`) и словарь ресурсов, изменяющий визуальный облик всех виджетов системы.

### 1. Точка входа плагина-стиля (`MySkinEntry.cs`)

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

### 2. C#-обертка эффекта (`MyCustomShaderEffect.cs`)

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

## 💾 Шаг 4: Тестирование и Локальная установка

1. Скомпилируйте проект плагина в режиме **Release**:
   ```bash
   dotnet build -c Release
   ```
2. Скопируйте полученную библиотеку `ВашПлагин.dll` из папки вывода `bin/Release/...`.
3. Перейдите в рабочую папку запущенной программы GlanceCore.
4. Создайте папку **`Plugins`** (рядом с GlanceCore.exe) и вставьте скопированную `.dll` внутрь этой папки.
5. Запустите GlanceCore — плагин мгновенно появится в Хабе!

---

## 🌐 Шаг 5: Публикация в Магазине плагинов (Store)

Дистрибуция плагинов полностью децентрализована и бесплатна на базе GitHub API:

1. Залейте скомпилированный `.dll` файл в релизы (Releases) своего публичного GitHub репозитория. Скопируйте прямую ссылку на скачивание.
2. Отправьте Pull Request в файл **`store_test.json`** главного репозитория GlanceCore, дописав блок своего плагина:
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
3. После слияния Pull Request ваш плагин станет мгновенно доступен для установки всем пользователям GlanceCore в один клик!
```