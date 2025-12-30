using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProtoMaster.Services
{
    /// <summary>
    /// 应用程序设置管理器
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProtoMaster", 
            "settings.json");

        private static AppSettings? _cachedSettings;

        /// <summary>
        /// 获取应用程序设置
        /// </summary>
        public static AppSettings LoadSettings()
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _cachedSettings = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                _cachedSettings = new AppSettings();
            }

            return _cachedSettings;
        }

        /// <summary>
        /// 保存应用程序设置
        /// </summary>
        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsPath, json);
                _cachedSettings = settings;

                System.Diagnostics.Debug.WriteLine($"Settings saved to: {_settingsPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前主题设置
        /// </summary>
        public static AppTheme GetTheme()
        {
            return LoadSettings().Theme;
        }

        /// <summary>
        /// 保存主题设置
        /// </summary>
        public static void SaveTheme(AppTheme theme)
        {
            var settings = LoadSettings();
            settings.Theme = theme;
            SaveSettings(settings);
        }

        /// <summary>
        /// 初始化主题管理器，从设置中加载主题
        /// </summary>
        public static void Initialize()
        {
            var savedTheme = GetTheme();
            ApplyTheme(savedTheme, saveToSettings: false); // 初始化时不需要保存
        }

        public static void ApplyTheme(AppTheme theme, bool saveToSettings = true)
        {
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
                SaveTheme(theme);
            }

            System.Diagnostics.Debug.WriteLine($"Theme changed to: {theme}");
        }
    }

    /// <summary>
    /// 应用程序设置模型
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 主题设置
        /// </summary>
        public AppTheme Theme { get; set; } = AppTheme.Dark;
    }
}