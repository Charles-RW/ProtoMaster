using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using ProtoMaster.Common;
using ProtoMaster.Plugin.E01.Generated;

namespace ProtoMaster.Plugin.E01;

public class ProtoDataParser
{
    public const string BIN_PREFIX = "intelliDriveDataFile";
    public const string LEN_PREFIX = "intelliDriveDataLenFile";

    public EntireProtoData ParseDirectory(string filePath)
    {
        EntireProtoData srData = new EntireProtoData();

        if (!Directory.Exists(filePath))
        {
            Console.WriteLine($"[Parser] Error: Directory {filePath} does not exist.");
            return srData;
        }

        foreach (var (index, fileData) in ParseDirectoryLazy(filePath))
        {
            srData.FilesData[index] = fileData;
        }

        return srData;
    }

    public IEnumerable<(int Index, FileData Data)> ParseDirectoryLazy(string filePath)
    {
        if (!Directory.Exists(filePath))
        {
            yield break;
        }

        int binFileCount = GetFileCount(filePath, BIN_PREFIX);
        int lenFileCount = GetFileCount(filePath, LEN_PREFIX);

        if (binFileCount != lenFileCount || binFileCount == 0)
        {
            Console.WriteLine($"[Parser] Error: File count mismatch. BIN:{binFileCount}, LEN:{lenFileCount}");
            yield break;
        }

        int minIndex = FindMinFileIndex(filePath);
        if (minIndex == -1) yield break;

        for (int i = minIndex; i < binFileCount + minIndex; i++)
        {
            string binPath = Path.Combine(filePath, $"{BIN_PREFIX}{i}");
            string lenPath = Path.Combine(filePath, $"{LEN_PREFIX}{i}");

            if (!File.Exists(binPath) || !File.Exists(lenPath)) continue;

            FileData fileData = ParseFile(binPath, lenPath);
            yield return (i, fileData);
        }
    }

    public FileData ParseFile(string binPath, string lenPath)
    {
        FileData fileData = new FileData();
        List<Triplet> triplets = ParseLenFile(lenPath);

        using (BinaryReader binReader = new BinaryReader(File.Open(binPath, FileMode.Open)))
        {
            for (int j = 0; j < triplets.Count; j++)
            {
                Frame frame = new Frame
                {
                    TripletInfo = triplets[j],
                    Data = binReader.ReadBytes(triplets[j].DataLength)
                };

                frame.CommonData = DataIdRouter.ParseAndConvert(
                    triplets[j].DataID,
                    frame.Data
                );

                fileData.Frames[j] = frame;
            }
        }
        return fileData;
    }

    public int GetFileCount(string directory, string prefix)
    {
        return Directory.GetFiles(directory, $"{prefix}*").Length;
    }

    public int FindMinFileIndex(string directory)
    {
        int minIndex = int.MaxValue;
        foreach (var file in Directory.GetFiles(directory, $"{BIN_PREFIX}*"))
        {
            var fileName = Path.GetFileName(file);
            if (int.TryParse(fileName.Substring(BIN_PREFIX.Length), out int index))
            {
                minIndex = Math.Min(minIndex, index);
            }
        }
        return minIndex == int.MaxValue ? -1 : minIndex;
    }

    public List<Triplet> ParseLenFile(string lenPath)
    {
        var triplets = new List<Triplet>();

        if (!File.Exists(lenPath))
            return triplets;

        foreach (var rawLine in File.ReadLines(lenPath))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var line = rawLine.Trim();
            var parts = line.Split(',');

            // 需求：逗号分隔，第1个元素为 Timestamp，第3个元素为 DataID，第4个元素为 DataLength
            if (parts.Length < 4)
                continue;

            // 解析 Timestamp（示例格式：2025-12-20-16:46:33:522 -> yyyy-MM-dd-HH:mm:ss:fff）
            ulong timestamp = 0;
            var tsStr = parts[0].Trim();
            if (DateTime.TryParseExact(tsStr, "yyyy-MM-dd-HH:mm:ss:fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
                || DateTime.TryParse(tsStr, out dt))
            {
                timestamp = (ulong)new DateTimeOffset(dt).ToUnixTimeMilliseconds();
            }

            // 解析 DataID 和 DataLength（第3、第4项）
            if (!int.TryParse(parts[2].Trim(), out var dataId))
                continue;
            if (!int.TryParse(parts[3].Trim(), out var dataLength))
                continue;

            triplets.Add(new Triplet
            {
                DataID = dataId,
                DataLength = dataLength,
                Timestamp = timestamp
            });
        }

        return triplets;
    }
}
