using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ProtoMaster.Models;

/// <summary>
/// 树视图节点模型
/// </summary>
public class TreeNodeModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    private bool _isExpanded;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(); }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set { 
            if (_isExpanded != value)
            {
                _isExpanded = value; 
                OnPropertyChanged();
                // 移除自动同步展开状态功能
                // 现在只更新本节点的展开状态，不影响对应节点
            }
        }
    }

    public ObservableCollection<TreeNodeModel> Children { get; } = new();
    
    /// <summary>
    /// 对应节点（原数据与Common数据之间的对应关系）
    /// </summary>
    public TreeNodeModel? CorrespondingNode { get; set; }
    
    /// <summary>
    /// 节点唯一标识（用于建立对应关系）
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    public TreeNodeModel() { }

    public TreeNodeModel(string name, string value = "")
    {
        Name = name;
        Value = value;
    }
    
    public TreeNodeModel(string name, string value, string nodeId) : this(name, value)
    {
        NodeId = nodeId;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    /// <summary>
    /// 建立与另一个节点的对应关系
    /// </summary>
    public void EstablishCorrespondence(TreeNodeModel correspondingNode)
    {
        CorrespondingNode = correspondingNode;
        correspondingNode.CorrespondingNode = this;
    }
    
    /// <summary>
    /// 递归查找具有指定NodeId的节点
    /// </summary>
    public TreeNodeModel? FindNodeById(string nodeId)
    {
        if (NodeId == nodeId)
            return this;
            
        foreach (var child in Children)
        {
            var found = child.FindNodeById(nodeId);
            if (found != null)
                return found;
        }
        
        return null;
    }
    
    /// <summary>
    /// 展开所有子节点
    /// </summary>
    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }
    
    /// <summary>
    /// 折叠所有子节点
    /// </summary>
    public void CollapseAll()
    {
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
        IsExpanded = false;
    }
    
    /// <summary>
    /// 转换为JSON格式
    /// </summary>
    public string ToJson(bool indented = true)
    {
        var jsonObject = ToJsonObject();
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        return JsonSerializer.Serialize(jsonObject, options);
    }
    
    /// <summary>
    /// 转换为JSON对象
    /// </summary>
    private object ToJsonObject()
    {
        if (Children.Count == 0)
        {
            // 叶子节点，返回值
            return string.IsNullOrEmpty(Value) ? Name : Value;
        }
        
        // 检查是否为数组类型（子节点名称都是 [0], [1], [2] 这种格式）
        var isArray = Children.Count > 0 && 
                      Children.All(c => c.Name.StartsWith("[") && c.Name.EndsWith("]"));
        
        if (isArray)
        {
            // 数组类型
            return Children.Select(c => c.ToJsonObject()).ToArray();
        }
        else
        {
            // 对象类型
            var dict = new Dictionary<string, object>();
            foreach (var child in Children)
            {
                dict[child.Name] = child.ToJsonObject();
            }
            return dict;
        }
    }
}
