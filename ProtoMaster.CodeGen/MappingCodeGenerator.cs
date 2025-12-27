using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using ProtoMaster.CodeGen.Models;

namespace ProtoMaster.CodeGen;

/// <summary>
/// 根据 JSON 配置生成双向转换代码
/// </summary>
public class MappingCodeGenerator
{
    private readonly MappingConfig _config;
    private readonly StringBuilder _sb = new();

    public MappingCodeGenerator(string configPath)
    {
        var json = File.ReadAllText(configPath);
        _config = JsonSerializer.Deserialize<MappingConfig>(json)
            ?? throw new InvalidOperationException("Failed to parse config");
    }

    public MappingCodeGenerator(MappingConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 生成所有转换代码
    /// </summary>
    public Dictionary<string, string> GenerateAll()
    {
        var files = new Dictionary<string, string>();

        // 1. 生成枚举转换器
        files["EnumConverters.g.cs"] = GenerateEnumConverters();

        // 2. 生成类型转换器
        files["TypeConverters.g.cs"] = GenerateTypeConverters();

        // 3. 生成集合转换器
        files["CollectionConverters.g.cs"] = GenerateCollectionConverters();

        // 4. 生成聚合转换器（SR_Info → CommonData）
        files["AggregateConverters.g.cs"] = GenerateAggregateConverters();

        // 5. 生成 DataID 路由器
        files["DataIdRouter.g.cs"] = GenerateDataIdRouter();

        // 6. 生成扩展方法
        files["ConverterExtensions.g.cs"] = GenerateExtensions();

        return files;
    }

    private string GenerateEnumConverters()
    {
        _sb.Clear();
        WriteHeader();
        WriteUsings();
        _sb.AppendLine($"namespace {_config.Namespace};");
        _sb.AppendLine();
        _sb.AppendLine("/// <summary>");
        _sb.AppendLine("/// 枚举转换器 (自动生成)");
        _sb.AppendLine("/// </summary>");
        _sb.AppendLine("public static class EnumConverters");
        _sb.AppendLine("{");

        foreach (var (name, converter) in _config.Converters)
        {
            if (converter.Type == "enumMap" && converter.Mappings != null)
            {
                GenerateEnumMapConverter(name, converter);
            }
            else if (converter.Type == "enumDirect")
            {
                GenerateEnumDirectConverter(name, converter);
            }
            else if (converter.Type == "custom")
            {
                GenerateCustomConverter(name, converter);
            }
        }

        _sb.AppendLine("}");
        return _sb.ToString();
    }

    private void GenerateEnumMapConverter(string name, ConverterDef converter)
    {
        var commonType = GetFullCommonType(converter.CommonType ?? "int");
        var defaultCommon = converter.DefaultToCommon ?? "Unknown";
        var defaultProto = converter.DefaultToProto ?? "0";

        // ToCommon 方法
        _sb.AppendLine($"    private static readonly Dictionary<uint, {commonType}> _{name}ToCommonMap = new()");
        _sb.AppendLine("    {");
        foreach (var (key, value) in converter.Mappings!)
        {
            _sb.AppendLine($"        {{ {key}, {commonType}.{value} }},");
        }
        _sb.AppendLine("    };");
        _sb.AppendLine();

        _sb.AppendLine($"    public static {commonType} {name}_ToCommon(uint value)");
        _sb.AppendLine($"        => _{name}ToCommonMap.TryGetValue(value, out var result) ? result : {commonType}.{defaultCommon};");
        _sb.AppendLine();

        // ToProto 方法
        _sb.AppendLine($"    private static readonly Dictionary<{commonType}, uint> _{name}ToProtoMap = new()");
        _sb.AppendLine("    {");
        foreach (var (key, value) in converter.Mappings!)
        {
            _sb.AppendLine($"        {{ {commonType}.{value}, {key} }},");
        }
        _sb.AppendLine("    };");
        _sb.AppendLine();

        _sb.AppendLine($"    public static uint {name}_ToProto({commonType} value)");
        _sb.AppendLine($"        => _{name}ToProtoMap.TryGetValue(value, out var result) ? result : {defaultProto};");
        _sb.AppendLine();

        // 生成类型判断方法 (用于过滤)
        GenerateTypeCheckMethod(name, converter);
    }

    /// <summary>
    /// 为 enumMap 类型生成 IsDynamicType / IsStaticType 判断方法
    /// </summary>
    private void GenerateTypeCheckMethod(string name, ConverterDef converter)
    {
        if (converter.Mappings == null) return;

        var commonType = GetFullCommonType(converter.CommonType ?? "int");
        var values = converter.Mappings.Values.ToList();

        // 根据转换器名称判断生成哪种检查方法
        if (name == "DynamicObjectTypeConverter")
        {
            _sb.AppendLine($"    private static readonly HashSet<{commonType}> _dynamicTypes = new()");
            _sb.AppendLine("    {");
            foreach (var value in values)
            {
                _sb.AppendLine($"        {commonType}.{value},");
            }
            _sb.AppendLine("    };");
            _sb.AppendLine();
            _sb.AppendLine($"    /// <summary>");
            _sb.AppendLine($"    /// 判断是否为动态目标类型");
            _sb.AppendLine($"    /// </summary>");
            _sb.AppendLine($"    public static bool IsDynamicType({commonType} type)");
            _sb.AppendLine($"        => _dynamicTypes.Contains(type);");
            _sb.AppendLine();
        }
        else if (name == "StaticObjectTypeConverter")
        {
            _sb.AppendLine($"    private static readonly HashSet<{commonType}> _staticTypes = new()");
            _sb.AppendLine("    {");
            foreach (var value in values)
            {
                _sb.AppendLine($"        {commonType}.{value},");
            }
            _sb.AppendLine("    };");
            _sb.AppendLine();
            _sb.AppendLine($"    /// <summary>");
            _sb.AppendLine($"    /// 判断是否为静态目标类型");
            _sb.AppendLine($"    /// </summary>");
            _sb.AppendLine($"    public static bool IsStaticType({commonType} type)");
            _sb.AppendLine($"        => _staticTypes.Contains(type);");
            _sb.AppendLine();
        }
    }

    private void GenerateEnumDirectConverter(string name, ConverterDef converter)
    {
        var commonType = GetFullCommonType(converter.CommonType ?? "int");

        _sb.AppendLine($"    public static {commonType} {name}_ToCommon(uint value)");
        _sb.AppendLine($"        => ({commonType})value;");
        _sb.AppendLine();

        _sb.AppendLine($"    public static uint {name}_ToProto({commonType} value)");
        _sb.AppendLine($"        => (uint)value;");
        _sb.AppendLine();
    }

    private void GenerateCustomConverter(string name, ConverterDef converter)
    {
        // 生成自定义转换器的占位方法
        _sb.AppendLine($"    // Custom converter: {name}");
        _sb.AppendLine($"    // ToCommon: {converter.ToCommonCode}");
        _sb.AppendLine($"    // ToProto: {converter.ToProtoCode}");

        // 1) 特例：车道线点
        if (name == "LinePointsConverter")
        {
            _sb.AppendLine($"    public static List<Vector3> {name}_ToCommon(Google.Protobuf.Collections.RepeatedField<ADAS_strt_LinePoint> source)");
            _sb.AppendLine($"        => source.Select(p => new Vector3(p.X / 100f, p.Y / 100f, p.Z / 100f)).ToList();");
            _sb.AppendLine();
            _sb.AppendLine($"    public static Google.Protobuf.Collections.RepeatedField<ADAS_strt_LinePoint> {name}_ToProto(List<Vector3> source)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        var rf = new Google.Protobuf.Collections.RepeatedField<ADAS_strt_LinePoint>();");
            _sb.AppendLine("        rf.AddRange(source.Select(p => new ADAS_strt_LinePoint { X = p.X * 100f, Y = p.Y * 100f, Z = p.Z * 100f }));");
            _sb.AppendLine("        return rf;");
            _sb.AppendLine("    }");
            _sb.AppendLine();
            return;
        }

        // 2) 特例：HPA 路径点
        if (name == "HPAPathPointToVector3")
        {
            _sb.AppendLine($"    public static Vector3 {name}_ToCommon(ADAS_strt_HPAVPAPathInfo source)");
            _sb.AppendLine($"        => new Vector3(source.X / 100f, source.Y / 100f, source.Z / 100f);");
            _sb.AppendLine();
            _sb.AppendLine($"    public static ADAS_strt_HPAVPAPathInfo {name}_ToProto(Vector3 source)");
            _sb.AppendLine($"        => new ADAS_strt_HPAVPAPathInfo {{ X = source.X * 100f, Y = source.Y * 100f, Z = source.Z * 100f }};");
            _sb.AppendLine();
            return;
        }

        // 3) 特例：轨迹点序列
        if (name == "TrajectoryPointsConverter")
        {
            _sb.AppendLine($"    public static CommonTrajectoryPoints {name}_ToCommon(Google.Protobuf.Collections.RepeatedField<ADAS_strt_TrajectoryPoint> source)");
            _sb.AppendLine($"        => new CommonTrajectoryPoints");
            _sb.AppendLine($"        {{");
            _sb.AppendLine($"            Points = source.Select(p => new Vector3(p.CoordinateX / 100f, p.CoordinateY / 100f, p.CoordinateZ / 100f)).ToList()");
            _sb.AppendLine($"        }};");
            _sb.AppendLine();
            _sb.AppendLine($"    public static ADAS_arr_TrajectoryPoint {name}_ToProto(CommonTrajectoryPoints source)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        var arr = new ADAS_arr_TrajectoryPoint();");
            _sb.AppendLine("        arr.TrajectoryPoints.AddRange(source.Points.Select(p => new ADAS_strt_TrajectoryPoint");
            _sb.AppendLine("        {");
            _sb.AppendLine("            CoordinateX = p.X * 100f,");
            _sb.AppendLine("            CoordinateY = p.Y * 100f,");
            _sb.AppendLine("            CoordinateZ = p.Z * 100f");
            _sb.AppendLine("        }));");
            _sb.AppendLine("        return arr;");
            _sb.AppendLine("    }");
            _sb.AppendLine();
            return;
        }

        // 4) 通用 custom：仍用 object 避免缺失属性导致编译错误
        if (!string.IsNullOrEmpty(converter.ToCommonCode))
        {
            _sb.AppendLine($"    public static object {name}_ToCommon(dynamic source)");
            _sb.AppendLine($"        => {converter.ToCommonCode};");
            _sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(converter.ToProtoCode))
        {
            _sb.AppendLine($"    public static object {name}_ToProto(dynamic source)");
            _sb.AppendLine($"        => {converter.ToProtoCode};");
            _sb.AppendLine();
        }
    }

    private string GetFullCommonType(string shortType)
    {
        // 添加完整命名空间前缀
        return shortType switch
        {
            "CommonObjectType" => "ProtoMaster.Common.Models.CommonObjectType",
            "CommonCarLightStatus" => "ProtoMaster.Common.Models.CommonCarLightStatus",
            "CommonObstacleColor" => "ProtoMaster.Common.Models.CommonObstacleColor",
            "CommonColor" => "ProtoMaster.Common.CommonColor",
            "LaneLineType" => "ProtoMaster.Common.Models.LaneLineType",
            "RoadMarkerType" => "ProtoMaster.Common.Models.RoadMarkerType",
            "ParkingSlotType" => "ProtoMaster.Common.Models.ParkingSlotType",
            "ParkingSlotStatus" => "ProtoMaster.Common.Models.ParkingSlotStatus",
            "ParkingSlotFloor" => "ProtoMaster.Common.Models.ParkingSlotFloor",
            "AccState" => "ProtoMaster.Common.Models.AccState",
            "HnopState" => "ProtoMaster.Common.Models.HnopState",
            "CruiseAccelerationState" => "ProtoMaster.Common.Models.CruiseAccelerationState",
            "NopLaneChangeInfo" => "ProtoMaster.Common.Models.NopLaneChangeInfo",
            "IcaState" => "ProtoMaster.Common.Models.IcaState",
            "AlcStatus" => "ProtoMaster.Common.Models.AlcStatus",
            "LaneTrackingState" => "ProtoMaster.Common.Models.LaneTrackingState",
            "ElkActiveState" => "ProtoMaster.Common.Models.ElkActiveState",
            _ => shortType
        };
    }

    private string GenerateTypeConverters()
    {
        _sb.Clear();
        WriteHeader();
        WriteUsings();
        _sb.AppendLine($"namespace {_config.Namespace};");
        _sb.AppendLine();

        foreach (var mapping in _config.TypeMappings)
        {
            GenerateSingleTypeConverter(mapping);
        }

        return _sb.ToString();
    }

    private void GenerateSingleTypeConverter(TypeMapping mapping)
    {
        var protoShortName = GetShortTypeName(mapping.ProtoType);
        var commonShortName = GetShortTypeName(mapping.CommonType);

        _sb.AppendLine($"/// <summary>");
        _sb.AppendLine($"/// {mapping.Description} 转换器");
        _sb.AppendLine($"/// {protoShortName} ⇄ {commonShortName}");
        if (!string.IsNullOrEmpty(mapping.Category))
        {
            _sb.AppendLine($"/// Category: {mapping.Category}");
        }
        _sb.AppendLine($"/// </summary>");
        _sb.AppendLine($"public static class {mapping.Id}Converter");
        _sb.AppendLine("{");

        // ToCommon 方法
        _sb.AppendLine($"    public static {mapping.CommonType} ToCommon({mapping.ProtoType} proto)");
        _sb.AppendLine("    {");
        _sb.AppendLine($"        if (proto == null) return new {mapping.CommonType}();");
        _sb.AppendLine();
        _sb.AppendLine($"        var common = new {mapping.CommonType}();");
        _sb.AppendLine();

        // 收集需要处理的 Vector3 字段
        var vector3Fields = new HashSet<string>();
        foreach (var field in mapping.FieldMappings)
        {
            if (field.Common.Contains('.'))
            {
                var parentPath = string.Join(".", field.Common.Split('.').Take(field.Common.Split('.').Length - 1));
                vector3Fields.Add(parentPath);
            }
        }

        // 为 Vector3 字段生成初始化
        foreach (var vectorField in vector3Fields)
        {
            _sb.AppendLine($"        // 初始化 Vector3 字段");
            break;
        }

        foreach (var field in mapping.FieldMappings)
        {
            GenerateToCommonFieldAssignment(field, vector3Fields);
        }

        // 默认值
        if (mapping.DefaultValues?.Common != null)
        {
            _sb.AppendLine("        // 默认值");
            foreach (var (prop, value) in mapping.DefaultValues.Common)
            {
                var formattedValue = FormatDefaultValue(prop, value);
                _sb.AppendLine($"        common.{prop} = {formattedValue};");
            }
        }

        _sb.AppendLine();
        _sb.AppendLine("        return common;");
        _sb.AppendLine("    }");
        _sb.AppendLine();

        // ToProto 方法
        _sb.AppendLine($"    public static {mapping.ProtoType} ToProto({mapping.CommonType} common)");
        _sb.AppendLine("    {");
        _sb.AppendLine($"        if (common == null) return new {mapping.ProtoType}();");
        _sb.AppendLine();
        _sb.AppendLine($"        var proto = new {mapping.ProtoType}();");
        _sb.AppendLine();

        foreach (var field in mapping.FieldMappings)
        {
            GenerateToProtoFieldAssignment(field);
        }

        _sb.AppendLine();
        _sb.AppendLine("        return proto;");
        _sb.AppendLine("    }");

        _sb.AppendLine("}");
        _sb.AppendLine();
    }

    private string FormatDefaultValue(string prop, string value)
    {
        // 处理枚举默认值
        if (prop == "CarLightStatus")
            return $"ProtoMaster.Common.Models.CommonCarLightStatus.{value}";
        if (prop == "Velocity" && value == "Vector3.Zero")
            return "System.Numerics.Vector3.Zero";
        return value;
    }

    private void GenerateToCommonFieldAssignment(FieldMapping field, HashSet<string> vector3Fields)
    {
        var protoPath = $"proto.{field.Proto}";
        var commonPath = field.Common;
        
        // 处理嵌套路径 (如 Position.X) - 需要特殊处理 Vector3
        if (commonPath.Contains('.'))
        {
            var parts = commonPath.Split('.');
            var parentPath = string.Join(".", parts.Take(parts.Length - 1));
            var propName = parts.Last();
            
            // 对于 Vector3 类型，需要整体赋值而不是分别设置 X、Y、Z
            // 这里先跳过，会在后面统一处理
            return;
        }

        string valueExpr;
        if (!string.IsNullOrEmpty(field.Converter))
        {
            valueExpr = $"EnumConverters.{field.Converter}_ToCommon({protoPath})";
        }
        else if (!string.IsNullOrEmpty(field.ToCommon))
        {
            // 处理 ToInt32 等转换
            var expr = field.ToCommon.Replace("{0}", protoPath);
            if (expr.StartsWith("ToInt32("))
                expr = expr.Replace("ToInt32(", "Convert.ToInt32(");
            else if (expr.StartsWith("ToUInt64("))
                expr = expr.Replace("ToUInt64(", "Convert.ToUInt64(");
            valueExpr = expr;
        }
        else
        {
            valueExpr = protoPath;
        }

        _sb.AppendLine($"        common.{commonPath} = {valueExpr};");
    }

    private void GenerateToProtoFieldAssignment(FieldMapping field)
    {
        var commonPath = $"common.{field.Common}";
        var protoPath = field.Proto;

        // 跳过嵌套 Proto 路径的复杂情况
        if (protoPath.Contains('.')) return;
        
        // 跳过嵌套 Common 路径 (如 Position.X)
        if (field.Common.Contains('.')) return;

        string valueExpr;
        if (!string.IsNullOrEmpty(field.Converter))
        {
            valueExpr = $"EnumConverters.{field.Converter}_ToProto({commonPath})";
        }
        else if (!string.IsNullOrEmpty(field.ToProto))
        {
            var expr = field.ToProto.Replace("{0}", commonPath);
            if (expr.StartsWith("ToUInt64("))
                expr = expr.Replace("ToUInt64(", "Convert.ToUInt64(");
            else if (expr.StartsWith("ToInt32("))
                expr = expr.Replace("ToInt32(", "Convert.ToInt32(");
            valueExpr = expr;
        }
        else
        {
            valueExpr = commonPath;
        }

        _sb.AppendLine($"        proto.{protoPath} = {valueExpr};");
    }

    private string GenerateCollectionConverters()
    {
        _sb.Clear();
        WriteHeader();
        WriteUsings();
        _sb.AppendLine($"namespace {_config.Namespace};");
        _sb.AppendLine();
        _sb.AppendLine("/// <summary>");
        _sb.AppendLine("/// 集合转换器 (自动生成)");
        _sb.AppendLine("/// </summary>");
        _sb.AppendLine("public static class CollectionConverters");
        _sb.AppendLine("{");

        foreach (var coll in _config.CollectionMappings)
        {
            var itemMapping = _config.TypeMappings.First(m => m.Id == coll.ItemMapping);

            // ToCommon
            _sb.AppendLine($"    public static List<{itemMapping.CommonType}> {coll.Id}_ToCommon({coll.ProtoType}? proto)");
            _sb.AppendLine("    {");
            _sb.AppendLine($"        if (proto == null) return new List<{itemMapping.CommonType}>();");
            _sb.AppendLine($"        return proto.{coll.ProtoItemsPath}.Select({coll.ItemMapping}Converter.ToCommon).ToList();");
            _sb.AppendLine("    }");
            _sb.AppendLine();

            // ToProto - 支持 toProtoFilter
            _sb.AppendLine($"    public static {coll.ProtoType} {coll.Id}_ToProto(List<{itemMapping.CommonType}>? common)");
            _sb.AppendLine("    {");
            _sb.AppendLine($"        var proto = new {coll.ProtoType}();");
            _sb.AppendLine($"        if (common == null) return proto;");
            
            if (!string.IsNullOrEmpty(coll.ToProtoFilter))
            {
                // 根据 category 生成过滤条件
                var category = itemMapping.Category;
                if (category == "dynamic")
                {
                    _sb.AppendLine($"        var filtered = common.Where(item => EnumConverters.IsDynamicType(item.Type));");
                }
                else if (category == "static")
                {
                    _sb.AppendLine($"        var filtered = common.Where(item => EnumConverters.IsStaticType(item.Type));");
                }
                else
                {
                    // 使用配置的过滤表达式
                    _sb.AppendLine($"        var filtered = common.Where(item => {coll.ToProtoFilter});");
                }
                _sb.AppendLine($"        proto.{coll.ProtoItemsPath}.AddRange(filtered.Select({coll.ItemMapping}Converter.ToProto));");
            }
            else
            {
                // 无过滤条件：直接转换
                _sb.AppendLine($"        proto.{coll.ProtoItemsPath}.AddRange(common.Select({coll.ItemMapping}Converter.ToProto));");
            }
            
            _sb.AppendLine("        return proto;");
            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        _sb.AppendLine("}");
        return _sb.ToString();
    }

    private string GenerateAggregateConverters()
    {
        _sb.Clear();
        WriteHeader();
        WriteUsings();
        _sb.AppendLine($"namespace {_config.Namespace};");
        _sb.AppendLine();

        foreach (var agg in _config.AggregateMappings)
        {
            _sb.AppendLine($"/// <summary>");
            _sb.AppendLine($"/// {agg.ProtoRoot} ⇄ {agg.CommonRoot} 聚合转换器");
            _sb.AppendLine($"/// </summary>");
            _sb.AppendLine($"public static class {agg.ProtoRoot}Converter");
            _sb.AppendLine("{");

            // ToCommon
            _sb.AppendLine($"    public static {agg.CommonRoot} ToCommon(Chery.Ads.Sense.Relity.{agg.ProtoRoot} proto)");
            _sb.AppendLine("    {");
            _sb.AppendLine($"        var common = new {agg.CommonRoot}();");
            _sb.AppendLine($"        if (proto == null) return common;");
            _sb.AppendLine();

            foreach (var ext in agg.Extractors)
            {
                GenerateAggregateExtractor(ext);
            }

            _sb.AppendLine("        return common;");
            _sb.AppendLine("    }");
            _sb.AppendLine();

            // ToProto
            _sb.AppendLine($"    public static Chery.Ads.Sense.Relity.{agg.ProtoRoot} ToProto({agg.CommonRoot} common)");
            _sb.AppendLine("    {");
            _sb.AppendLine($"        var proto = new Chery.Ads.Sense.Relity.{agg.ProtoRoot}();");
            _sb.AppendLine($"        if (common == null) return proto;");
            _sb.AppendLine();

            foreach (var ext in agg.Extractors)
            {
                GenerateAggregateToProtoExtractor(ext);
            }

            _sb.AppendLine("        return proto;");
            _sb.AppendLine("    }");

            _sb.AppendLine("}");
            _sb.AppendLine();
        }

        return _sb.ToString();
    }

    private void GenerateAggregateExtractor(ExtractorDef ext)
    {
        var isCollection = _config.CollectionMappings.Any(c => c.Id == ext.Mapping);
        
        if (isCollection)
        {
            if (ext.Mode == "addRange")
            {
                _sb.AppendLine($"        common.{ext.CommonPath}.AddRange(CollectionConverters.{ext.Mapping}_ToCommon(proto.{ext.ProtoPath}));");
            }
            else
            {
                _sb.AppendLine($"        common.{ext.CommonPath} = CollectionConverters.{ext.Mapping}_ToCommon(proto.{ext.ProtoPath});");
            }
        }
        else
        {
            _sb.AppendLine($"        common.{ext.CommonPath} = {ext.Mapping}Converter.ToCommon(proto.{ext.ProtoPath});");
        }
    }

    /// <summary>
    /// 生成反向转换 (CommonData -> Proto) 的 extractor
    /// </summary>
    private void GenerateAggregateToProtoExtractor(ExtractorDef ext)
    {
        var isCollection = _config.CollectionMappings.Any(c => c.Id == ext.Mapping);
        
        if (isCollection)
        {
            // 对于集合，调用 CollectionConverters 的 ToProto 方法
            // 过滤逻辑已在 CollectionConverters 中处理
            _sb.AppendLine($"        proto.{ext.ProtoPath} = CollectionConverters.{ext.Mapping}_ToProto(common.{ext.CommonPath});");
        }
        else
        {
            _sb.AppendLine($"        proto.{ext.ProtoPath} = {ext.Mapping}Converter.ToProto(common.{ext.CommonPath});");
        }
    }

    private string GenerateDataIdRouter()
    {
        _sb.Clear();
        WriteHeader();
        WriteUsings();
        _sb.AppendLine($"namespace {_config.Namespace};");
        _sb.AppendLine();
        _sb.AppendLine("/// <summary>");
        _sb.AppendLine("/// 根据 DataID 路由到对应的 Proto 解析器");
        _sb.AppendLine("/// </summary>");
        _sb.AppendLine("public static class DataIdRouter");
        _sb.AppendLine("{");
        _sb.AppendLine("    public static CommonData? ParseAndConvert(int dataId, byte[] data)");
        _sb.AppendLine("    {");
        _sb.AppendLine("        return dataId switch");
        _sb.AppendLine("        {");

        // 每个 dataId 单独生成分支和方法
        foreach (var (id, protoTypeFull) in _config.DataIdRouting)
        {
            var protoTypeShort = protoTypeFull.Split('.').Last();
            _sb.AppendLine($"            {id} => Parse_{protoTypeShort}_{id}(data),");
        }
        _sb.AppendLine("            _ => null");
        _sb.AppendLine("        };");
        _sb.AppendLine("    }");
        _sb.AppendLine();

        // 为每个 dataId+protoType 生成唯一的方法
        foreach (var (id, protoTypeFull) in _config.DataIdRouting)
        {
            var protoTypeShort = protoTypeFull.Split('.').Last();
            var ns = string.Join('.', protoTypeFull.Split('.').Take(protoTypeFull.Split('.').Length - 1));
            _sb.AppendLine($"    private static CommonData? Parse_{protoTypeShort}_{id}(byte[] data)");
            _sb.AppendLine("    {");
            _sb.AppendLine("        try");
            _sb.AppendLine("        {");
            _sb.AppendLine($"            var proto = {ns}.{protoTypeShort}.Parser.ParseFrom(data);");
            _sb.AppendLine($"            return {protoTypeShort}Converter.ToCommon(proto);");
            _sb.AppendLine("        }");
            _sb.AppendLine("        catch { return null; }");
            _sb.AppendLine("    }");
            _sb.AppendLine();
        }

        _sb.AppendLine("}");
        return _sb.ToString();
    }

    private string GenerateExtensions()
    {
        _sb.Clear();
        WriteHeader();
        WriteUsings();
        _sb.AppendLine($"namespace {_config.Namespace};");
        _sb.AppendLine();
        _sb.AppendLine("/// <summary>");
        _sb.AppendLine("/// Proto 类型扩展方法");
        _sb.AppendLine("/// </summary>");
        _sb.AppendLine("public static class ConverterExtensions");
        _sb.AppendLine("{");

        // 记录 commonType 和 protoType 的映射，避免重复扩展方法
        var toProtoMethods = new HashSet<string>();

        foreach (var mapping in _config.TypeMappings)
        {
            // ToCommon 扩展方法
            _sb.AppendLine($"    public static {mapping.CommonType} ToCommon(this {mapping.ProtoType} proto)");
            _sb.AppendLine($"        => {mapping.Id}Converter.ToCommon(proto);");
            _sb.AppendLine();

            // ToProto 扩展方法，针对 commonType 有多个 protoType 的情况，生成不同方法名
            var protoMethodName = $"ToProto_{mapping.ProtoType.Replace('.', '_')}";
            var key = $"{mapping.CommonType}_{mapping.ProtoType}";
            if (!toProtoMethods.Contains(key))
            {
                // 如果 commonType 有多个 protoType，生成带类型后缀的方法
                var duplicate = _config.TypeMappings.Count(m => m.CommonType == mapping.CommonType) > 1;
                if (duplicate)
                {
                    _sb.AppendLine($"    public static {mapping.ProtoType} {protoMethodName}(this {mapping.CommonType} common)");
                    _sb.AppendLine($"        => {mapping.Id}Converter.ToProto(common);");
                }
                else
                {
                    _sb.AppendLine($"    public static {mapping.ProtoType} ToProto(this {mapping.CommonType} common)");
                    _sb.AppendLine($"        => {mapping.Id}Converter.ToProto(common);");
                }
                _sb.AppendLine();
                toProtoMethods.Add(key);
            }
        }

        _sb.AppendLine("}");
        return _sb.ToString();
    }

    private void WriteHeader()
    {
        _sb.AppendLine("// <auto-generated>");
        _sb.AppendLine($"// 此代码由 MappingCodeGenerator 自动生成");
        _sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _sb.AppendLine($"// 配置版本: {_config.Version}");
        _sb.AppendLine("// 请勿手动修改此文件");
        _sb.AppendLine("// </auto-generated>");
        _sb.AppendLine();
    }

    private void WriteUsings()
    {
        _sb.AppendLine("using System;");
        _sb.AppendLine("using System.Collections.Generic;");
        _sb.AppendLine("using System.Linq;");
        _sb.AppendLine("using System.Numerics;");
        _sb.AppendLine("using ProtoMaster.Common;");
        _sb.AppendLine("using ProtoMaster.Common.Models;");
        _sb.AppendLine("using Chery.Ads.Sense.Relity;");
        _sb.AppendLine();
    }

    private static string GetShortTypeName(string fullName)
        => fullName.Split('.').Last();
}