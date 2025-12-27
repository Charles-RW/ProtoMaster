using System.Numerics;
using System.Reflection;
using System.Collections.Concurrent;
using ProtoMaster.Common;
using ProtoMaster.Common.Models;
using ProtoMaster.Models;

namespace ProtoMaster.Services;

/// <summary>
/// 将 CommonData 转换为树节点的服务
/// </summary>
public static class TreeNodeBuilder
{
    // Cache for PropertyInfo to avoid repeated reflection costs
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    /// <summary>
    /// 从 CommonData 构建树节点
    /// </summary>
    public static TreeNodeModel BuildFromCommonData(CommonData data, string frameName)
    {
        var frameNode = new TreeNodeModel(frameName);

        // EgoVehiclePose
        if (data.EgoVehiclePose != null)
        {
            var egoNode = new TreeNodeModel("EgoVehiclePose");
            AddObjectProperties(egoNode, data.EgoVehiclePose);
            frameNode.Children.Add(egoNode);
        }

        // Obstacles
        if (data.Obstacles?.Obstacles != null && data.Obstacles.Obstacles.Count > 0)
        {
            var obstaclesNode = new TreeNodeModel("Obstacles", $"[{data.Obstacles.Obstacles.Count}]");
            foreach (var obstacle in data.Obstacles.Obstacles)
            {
                var obstacleNode = new TreeNodeModel($"Obstacle_{obstacle.Id}", obstacle.Type.ToString());
                AddObjectProperties(obstacleNode, obstacle);
                obstaclesNode.Children.Add(obstacleNode);
            }
            frameNode.Children.Add(obstaclesNode);
        }

        // LaneLines
        if (data.LaneLines?.LaneLines != null && data.LaneLines.LaneLines.Count > 0)
        {
            var laneLinesNode = new TreeNodeModel("LaneLines", $"[{data.LaneLines.LaneLines.Count}]");
            foreach (var laneLine in data.LaneLines.LaneLines)
            {
                var lineNode = new TreeNodeModel($"LaneLine_{laneLine.lineId}", laneLine.lineType.ToString());
                AddObjectProperties(lineNode, laneLine);
                laneLinesNode.Children.Add(lineNode);
            }
            frameNode.Children.Add(laneLinesNode);
        }

        // RoadMarkers
        if (data.RoadMarkers?.roadMarkerList != null && data.RoadMarkers.roadMarkerList.Count > 0)
        {
            var roadMarkersNode = new TreeNodeModel("RoadMarkers", $"[{data.RoadMarkers.roadMarkerList.Count}]");
            foreach (var marker in data.RoadMarkers.roadMarkerList)
            {
                var markerNode = new TreeNodeModel($"RoadMarker_{marker.roadMarkerID}", marker.roadMarkerType.ToString());
                AddObjectProperties(markerNode, marker);
                roadMarkersNode.Children.Add(markerNode);
            }
            frameNode.Children.Add(roadMarkersNode);
        }

        // ParkingSlots
        if (data.SlotList?.parkingSlotList != null && data.SlotList.parkingSlotList.Count > 0)
        {
            var slotsNode = new TreeNodeModel("ParkingSlots", $"[{data.SlotList.parkingSlotList.Count}]");
            foreach (var slot in data.SlotList.parkingSlotList)
            {
                var slotNode = new TreeNodeModel($"Slot_{slot.slotID}", slot.slotType.ToString());
                AddObjectProperties(slotNode, slot);
                slotsNode.Children.Add(slotNode);
            }
            frameNode.Children.Add(slotsNode);
        }

        // StateInfo
        if (data.StateInfo != null)
        {
            var stateNode = new TreeNodeModel("StateInfo");
            AddObjectProperties(stateNode, data.StateInfo);
            frameNode.Children.Add(stateNode);
        }

        return frameNode;
    }

    /// <summary>
    /// 从 JSON 字符串构建树节点（用于 Proto 原始数据）
    /// </summary>
    public static TreeNodeModel BuildFromJson(string json, string frameName)
    {
        var frameNode = new TreeNodeModel(frameName);
        
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            AddJsonElement(frameNode, doc.RootElement);
        }
        catch
        {
            frameNode.Children.Add(new TreeNodeModel("Error", "无法解析 JSON"));
        }

        return frameNode;
    }

    private static void AddJsonElement(TreeNodeModel parent, System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childNode = new TreeNodeModel(property.Name);
                    if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Object ||
                        property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        AddJsonElement(childNode, property.Value);
                    }
                    else
                    {
                        childNode.Value = property.Value.ToString();
                    }
                    parent.Children.Add(childNode);
                }
                break;

            case System.Text.Json.JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var itemNode = new TreeNodeModel($"[{index}]");
                    if (item.ValueKind == System.Text.Json.JsonValueKind.Object ||
                        item.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        AddJsonElement(itemNode, item);
                    }
                    else
                    {
                        itemNode.Value = item.ToString();
                    }
                    parent.Children.Add(itemNode);
                    index++;
                }
                break;
        }
    }

    private static void AddObjectProperties(TreeNodeModel parent, object obj)
    {
        if (obj == null) return;

        // Handle collections specially to iterate items instead of properties
        if (obj is System.Collections.IList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var itemStr = FormatValue(item);
                
                if (item != null && IsComplexType(item.GetType()) && !IsSimpleDisplayType(item.GetType()))
                {
                    var childNode = new TreeNodeModel($"[{i}]", itemStr);
                    AddObjectProperties(childNode, item);
                    parent.Children.Add(childNode);
                }
                else
                {
                    parent.Children.Add(new TreeNodeModel($"[{i}]", itemStr));
                }
            }
            return;
        }

        var type = obj.GetType();
        
        // Use cached properties
        if (!_propertyCache.TryGetValue(type, out var properties))
        {
            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _propertyCache[type] = properties;
        }

        foreach (var prop in properties)
        {
            // Skip indexer properties (like Item[]) to avoid TargetParameterCountException
            if (prop.GetIndexParameters().Length > 0)
                continue;

            try
            {
                var value = prop.GetValue(obj);
                var valueStr = FormatValue(value);
                
                if (value != null && IsComplexType(prop.PropertyType) && !IsSimpleDisplayType(prop.PropertyType))
                {
                    var childNode = new TreeNodeModel(prop.Name, valueStr);
                    AddObjectProperties(childNode, value);
                    parent.Children.Add(childNode);
                }
                else
                {
                    parent.Children.Add(new TreeNodeModel(prop.Name, valueStr));
                }
            }
            catch
            {
                parent.Children.Add(new TreeNodeModel(prop.Name, "<error>"));
            }
        }
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "<null>";

        return value switch
        {
            Vector3 v => $"({v.X:F2}, {v.Y:F2}, {v.Z:F2})",
            double d => d.ToString("F4"),
            float f => f.ToString("F4"),
            Enum e => e.ToString(),
            System.Collections.IList list => $"[{list.Count}]",
            _ => value.ToString() ?? "<null>"
        };
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive 
               && type != typeof(string) 
               && type != typeof(decimal)
               && !type.IsEnum;
    }

    private static bool IsSimpleDisplayType(Type type)
    {
        return type == typeof(Vector3) 
               || type == typeof(DateTime) 
               || type == typeof(TimeSpan);
    }
}
