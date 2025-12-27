using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TreeNodeModel> Children { get; } = new();

    public TreeNodeModel() { }

    public TreeNodeModel(string name, string value = "")
    {
        Name = name;
        Value = value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
