using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        // 节点对应关系缓存
        private readonly Dictionary<string, TreeNodeModel> _nodeIdToCommonNodeMap = new();
        private readonly Dictionary<string, TreeNodeModel> _nodeIdToPluginNodeMap = new();

        // AvalonDock 布局保存/恢复
        private string _layoutConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "ProtoMaster", "layout.config");

        // 帧数据结构
        private record FrameData(string FileName, int DataId, TreeNodeModel? CommonNode, TreeNodeModel? PluginNode);

        // 防止递归选择的标志
        private bool _isSelectingCorrespondingNode = false;

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

            // 同步主题菜单状态
            SyncThemeMenuStates();
            
            // 监听主题变化事件
            ThemeManager.ThemeChanged += OnThemeChanged;

            // 设置 AvalonDock 主题
            SetAvalonDockTheme(ThemeManager.CurrentTheme == AppTheme.Dark);

            LoadPlugins();
            
            // 添加面板状态变化事件监听
            SetupPanelEventHandlers();
            
            LoadLayout();
            
            // 在窗口加载完成后确保布局正确
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// 主题变化事件处理
        /// </summary>
        private void OnThemeChanged(object? sender, AppTheme theme)
        {
            // 同步主题菜单状态
            SyncThemeMenuStates();
            
            // 更新 AvalonDock 主题
            SetAvalonDockTheme(theme == AppTheme.Dark);
        }

        /// <summary>
        /// 同步主题菜单状态
        /// </summary>
        private void SyncThemeMenuStates()
        {
            var isDarkTheme = ThemeManager.CurrentTheme == AppTheme.Dark;
            MenuDarkTheme.IsChecked = isDarkTheme;
            MenuLightTheme.IsChecked = !isDarkTheme;
        }

        protected override void OnClosed(EventArgs e)
        {
            // 取消主题变化事件监听
            ThemeManager.ThemeChanged -= OnThemeChanged;
            
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

                        // 把序列化器里要求的 content 显示到当前窗口中"真实"的 UI 内容实例（UserControl / Grid 等）
                        // 避免序列化器创建一个新的 LayoutAnchorable 包裹一个新的 Content 实例，
                        // 从而导致运行时出现两个相同功能的 Anchorable（UI 与代码引用不一致）
                        switch (e.Model.ContentId)
                        {
                            case "CommonData":
                                e.Content = CommonDataAnchorable.Content;
                                e.Cancel = false;
                                Debug.WriteLine("Restored CommonData content (mapped to existing content)");
                                break;
                            case "PluginData":
                                e.Content = PluginDataAnchorable.Content;
                                e.Cancel = false;
                                Debug.WriteLine("Restored PluginData content (mapped to existing content)");
                                break;
                            default:
                                Debug.WriteLine($"Unknown content id: {e.Model.ContentId}");
                                break;
                        }
                    };

                    using var reader = new StreamReader(_layoutConfigPath);
                    layoutSerializer.Deserialize(reader);
                    Debug.WriteLine("Layout deserialized successfully");

                    var anchorables = DockingManager.Layout.Descendents().OfType<LayoutAnchorable>().ToList();

                    // 处理 CommonData
                    var commonGroup = anchorables.Where(a => a.ContentId == "CommonData").ToList();
                    if (commonGroup.Count > 0)
                    {
                        // 尝试找到已正确引用我们 content 的 model，否则选第一个作为保留者并替换其 Content
                        var keeper = commonGroup.FirstOrDefault(a => ReferenceEquals(a.Content, CommonDataAnchorable.Content))
                                    ?? commonGroup[0];
                        // 确保 keeper 使用真实 content
                        //keeper.Content = CommonDataAnchorable.Content;
                        CommonDataAnchorable = keeper;
                        // 删除其余重复 model
                        foreach (var extra in commonGroup.Where(a => !ReferenceEquals(a, keeper)).ToList())
                        {
                            if (extra.Parent is LayoutAnchorablePane parentPane)
                                parentPane.Children.Remove(extra);
                        }
                    }

                    // 处理 PluginData（同上）
                    var pluginGroup = anchorables.Where(a => a.ContentId == "PluginData").ToList();
                    if (pluginGroup.Count > 0)
                    {
                        var keeper = pluginGroup.FirstOrDefault(a => ReferenceEquals(a.Content, PluginDataAnchorable.Content))
                                    ?? pluginGroup[0];
                        PluginDataAnchorable = keeper;
                        foreach (var extra in pluginGroup.Where(a => !ReferenceEquals(a, keeper)).ToList())
                        {
                            if (extra.Parent is LayoutAnchorablePane parentPane)
                                parentPane.Children.Remove(extra);
                        }
                    }

                    // 同步菜单状态，确保 UI 与菜单一致
                    Dispatcher.BeginInvoke(() => SyncMenuStates(), DispatcherPriority.ApplicationIdle);
                }
                else
                {
                    Debug.WriteLine("No layout config file found, using default layout");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load layout: {ex.Message}");
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
                // 建立节点映射
                RegisterNodeMapping(frame.CommonNode, isCommonNode: true);
            }

            // 添加 Proto JSON 数据节点
            if (frame.PluginNode != null)
            {
                pluginFileNode.Children.Add(frame.PluginNode);
                // 建立节点映射
                RegisterNodeMapping(frame.PluginNode, isCommonNode: false);
            }

            // 建立对应关系
            if (frame.CommonNode != null && frame.PluginNode != null)
            {
                EstablishNodeCorrespondence(frame.CommonNode, frame.PluginNode);
            }
        }

        /// <summary>
        /// 注册节点到映射表
        /// </summary>
        private void RegisterNodeMapping(TreeNodeModel node, bool isCommonNode)
        {
            if (!string.IsNullOrEmpty(node.NodeId))
            {
                if (isCommonNode)
                {
                    _nodeIdToCommonNodeMap[node.NodeId] = node;
                }
                else
                {
                    _nodeIdToPluginNodeMap[node.NodeId] = node;
                }
            }

            // 递归注册子节点
            foreach (var child in node.Children)
            {
                RegisterNodeMapping(child, isCommonNode);
            }
        }

        /// <summary>
        /// 建立节点间的对应关系（递归）
        /// </summary>
        private void EstablishNodeCorrespondence(TreeNodeModel commonNode, TreeNodeModel pluginNode)
        {
            // 建立直接对应关系
            if (!string.IsNullOrEmpty(commonNode.NodeId) && commonNode.NodeId == pluginNode.NodeId)
            {
                commonNode.EstablishCorrespondence(pluginNode);
            }

            // 递归处理子节点 - 按照名称匹配
            var commonChildren = commonNode.Children.ToList();
            var pluginChildren = pluginNode.Children.ToList();

            foreach (var commonChild in commonChildren)
            {
                var correspondingPluginChild = pluginChildren.FirstOrDefault(p => 
                    p.NodeId == commonChild.NodeId || 
                    (string.IsNullOrEmpty(commonChild.NodeId) && p.Name == commonChild.Name));
                
                if (correspondingPluginChild != null)
                {
                    EstablishNodeCorrespondence(commonChild, correspondingPluginChild);
                }
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
            _nodeIdToCommonNodeMap.Clear();
            _nodeIdToPluginNodeMap.Clear();

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
            string nodeId = $"{fileName}_{dataId}"; // 生成唯一的节点ID
            
            TreeNodeModel? commonNode = null;
            TreeNodeModel? pluginNode = null;

            if (commonData != null)
            {
                commonNode = TreeNodeBuilder.BuildFromCommonData(commonData, frameName, nodeId);
            }

            if (!string.IsNullOrEmpty(protoJson))
            {
                pluginNode = TreeNodeBuilder.BuildFromJson(protoJson, frameName, nodeId);
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
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.ApplyTheme(AppTheme.Light);
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

        #region TreeView Utility Methods

        /// <summary>
        /// 选择并展开指定节点及其父节点路径
        /// </summary>
        private void SelectAndExpandNode(TreeView treeView, TreeNodeModel targetNode)
        {
            // 获取节点到根的路径
            var pathToRoot = GetPathToRoot(targetNode);
            
            // 从根开始展开路径上的所有节点
            TreeViewItem? currentContainer = null;
            
            for (int i = pathToRoot.Count - 1; i >= 0; i--)
            {
                var nodeInPath = pathToRoot[i];
                
                if (currentContainer == null)
                {
                    // 根节点
                    currentContainer = treeView.ItemContainerGenerator.ContainerFromItem(nodeInPath) as TreeViewItem;
                }
                else
                {
                    // 子节点
                    currentContainer.IsExpanded = true;
                    currentContainer.UpdateLayout(); // 确保子容器已生成
                    currentContainer = currentContainer.ItemContainerGenerator.ContainerFromItem(nodeInPath) as TreeViewItem;
                }
                
                if (currentContainer != null)
                {
                    currentContainer.IsExpanded = true;
                    
                    // 如果这是目标节点，选择它
                    if (nodeInPath == targetNode)
                    {
                        currentContainer.IsSelected = true;
                        currentContainer.BringIntoView();
                    }
                }
            }
        }

        /// <summary>
        /// 获取节点到根的路径
        /// </summary>
        private List<TreeNodeModel> GetPathToRoot(TreeNodeModel node)
        {
            var path = new List<TreeNodeModel>();
            var current = node;
            
            // 添加当前节点
            path.Add(current);
            
            // 查找父节点路径
            var parentNode = FindParentNode(current);
            while (parentNode != null)
            {
                path.Add(parentNode);
                parentNode = FindParentNode(parentNode);
            }
            
            return path;
        }

        /// <summary>
        /// 查找指定节点的父节点
        /// </summary>
        private TreeNodeModel? FindParentNode(TreeNodeModel childNode)
        {
            // 在Common数据中查找
            foreach (var rootNode in CommonDataNodes)
            {
                var parent = FindParentNodeRecursive(rootNode, childNode);
                if (parent != null) return parent;
            }
            
            // 在Plugin数据中查找
            foreach (var rootNode in PluginDataNodes)
            {
                var parent = FindParentNodeRecursive(rootNode, childNode);
                if (parent != null) return parent;
            }
            
            return null;
        }

        /// <summary>
        /// 递归查找父节点
        /// </summary>
        private TreeNodeModel? FindParentNodeRecursive(TreeNodeModel currentNode, TreeNodeModel targetChild)
        {
            // 检查当前节点的直接子节点
            foreach (var child in currentNode.Children)
            {
                if (child == targetChild)
                    return currentNode;
                    
                // 递归查找
                var found = FindParentNodeRecursive(child, targetChild);
                if (found != null)
                    return found;
            }
            
            return null;
        }

        #endregion

        #region TreeView Right-Click Context Menu Events

        /// <summary>
        /// 同步到对应节点
        /// </summary>
        private void SyncToCorrespondingNode_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var treeViewItem = contextMenu?.PlacementTarget as FrameworkElement;
            
            Debug.WriteLine($"SyncToCorrespondingNode_Click: menuItem={menuItem?.Header}, PlacementTarget={treeViewItem?.GetType().Name}");
            
            // 通过可视化树查找 TreeViewItem
            while (treeViewItem != null && !(treeViewItem is TreeViewItem))
            {
                treeViewItem = treeViewItem.Parent as FrameworkElement;
                if (treeViewItem == null)
                {
                    // 如果Parent为null，尝试使用VisualTreeHelper
                    if (contextMenu?.PlacementTarget != null)
                    {
                        treeViewItem = System.Windows.Media.VisualTreeHelper.GetParent(contextMenu.PlacementTarget) as FrameworkElement;
                        while (treeViewItem != null && !(treeViewItem is TreeViewItem))
                        {
                            treeViewItem = System.Windows.Media.VisualTreeHelper.GetParent(treeViewItem) as FrameworkElement;
                        }
                    }
                    break;
                }
            }
            
            Debug.WriteLine($"Found TreeViewItem: {treeViewItem?.GetType().Name}, DataContext: {(treeViewItem as TreeViewItem)?.DataContext?.GetType().Name}");
            
            if (treeViewItem is TreeViewItem item && item.DataContext is TreeNodeModel node)
            {
                try
                {
                    Debug.WriteLine($"Processing node: '{node.Name}', HasCorrespondingNode: {node.CorrespondingNode != null}");
                    
                    if (node.CorrespondingNode != null)
                    {
                        // 查找当前节点属于哪个TreeView
                        var currentTreeView = FindAncestorTreeView(item);
                        Debug.WriteLine($"Current TreeView: {currentTreeView?.Name ?? "null"}");
                        
                        TreeView? targetTreeView = null;
                        
                        // 通过名称判断当前TreeView
                        if (currentTreeView?.Name == "CommonDataTreeView")
                        {
                            targetTreeView = this.FindName("PluginDataTreeView") as TreeView;
                            Debug.WriteLine("Switching from CommonData to PluginData");
                        }
                        else if (currentTreeView?.Name == "PluginDataTreeView")
                        {
                            targetTreeView = this.FindName("CommonDataTreeView") as TreeView;
                            Debug.WriteLine("Switching from PluginData to CommonData");
                        }
                        else
                        {
                            // 如果无法确定当前TreeView，尝试智能推断
                            Debug.WriteLine("Unable to determine current TreeView, attempting smart inference");
                            
                            // 检查节点是否在Common数据中
                            bool isInCommonData = IsNodeInCollection(node, CommonDataNodes);
                            bool isInPluginData = IsNodeInCollection(node, PluginDataNodes);
                            
                            if (isInCommonData)
                            {
                                targetTreeView = this.FindName("PluginDataTreeView") as TreeView;
                                Debug.WriteLine("Node found in CommonData, targeting PluginData");
                            }
                            else if (isInPluginData)
                            {
                                targetTreeView = this.FindName("CommonDataTreeView") as TreeView;
                                Debug.WriteLine("Node found in PluginData, targeting CommonData");
                            }
                        }
                        
                        Debug.WriteLine($"Target TreeView: {targetTreeView?.Name ?? "null"}");
                        
                        if (targetTreeView != null)
                        {
                            _isSelectingCorrespondingNode = true;
                            try
                            {
                                // 选择并展开对应节点
                                SelectAndExpandNode(targetTreeView, node.CorrespondingNode);
                                StatusBarText.Text = $"已同步到对应节点: '{node.CorrespondingNode.Name}'";
                                Debug.WriteLine($"Successfully synced to corresponding node: '{node.CorrespondingNode.Name}'");
                            }
                            finally
                            {
                                _isSelectingCorrespondingNode = false;
                            }
                        }
                        else
                        {
                            StatusBarText.Text = "无法确定目标TreeView";
                            Debug.WriteLine("Unable to determine target TreeView");
                        }
                    }
                    else
                    {
                        StatusBarText.Text = $"节点 '{node.Name}' 没有对应节点";
                        Debug.WriteLine($"Node '{node.Name}' has no corresponding node");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error syncing to corresponding node: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    StatusBarText.Text = "同步节点时出错";
                }
            }
            else
            {
                Debug.WriteLine($"Unable to find TreeViewItem or TreeNodeModel. TreeViewItem: {treeViewItem}, DataContext: {(treeViewItem as TreeViewItem)?.DataContext}");
                StatusBarText.Text = "无法找到选择的节点";
            }
        }

        /// <summary>
        /// 查找父级TreeView控件 - 改进版，支持模板化和复杂可视化树
        /// </summary>
        private TreeView? FindAncestorTreeView(FrameworkElement element)
        {
            // 先尝试使用 Parent 属性遍历逻辑树
            var parent = element.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent is TreeView treeView)
                    return treeView;
                parent = parent.Parent as FrameworkElement;
            }

            // 如果逻辑树中找不到，使用 VisualTreeHelper 遍历可视化树
            DependencyObject visualParent = System.Windows.Media.VisualTreeHelper.GetParent(element);
            while (visualParent != null)
            {
                if (visualParent is TreeView visualTreeView)
                    return visualTreeView;
                visualParent = System.Windows.Media.VisualTreeHelper.GetParent(visualParent);
            }

            // 最后尝试直接比较名称（作为备选方案）
            return TryFindTreeViewByName(element);
        }

        /// <summary>
        /// 通过名称查找TreeView（备选方案）
        /// </summary>
        private TreeView? TryFindTreeViewByName(FrameworkElement contextElement)
        {
            // 如果上下文菜单位于PluginDataTreeView的内容中，直接返回对应的TreeView
            try
            {
                // 检查是否能通过名称找到TreeView
                var commonTreeView = this.FindName("CommonDataTreeView") as TreeView;
                var pluginTreeView = this.FindName("PluginDataTreeView") as TreeView;

                // 如果找到了TreeView，检查contextElement是否在其中
                if (commonTreeView != null && IsElementInTreeView(contextElement, commonTreeView))
                {
                    Debug.WriteLine("Found CommonDataTreeView via name lookup");
                    return commonTreeView;
                }

                if (pluginTreeView != null && IsElementInTreeView(contextElement, pluginTreeView))
                {
                    Debug.WriteLine("Found PluginDataTreeView via name lookup");
                    return pluginTreeView;
                }

                // 如果以上都找不到，返回第一个找到的TreeView作为默认值
                Debug.WriteLine("Fallback: returning CommonDataTreeView as default");
                return commonTreeView ?? pluginTreeView;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in TryFindTreeViewByName: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查元素是否在指定的TreeView中
        /// </summary>
        private bool IsElementInTreeView(FrameworkElement element, TreeView treeView)
        {
            try
            {
                DependencyObject current = element;
                while (current != null)
                {
                    if (ReferenceEquals(current, treeView))
                        return true;
                    current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 展开所有子节点
        /// </summary>
        private void ExpandAllNodes_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var treeViewItem = contextMenu?.PlacementTarget as FrameworkElement;
            
            // 通过可视化树查找 TreeViewItem
            while (treeViewItem != null && !(treeViewItem is TreeViewItem))
            {
                treeViewItem = treeViewItem.Parent as FrameworkElement;
            }
            
            if (treeViewItem is TreeViewItem item && item.DataContext is TreeNodeModel node)
            {
                try
                {
                    node.ExpandAll();
                    StatusBarText.Text = $"已展开 '{node.Name}' 的所有子节点";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error expanding all nodes: {ex.Message}");
                    StatusBarText.Text = "展开节点时出错";
                }
            }
        }

        /// <summary>
        /// 折叠所有子节点
        /// </summary>
        private void CollapseAllNodes_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var treeViewItem = contextMenu?.PlacementTarget as FrameworkElement;
            
            // 通过可视化树查找 TreeViewItem
            while (treeViewItem != null && !(treeViewItem is TreeViewItem))
            {
                treeViewItem = treeViewItem.Parent as FrameworkElement;
            }
            
            if (treeViewItem is TreeViewItem item && item.DataContext is TreeNodeModel node)
            {
                try
                {
                    node.CollapseAll();
                    StatusBarText.Text = $"已折叠 '{node.Name}' 的所有子节点";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error collapsing all nodes: {ex.Message}");
                    StatusBarText.Text = "折叠节点时出错";
                }
            }
        }

        /// <summary>
        /// 复制节点数据为JSON格式
        /// </summary>
        private void CopyNodeAsJson_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var treeViewItem = contextMenu?.PlacementTarget as FrameworkElement;
            
            // 通过可视化树查找 TreeViewItem
            while (treeViewItem != null && !(treeViewItem is TreeViewItem))
            {
                treeViewItem = treeViewItem.Parent as FrameworkElement;
            }
            
            if (treeViewItem is TreeViewItem item && item.DataContext is TreeNodeModel node)
            {
                try
                {
                    var json = node.ToJson(indented: true);
                    Clipboard.SetText(json);
                    StatusBarText.Text = $"已复制 '{node.Name}' 节点数据到剪贴板 ({json.Length} 字符)";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error copying node as JSON: {ex.Message}");
                    StatusBarText.Text = "复制节点数据时出错";
                    MessageBox.Show($"复制节点数据时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// PluginDataTreeView选择项变化事件处理程序
        /// </summary>
        private void PluginDataTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // 防止递归触发
            if (_isSelectingCorrespondingNode)
                return;

            if (e.NewValue is TreeNodeModel selectedNode)
            {
                try
                {
                    // 如果当前选择的节点有对应节点，可以实现自动同步功能
                    if (selectedNode.CorrespondingNode != null)
                    {
                        // 这里可以添加自动同步逻辑，比如高亮对应节点
                        // 或者在状态栏显示对应节点信息
                        StatusBarText.Text = $"选择节点: '{selectedNode.Name}' (有对应节点: '{selectedNode.CorrespondingNode.Name}')";
                    }
                    else
                    {
                        StatusBarText.Text = $"选择节点: '{selectedNode.Name}' (无对应节点)";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in PluginDataTreeView_SelectedItemChanged: {ex.Message}");
                    StatusBarText.Text = "处理选择项变化时出错";
                }
            }
        }

        #endregion

        /// <summary>
        /// 判断节点是否属于指定集合（递归查找）
        /// </summary>
        private bool IsNodeInCollection(TreeNodeModel node, ObservableCollection<TreeNodeModel> collection)
        {
            foreach (var root in collection)
            {
                if (IsNodeInSubtree(node, root))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 判断节点是否在以root为根的子树中
        /// </summary>
        private bool IsNodeInSubtree(TreeNodeModel target, TreeNodeModel root)
        {
            if (ReferenceEquals(target, root))
                return true;
            foreach (var child in root.Children)
            {
                if (IsNodeInSubtree(target, child))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 设置面板相关事件处理程序（如有需要可扩展）
        /// </summary>
        private void SetupPanelEventHandlers()
        {
            // 示例：可在此注册面板的事件监听，如 IsVisibleChanged、IsActiveChanged 等
        }
    }
}