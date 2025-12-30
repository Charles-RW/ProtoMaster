using System.Numerics;
using System.Collections.Concurrent;
using System.Collections;
using System.Reflection;
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
    public static TreeNodeModel BuildFromCommonData(CommonData data, string frameName, string? nodeId = null)
    {
        var frameNode = new TreeNodeModel(frameName, "", nodeId ?? "");

        // EgoVehiclePose
        if (data.EgoVehiclePose != null)
        {
            var egoNode = new TreeNodeModel("EgoVehiclePose", "", $"{nodeId}_EgoVehiclePose");
            AddObjectProperties(egoNode, data.EgoVehiclePose, $"{nodeId}_EgoVehiclePose");
            frameNode.Children.Add(egoNode);
        }

        // Obstacles
        if (data.Obstacles?.Obstacles != null && data.Obstacles.Obstacles.Count > 0)
        {
            var obstaclesNode = new TreeNodeModel("Obstacles", $"[{data.Obstacles.Obstacles.Count}]", $"{nodeId}_Obstacles");
            for (int i = 0; i < data.Obstacles.Obstacles.Count; i++)
            {
                var obstacle = data.Obstacles.Obstacles[i];
                var obstacleNode = new TreeNodeModel($"Obstacle_{obstacle.Id}", obstacle.Type.ToString(), $"{nodeId}_Obstacle_{i}");
                AddObjectProperties(obstacleNode, obstacle, $"{nodeId}_Obstacle_{i}");
                obstaclesNode.Children.Add(obstacleNode);
            }
            frameNode.Children.Add(obstaclesNode);
        }

        // LaneLines
        if (data.LaneLines?.LaneLines != null && data.LaneLines.LaneLines.Count > 0)
        {
            var laneLinesNode = new TreeNodeModel("LaneLines", $"[{data.LaneLines.LaneLines.Count}]", $"{nodeId}_LaneLines");
            for (int i = 0; i < data.LaneLines.LaneLines.Count; i++)
            {
                var laneLine = data.LaneLines.LaneLines[i];
                var lineNode = new TreeNodeModel($"LaneLine_{laneLine.lineId}", laneLine.lineType.ToString(), $"{nodeId}_LaneLine_{i}");
                AddObjectProperties(lineNode, laneLine, $"{nodeId}_LaneLine_{i}");
                laneLinesNode.Children.Add(lineNode);
            }
            frameNode.Children.Add(laneLinesNode);
        }

        // RoadMarkers
        if (data.RoadMarkers?.roadMarkerList != null && data.RoadMarkers.roadMarkerList.Count > 0)
        {
            var roadMarkersNode = new TreeNodeModel("RoadMarkers", $"[{data.RoadMarkers.roadMarkerList.Count}]", $"{nodeId}_RoadMarkers");
            for (int i = 0; i < data.RoadMarkers.roadMarkerList.Count; i++)
            {
                var marker = data.RoadMarkers.roadMarkerList[i];
                var markerNode = new TreeNodeModel($"RoadMarker_{marker.roadMarkerID}", marker.roadMarkerType.ToString(), $"{nodeId}_RoadMarker_{i}");
                AddObjectProperties(markerNode, marker, $"{nodeId}_RoadMarker_{i}");
                roadMarkersNode.Children.Add(markerNode);
            }
            frameNode.Children.Add(roadMarkersNode);
        }

        // ParkingSlots
        if (data.SlotList?.parkingSlotList != null && data.SlotList.parkingSlotList.Count > 0)
        {
            var slotsNode = new TreeNodeModel("ParkingSlots", $"[{data.SlotList.parkingSlotList.Count}]", $"{nodeId}_ParkingSlots");
            for (int i = 0; i < data.SlotList.parkingSlotList.Count; i++)
            {
                var slot = data.SlotList.parkingSlotList[i];
                var slotNode = new TreeNodeModel($"Slot_{slot.slotID}", slot.slotType.ToString(), $"{nodeId}_Slot_{i}");
                AddObjectProperties(slotNode, slot, $"{nodeId}_Slot_{i}");
                slotsNode.Children.Add(slotNode);
            }
            frameNode.Children.Add(slotsNode);
        }

        // StateInfo
        if (data.StateInfo != null)
        {
            var stateNode = new TreeNodeModel("StateInfo", "", $"{nodeId}_StateInfo");
            AddObjectProperties(stateNode, data.StateInfo, $"{nodeId}_StateInfo");
            frameNode.Children.Add(stateNode);
        }

        return frameNode;
    }

    /// <summary>
    /// 从 JSON 字符串构建树节点（用于 Proto 原始数据）
    /// </summary>
    public static TreeNodeModel BuildFromJson(string json, string frameName, string? nodeId = null)
    {
        var frameNode = new TreeNodeModel(frameName, "", nodeId ?? "");
        
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            AddJsonElement(frameNode, doc.RootElement, nodeId ?? "");
        }
        catch
        {
            frameNode.Children.Add(new TreeNodeModel("Error", "无法解析 JSON"));
        }

        return frameNode;
    }

    private static void AddJsonElement(TreeNodeModel parent, System.Text.Json.JsonElement element, string parentId)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childId = $"{parentId}_{property.Name}";
                    var childNode = new TreeNodeModel(property.Name, "", childId);
                    if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Object ||
                        property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        AddJsonElement(childNode, property.Value, childId);
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
                    var itemId = $"{parentId}_{index}";
                    var itemNode = new TreeNodeModel($"[{index}]", "", itemId);
                    if (item.ValueKind == System.Text.Json.JsonValueKind.Object ||
                        item.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        AddJsonElement(itemNode, item, itemId);
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

    private static void AddObjectProperties(TreeNodeModel parent, object obj, string parentId)
    {
        if (obj == null) return;

        // Handle collections specially to iterate items instead of properties
        if (obj is IList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var itemStr = FormatValue(item);
                var itemId = $"{parentId}_{i}";
                
                if (item != null && IsComplexType(item.GetType()) && !IsSimpleDisplayType(item.GetType()))
                {
                    var childNode = new TreeNodeModel($"[{i}]", itemStr, itemId);
                    AddObjectProperties(childNode, item, itemId);
                    parent.Children.Add(childNode);
                }
                else
                {
                    parent.Children.Add(new TreeNodeModel($"[{i}]", itemStr, itemId));
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
                var propId = $"{parentId}_{prop.Name}";
                
                if (value != null && IsComplexType(prop.PropertyType) && !IsSimpleDisplayType(prop.PropertyType))
                {
                    var childNode = new TreeNodeModel(prop.Name, valueStr, propId);
                    AddObjectProperties(childNode, value, propId);
                    parent.Children.Add(childNode);
                }
                else
                {
                    parent.Children.Add(new TreeNodeModel(prop.Name, valueStr, propId));
                }
            }
            catch
            {
                parent.Children.Add(new TreeNodeModel(prop.Name, "<error>", $"{parentId}_{prop.Name}"));
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
            IList list => $"[{list.Count}]",
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
