using System;
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

    public static void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;

        var themeUri = theme switch
        {
            AppTheme.Dark => new Uri("Themes/DarkTheme.xaml", UriKind.Relative),
            AppTheme.Light => new Uri("Themes/LightTheme.xaml", UriKind.Relative),
            _ => new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
        };

        // 移除现有主题
        var existingTheme = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);

        if (existingTheme != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
        }

        // 添加新主题
        var newTheme = new ResourceDictionary { Source = themeUri };
        Application.Current.Resources.MergedDictionaries.Add(newTheme);

        ThemeChanged?.Invoke(null, theme);
    }

    public static void ToggleTheme()
    {
        ApplyTheme(_currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }
}