using System;
using System.Linq;
using System.Windows;

namespace ProtoMaster.Services;

public enum AppTheme
{
    Light,
    Dark
}

public static class ThemeManager
{
    private static AppTheme _currentTheme = AppTheme.Dark;

    public static AppTheme CurrentTheme => _currentTheme;

    public static event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>
    /// 初始化主题管理器，从设置中加载主题
    /// </summary>
    public static void Initialize()
    {
        var savedTheme = SettingsManager.GetTheme();
        ApplyTheme(savedTheme, saveToSettings: false); // 初始化时不需要保存
    }

    public static void ApplyTheme(AppTheme theme, bool saveToSettings = true)
    {
        _currentTheme = theme;

        var themeUri = theme switch
        {
            AppTheme.Dark => new Uri("Themes/DarkTheme.xaml", UriKind.Relative),
            AppTheme.Light => new Uri("Themes/LightTheme.xaml", UriKind.Relative),
            _ => new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
        };

        // 移除旧主题
        var existingTheme = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);

        if (existingTheme != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
        }

        // 添加新主题
        var newTheme = new ResourceDictionary { Source = themeUri };
        Application.Current.Resources.MergedDictionaries.Add(newTheme);

        // 保存主题设置
        if (saveToSettings)
        {
            SettingsManager.SaveTheme(theme);
        }

        ThemeChanged?.Invoke(null, theme);

        System.Diagnostics.Debug.WriteLine($"Theme changed to: {theme}");
    }

    public static void ToggleTheme()
    {
        ApplyTheme(_currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }
}