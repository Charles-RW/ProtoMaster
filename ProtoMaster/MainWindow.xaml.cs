using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Layout;
using ProtoMaster.Common;
using ProtoMaster.Common.Models;
using ProtoMaster.Models;
using ProtoMaster.PluginInterface;
using ProtoMaster.Services;

namespace ProtoMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IProtoPlugin> _plugins = new();
        private string? _currentPluginName;

        /// <summary>
        /// Common 数据树节点集合
        /// </summary>
        public ObservableCollection<TreeNodeModel> CommonDataNodes { get; } = new();

        /// <summary>
        /// Plugin 数据树节点集合
        /// </summary>
        public ObservableCollection<TreeNodeModel> PluginDataNodes { get; } = new();

        // 批量更新相关字段
        private readonly ConcurrentQueue<FrameData> _pendingFrames = new();
        private readonly DispatcherTimer _batchUpdateTimer;
        private const int BatchSize = 100; // 每次批量处理的最大帧数
        private const int UpdateIntervalMs = 50; // UI 更新间隔（毫秒）

        // 缓存文件节点，避免重复查找
        private readonly Dictionary<string, TreeNodeModel> _commonFileNodeCache = new();
        private readonly Dictionary<string, TreeNodeModel> _pluginFileNodeCache = new();

        // AvalonDock 布局保存/恢复
        private string _layoutConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "ProtoMaster", "layout.config");

        // 帧数据结构
        private record FrameData(string FileName, int DataId, TreeNodeModel? CommonNode, TreeNodeModel? PluginNode);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化批量更新定时器
            _batchUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(UpdateIntervalMs)
            };
            _batchUpdateTimer.Tick += BatchUpdateTimer_Tick;

            // 设置 AvalonDock 主题
            SetAvalonDockTheme(isDark: true);

            LoadPlugins();
            
            // 添加面板状态变化事件监听
            SetupPanelEventHandlers();
            
            LoadLayout();
            
            // 在窗口加载完成后确保布局正确
            this.Loaded += MainWindow_Loaded;
        }

        private void SetupPanelEventHandlers()
        {
            // 监听面板可见性变化事件，自动同步菜单状态
            CommonDataAnchorable.IsVisibleChanged += (sender, e) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MenuShowCommonData.IsChecked = CommonDataAnchorable.IsVisible;
                    Debug.WriteLine($"CommonData visibility changed: {CommonDataAnchorable.IsVisible}");
                });
            };

            PluginDataAnchorable.IsVisibleChanged += (sender, e) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MenuShowPluginData.IsChecked = PluginDataAnchorable.IsVisible;
                    Debug.WriteLine($"PluginData visibility changed: {PluginDataAnchorable.IsVisible}");
                });
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveLayout();
            base.OnClosed(e);
        }

        private void SetAvalonDockTheme(bool isDark)
        {
            // 移除对不存在的主题类的引用
            // AvalonDock 可能需要额外的主题包或使用默认主题
            // 如果需要自定义主题，可以通过其他方式设置
            
            // 注释掉导致错误的主题设置代码
            /*
            if (isDark)
            {
                DockingManager.Theme = new AvalonDock.Themes.Vs2013DarkTheme();
            }
            else
            {
                DockingManager.Theme = new AvalonDock.Themes.Vs2013LightTheme();
            }
            */
            
            // 可以使用 AvalonDock 的默认主题或通过 ResourceDictionary 设置样式
            // 这里暂时留空，使用默认主题
        }

        private void SaveLayout()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_layoutConfigPath)!);
                
                var layoutSerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(DockingManager);
                using var writer = new StreamWriter(_layoutConfigPath);
                layoutSerializer.Serialize(writer);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save layout: {ex.Message}");
            }
        }

        private void LoadLayout()
        {
            try
            {
                if (File.Exists(_layoutConfigPath))
                {
                    var layoutSerializer = new AvalonDock.Layout.Serialization.XmlLayoutSerializer(DockingManager);
                    
                    layoutSerializer.LayoutSerializationCallback += (s, e) =>
                    {
                        Debug.WriteLine($"LayoutSerializationCallback: ContentId={e.Model.ContentId}");
                        
                        // 确保必要的面板被正确恢复
                        switch (e.Model.ContentId)
                        {
                            case "CommonData":
                                e.Content = CommonDataAnchorable.Content;
                                e.Cancel = false;
                                Debug.WriteLine("Restored CommonData content");
                                break;
                            case "PluginData":
                                e.Content = PluginDataAnchorable.Content;
                                e.Cancel = false;
                                Debug.WriteLine("Restored PluginData content");
                                break;
                            case "MainView":
                                e.Cancel = false;
                                Debug.WriteLine("Restored MainView");
                                break;
                            default:
                                Debug.WriteLine($"Unknown content id: {e.Model.ContentId}");
                                break;
                        }
                    };

                    using var reader = new StreamReader(_layoutConfigPath);
                    layoutSerializer.Deserialize(reader);
                    Debug.WriteLine("Layout deserialized successfully");
                }
                else
                {
                    Debug.WriteLine("No layout config file found, using default layout");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load layout: {ex.Message}");
                // 如果加载失败，确保使用默认布局
                EnsureDefaultLayout();
            }
        }

        private void EnsureDefaultLayout()
        {
            try
            {
                Debug.WriteLine("Ensuring default layout");
                
                // 确保面板可见性
                if (CommonDataAnchorable.IsHidden)
                {
                    CommonDataAnchorable.Show();
                }
                if (PluginDataAnchorable.IsHidden)
                {
                    PluginDataAnchorable.Show();
                }
                
                // 同步菜单状态
                SyncMenuStates();
                
                Debug.WriteLine($"Default layout ensured - CommonData visible: {CommonDataAnchorable.IsVisible}, PluginData visible: {PluginDataAnchorable.IsVisible}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error ensuring default layout: {ex.Message}");
            }
        }

        private void BatchUpdateTimer_Tick(object? sender, EventArgs e)
        {
            ProcessPendingFrames();
        }

        private void ProcessPendingFrames()
        {
            int processedCount = 0;

            while (processedCount < BatchSize && _pendingFrames.TryDequeue(out var frame))
            {
                AddFrameToTree(frame);
                processedCount++;
            }

            // 如果队列已空且不再加载，停止定时器
            if (_pendingFrames.IsEmpty && !_isLoading)
            {
                _batchUpdateTimer.Stop();
            }
        }

        private void AddFrameToTree(FrameData frame)
        {
            // 查找或创建文件节点 (使用缓存)
            var commonFileNode = FindOrCreateFileNode(CommonDataNodes, _commonFileNodeCache, frame.FileName);
            var pluginFileNode = FindOrCreateFileNode(PluginDataNodes, _pluginFileNodeCache, frame.FileName);

            // 添加 Common 数据节点
            if (frame.CommonNode != null)
            {
                commonFileNode.Children.Add(frame.CommonNode);
            }

            // 添加 Proto JSON 数据节点
            if (frame.PluginNode != null)
            {
                pluginFileNode.Children.Add(frame.PluginNode);
            }
        }

        private volatile bool _isLoading;

        private void LoadPlugins()
        {
            string pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginFolder))
            {
                Directory.CreateDirectory(pluginFolder);
                return;
            }

            foreach (var dll in Directory.GetFiles(pluginFolder, "*.dll"))
            {
                try
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(dll);
                    foreach (var type in assembly.GetTypes())
                    {
                        // 在加载类型后，先确认可实现的更具体接口，然后再实例化并转换
                        if (typeof(IProtoPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        {
                            try
                            {
                                var obj = Activator.CreateInstance(type);
                                if (obj is IProtoPluginWithTreeData treePlugin)
                                {
                                    _plugins.Add(treePlugin); // 可以直接作为 IProtoPluginWithTreeData 使用
                                    Debug.WriteLine($"Loaded tree plugin: {treePlugin.Name}");
                                }
                                else if (obj is IProtoPlugin plugin)
                                {
                                    _plugins.Add(plugin);
                                    Debug.WriteLine($"Loaded plugin: {plugin.Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"CreateInstance failed for {type.FullName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load plugin {dll}: {ex.Message}");
                }
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                // TODO: 处理打开的文件
            }
        }

        private async void OpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.FolderName;
                await LoadDataFromDirectoryAsync(folderPath);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ToggleCommonDataPanel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"ToggleCommonDataPanel_Click: IsChecked={MenuShowCommonData.IsChecked}");
                
                if (MenuShowCommonData.IsChecked == true)
                {
                    // 显示面板
                    if (CommonDataAnchorable.IsHidden)
                    {
                        CommonDataAnchorable.Show();
                    }
                    else if (!CommonDataAnchorable.IsVisible)
                    {
                        CommonDataAnchorable.IsVisible = true;
                    }
                    
                    // 确保面板激活
                    CommonDataAnchorable.IsActive = true;
                    Debug.WriteLine("CommonData panel shown and activated");
                }
                else
                {
                    // 隐藏面板
                    CommonDataAnchorable.Hide();
                    Debug.WriteLine("CommonData panel hidden");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ToggleCommonDataPanel_Click: {ex.Message}");
                MessageBox.Show($"切换面板时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TogglePluginDataPanel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"TogglePluginDataPanel_Click: IsChecked={MenuShowPluginData.IsChecked}");
                
                if (MenuShowPluginData.IsChecked == true)
                {
                    // 显示面板
                    if (PluginDataAnchorable.IsHidden)
                    {
                        PluginDataAnchorable.Show();
                    }
                    else if (!PluginDataAnchorable.IsVisible)
                    {
                        PluginDataAnchorable.IsVisible = true;
                    }
                    
                    // 确保面板激活
                    PluginDataAnchorable.IsActive = true;
                    Debug.WriteLine("PluginData panel shown and activated");
                }
                else
                {
                    // 隐藏面板
                    PluginDataAnchorable.Hide();
                    Debug.WriteLine("PluginData panel hidden");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in TogglePluginDataPanel_Click: {ex.Message}");
                MessageBox.Show($"切换面板时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_layoutConfigPath))
                {
                    File.Delete(_layoutConfigPath);
                }

                // 重新创建默认布局
                var messageResult = MessageBox.Show("重置布局需要重启应用程序。是否现在重启？", 
                                                   "重置布局", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageResult == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(Environment.ProcessPath!);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重置布局失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadData_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadDataFromDirectoryAsync(dialog.FolderName);
            }
        }

        private async Task LoadDataFromDirectoryAsync(string folderPath)
        {
            if (_plugins.Count == 0)
            {
                MessageBox.Show("没有可用的插件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var plugin = _plugins[0];
            _currentPluginName = plugin.Name;
            PluginDataAnchorable.Title = $"{plugin.Name} 数据";

            // 显示进度
            LoadProgress.Visibility = Visibility.Visible;
            StatusBarText.Text = "正在加载数据...";
            StatusText.Text = "加载中...";

            // 清空现有数据
            CommonDataNodes.Clear();
            PluginDataNodes.Clear();
            _commonFileNodeCache.Clear();
            _pluginFileNodeCache.Clear();

            // 清空待处理队列
            while (_pendingFrames.TryDequeue(out _)) { }

            try
            {
                _isLoading = true;
                _batchUpdateTimer.Start();

                // 在后台线程加载数据
                await Task.Run(() => LoadDataWithTreeBuilding(folderPath, plugin));

                // 等待所有待处理帧被处理完
                while (!_pendingFrames.IsEmpty)
                {
                    await Task.Delay(UpdateIntervalMs);
                }

                StatusBarText.Text = $"加载完成: {folderPath}";
                StatusText.Text = "就绪";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusBarText.Text = "加载失败";
                StatusText.Text = "错误";
            }
            finally
            {
                _isLoading = false;
                LoadProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadDataWithTreeBuilding(string folderPath, IProtoPlugin plugin)
        {
            // 调用 Plugin 加载数据，同时获取树节点数据
            if (plugin is IProtoPluginWithTreeData treePlugin)
            {
                treePlugin.LoadWithCallback(folderPath, OnFrameLoaded);
            }
            else
            {
                // 兼容旧接口：加载整个数据后构建树
                var entireData = plugin.Load(folderPath);
                BuildTreeFromEntireData(folderPath, entireData, plugin.Name);
            }
        }

        /// <summary>
        /// 从 EntireProtoData 构建树（兼容旧接口）
        /// </summary>
        private void BuildTreeFromEntireData(string folderPath, EntireProtoData entireData, string pluginName)
        {
            string fileName = Path.GetFileName(folderPath);

            Dispatcher.BeginInvoke(() =>
            {
                // 创建文件根节点
                var commonFileNode = new TreeNodeModel(fileName);
                var pluginFileNode = new TreeNodeModel(fileName);

                CommonDataNodes.Add(commonFileNode);
                PluginDataNodes.Add(pluginFileNode);
                
                _commonFileNodeCache[fileName] = commonFileNode;
                _pluginFileNodeCache[fileName] = pluginFileNode;
            });
        }

        private void OnFrameLoaded(string fileName, int dataId, CommonData? commonData, string? protoJson)
        {
            // 在后台线程构建树节点，减轻 UI 线程负担
            string frameName = $"Frame (DataID: {dataId})";
            TreeNodeModel? commonNode = null;
            TreeNodeModel? pluginNode = null;

            if (commonData != null)
            {
                commonNode = TreeNodeBuilder.BuildFromCommonData(commonData, frameName);
            }

            if (!string.IsNullOrEmpty(protoJson))
            {
                pluginNode = TreeNodeBuilder.BuildFromJson(protoJson, frameName);
            }

            // 将预构建好的节点加入队列
            _pendingFrames.Enqueue(new FrameData(fileName, dataId, commonNode, pluginNode));
        }

        /// <summary>
        /// 查找或创建文件节点
        /// </summary>
        private TreeNodeModel FindOrCreateFileNode(ObservableCollection<TreeNodeModel> nodes, Dictionary<string, TreeNodeModel> cache, string fileName)
        {
            if (cache.TryGetValue(fileName, out var node))
            {
                return node;
            }

            var newNode = new TreeNodeModel(fileName);
            nodes.Add(newNode);
            cache[fileName] = newNode;
            return newNode;
        }

        // 示例：转换逻辑
        public void ConvertData(string sourceFile, IProtoPlugin sourcePlugin, string destFile, IProtoPlugin destPlugin)
        {
            // 1. 使用源插件读取为通用数据
            EntireProtoData commonData = sourcePlugin.Load(sourceFile);

            // 2. 使用目标插件保存通用数据
            //destPlugin.Save(destFile, commonData);
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ApplyTheme(AppTheme.Dark);
            SetAvalonDockTheme(isDark: true);
            MenuDarkTheme.IsChecked = true;
            MenuLightTheme.IsChecked = false;
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ApplyTheme(AppTheme.Light);
            SetAvalonDockTheme(isDark: false);
            MenuLightTheme.IsChecked = true;
            MenuDarkTheme.IsChecked = false;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("MainWindow_Loaded - verifying panel states");
                
                // 延迟执行，确保 AvalonDock 完全初始化
                Dispatcher.BeginInvoke(() =>
                {
                    // 同步菜单状态
                    SyncMenuStates();
                    
                    Debug.WriteLine($"Panel states after load - CommonData visible: {CommonDataAnchorable.IsVisible}, PluginData visible: {PluginDataAnchorable.IsVisible}");
                }, DispatcherPriority.ApplicationIdle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MainWindow_Loaded: {ex.Message}");
            }
        }

        private void SyncMenuStates()
        {
            try
            {
                // 同步菜单状态与面板实际状态
                MenuShowCommonData.IsChecked = CommonDataAnchorable.IsVisible;
                MenuShowPluginData.IsChecked = PluginDataAnchorable.IsVisible;
                
                Debug.WriteLine($"Menu states synced - CommonData: {MenuShowCommonData.IsChecked}, PluginData: {MenuShowPluginData.IsChecked}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing menu states: {ex.Message}");
            }
        }
    }
}