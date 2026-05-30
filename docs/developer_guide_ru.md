\# 💎 Руководство по разработке плагинов для GlanceCore (v0.5.0)



Добро пожаловать в руководство разработчика GlanceCore SDK. Платформа GlanceCore поддерживает динамическую загрузку сторонних расширений в двух форматах:

1\. \*\*Виджеты-плагины (Widgets):\*\* Кастомные окна с любой функциональностью.

2\. \*\*Глобальные стили/шейдеры (Skins):\*\* Стили оформления с собственными пиксельными HLSL-шейдерами, которые автоматически становятся доступны для выбора во всех виджетах приложения.



\---



\## 🛠 Шаг 1: Подготовка проекта в Visual Studio



Все плагины разрабатываются в виде отдельных библиотек классов (.dll) на базе .NET 10.



1\. Создайте проект \*\*WPF Class Library (Библиотека классов WPF)\*\*.

2\. Укажите версию платформы \*\*.NET 10\*\*.

3\. Откройте файл проекта `.csproj` и настройте его конфигурацию:



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

🧩 Шаг 2: Создание Виджета-плагина

Каждый плагин должен иметь одну точку входа — класс, реализующий интерфейс GlanceCore.Plugins.IWidgetPlugin.

1\. Точка входа плагина (PluginEntry.cs)

code

C#

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

&#x20;           Text = "Текст виджета",

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

2\. XAML-разметка окна виджета (CustomWidgetWindow.xaml)

Окно виджета обязано наследоваться от базового класса BaseWidgetWindow и иметь строго стандартизированную структуру слоев для работы эффектов стекла:

code

Xml

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

&#x20;                           <GradientStop Color="25FFFFFF" Offset="0"/>

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

3\. C#-код окна виджета (CustomWidgetWindow.xaml.cs)

code

C#

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

🎨 Шаг 3: Создание глобального плагина Скинов/Шейдеров

Плагин стиля поставляет скомпилированный байт-код шейдера (.ps) и словарь ресурсов, изменяющий визуальный облик всех виджетов системы.

1\. Точка входа плагина-стиля (MySkinEntry.cs)

code

C#

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

Шейдерный класс MyCustomShaderEffect компилируется стандартным компилятором fxc.exe (профиль ps\_3\_0) и встраивается как ресурс в .csproj плагина.

💾 Шаг 4: Тестирование и Локальная установка

Скомпилируйте проект плагина в режиме Release:

code

Bash

dotnet build -c Release

Скопируйте полученную библиотеку ВашПлагин.dll из папки вывода bin/Release/....

Перейдите в рабочую папку запущенной программы GlanceCore.

Создайте папку Plugins (рядом с GlanceCore.exe) и вставьте скопированную .dll внутрь этой папки.

Запустите GlanceCore — плагин мгновенно появится в Хабе!

🌐 Шаг 5: Публикация в Магазине плагинов (Store)

Дистрибуция плагинов полностью децентрализована и бесплатна на базе GitHub API:

Залейте скомпилированную .dll файл в релизы (Releases) своего публичного GitHub репозитория. Скопируйте прямую ссылку на скачивание.

Отправьте Pull Request в файл store\_test.json главного репозитория GlanceCore, дописав блок своего плагина:

code

JSON

{

&#x20; "id": "YourUniqueId\_01",

&#x20; "title": "Your Widget Name",

&#x20; "description": "Widget description here",

&#x20; "previewImage": "DIRECT\_URL\_TO\_PREVIEW\_PNG\_ON\_GITHUB",

&#x20; "downloadUrl": "DIRECT\_URL\_TO\_DLL\_IN\_YOUR\_GITHUB\_RELEASES",

&#x20; "author": "YourName",

&#x20; "rating": 5.0,

&#x20; "category": "Essentials"

}

После слияния Pull Request ваш плагин станет мгновенно доступен для установки всем пользователям GlanceCore в один клик!

code

Code

\---



\### 🚀 Чек-лист подготовки GlanceCore к релизу (v0.5.0)



Чтобы твое приложение перед публикацией работало с максимальной производительностью, выполни финальную оптимизацию сборки:



1\. \*\*Компиляция ReadyToRun (R2R):\*\*

&#x20;  Это запускает предварительную компиляцию C# в машинный код на твоем компьютере, убирая микро-задержки JIT-компилятора у конечных пользователей при старте. Команда сборки:

&#x20;  ```bash

&#x20;  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true

