using System;
using ProtoMaster.Common;
using ProtoMaster.Common.Models;
using System.Collections.Generic;

namespace ProtoMaster.PluginInterface;

/// <summary>
/// 基础插件接口
/// </summary>
public interface IProtoPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }

    EntireProtoData Load(string filePath);
    void Save(string filePath, EntireProtoData data);
}

/// <summary>
/// 扩展的插件接口，支持加载时回调以填充树视图
/// </summary>
public interface IProtoPluginWithTreeData : IProtoPlugin
{
    /// <summary>
    /// 加载数据并在每帧解析后回调
    /// </summary>
    /// <param name="filePath">数据目录路径</param>
    /// <param name="onFrameLoaded">回调函数 (文件名, DataID, CommonData, ProtoJson)</param>
    void LoadWithCallback(string filePath, Action<string, int, CommonData?, string?> onFrameLoaded);
}