using System.Configuration;
using System.Data;
using System.Windows;
using ProtoMaster.Services;

namespace ProtoMaster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 在应用启动时初始化主题管理器
            // 这会从保存的设置中加载上次使用的主题
            ThemeManager.Initialize();
        }
    }
}
