using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Google.Protobuf;
using ProtoMaster.Common;
using ProtoMaster.Common.Models;
using ProtoMaster.Plugin.E01.Generated;
using ProtoMaster.PluginInterface;

namespace ProtoMaster.Plugin.E01;

public class E01Plugin : IProtoPluginWithTreeData
{
    public string Name => "E01";
    public string Version => "1.0.0";
    public string Description => "E01 Proto 数据解析器";

    private readonly ProtoDataParser _parser = new();

    public EntireProtoData Load(string filePath)
    {
        Debug.WriteLine("E01Plugin Load method called.");

        var entireData = _parser.ParseDirectory(filePath);
        return entireData;
    }

    /// <summary>
    /// 加载数据并在每帧解析后回调
    /// </summary>
    public void LoadWithCallback(string filePath, Action<string, int, CommonData?, string?> onFrameLoaded)
    {
        Debug.WriteLine("E01Plugin LoadWithCallback method called.");

        if (!Directory.Exists(filePath))
        {
            Debug.WriteLine($"[Parser] Error: Directory {filePath} does not exist.");
            return;
        }

        int binFileCount = _parser.GetFileCount(filePath, ProtoDataParser.BIN_PREFIX);
        int lenFileCount = _parser.GetFileCount(filePath, ProtoDataParser.LEN_PREFIX);

        if (binFileCount != lenFileCount || binFileCount == 0)
        {
            Debug.WriteLine($"[Parser] Error: File count mismatch. BIN:{binFileCount}, LEN:{lenFileCount}");
            return;
        }

        int minIndex = _parser.FindMinFileIndex(filePath);
        if (minIndex == -1) return;

        for (int i = minIndex; i < binFileCount + minIndex; i++)
        {
            string binPath = Path.Combine(filePath, $"{ProtoDataParser.BIN_PREFIX}{i}");
            string lenPath = Path.Combine(filePath, $"{ProtoDataParser.LEN_PREFIX}{i}");

            if (!File.Exists(binPath) || !File.Exists(lenPath)) continue;

            string fileName = $"intelliDriveDataFile{i}";
            var triplets = _parser.ParseLenFile(lenPath);

            using var binReader = new BinaryReader(File.Open(binPath, FileMode.Open));
            
            for (int j = 0; j < triplets.Count; j++)
            {
                var triplet = triplets[j];
                var data = binReader.ReadBytes(triplet.DataLength);

                // 解析并获取 CommonData 和 JSON
                var (commonData, protoJson) = ParseAndConvertWithJson(triplet.DataID, data);

                // 回调通知 UI
                onFrameLoaded(fileName, triplet.DataID, commonData, protoJson);
            }
        }
    }

    /// <summary>
    /// 解析 Proto 数据并返回 CommonData 和 JSON
    /// </summary>
    private (CommonData? Data, string? Json) ParseAndConvertWithJson(int dataId, byte[] data)
    {
        try
        {
            return dataId switch
            {
                26 or 27 or 21 => ParseSRInfo(data),
                _ => (null, null)
            };
        }
        catch
        {
            return (null, null);
        }
    }

    private (CommonData? Data, string? Json) ParseSRInfo(byte[] data)
    {
        try
        {
            var proto = Chery.Ads.Sense.Relity.SR_Info.Parser.ParseFrom(data);
            var commonData = SR_InfoConverter.ToCommon(proto);
            var json = JsonFormatter.Default.Format(proto);
            return (commonData, json);
        }
        catch
        {
            return (null, null);
        }
    }

    public void Save(string filePath, EntireProtoData data)
    {
        Debug.WriteLine("E01Plugin Save method called, but not implemented.");
    }
}