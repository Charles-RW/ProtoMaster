using System.Text.Json.Serialization;

namespace ProtoMaster.CodeGen.Models;

public class MappingConfig
{
    [JsonPropertyName("pluginName")]
    public string PluginName { get; set; } = "";

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("dataIdRouting")]
    public Dictionary<string, string> DataIdRouting { get; set; } = [];

    [JsonPropertyName("typeMappings")]
    public List<TypeMapping> TypeMappings { get; set; } = [];

    [JsonPropertyName("collectionMappings")]
    public List<CollectionMapping> CollectionMappings { get; set; } = [];

    [JsonPropertyName("converters")]
    public Dictionary<string, ConverterDef> Converters { get; set; } = [];

    [JsonPropertyName("aggregateMappings")]
    public List<AggregateMapping> AggregateMappings { get; set; } = [];
}



public class FieldMapping
{
    [JsonPropertyName("proto")]
    public string Proto { get; set; } = "";

    [JsonPropertyName("common")]
    public string Common { get; set; } = "";

    [JsonPropertyName("converter")]
    public string? Converter { get; set; }

    [JsonPropertyName("toCommon")]
    public string? ToCommon { get; set; }

    [JsonPropertyName("toProto")]
    public string? ToProto { get; set; }
}

public class DefaultValues
{
    [JsonPropertyName("common")]
    public Dictionary<string, string>? Common { get; set; }

    [JsonPropertyName("proto")]
    public Dictionary<string, string>? Proto { get; set; }
}

public class CollectionMapping
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("protoType")]
    public string ProtoType { get; set; } = "";

    [JsonPropertyName("protoItemsPath")]
    public string ProtoItemsPath { get; set; } = "";

    [JsonPropertyName("itemMapping")]
    public string ItemMapping { get; set; } = "";

    [JsonPropertyName("toProtoFilter")]
    public string? ToProtoFilter { get; set; }
}

public class ConverterDef
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";  // enumMap, enumDirect, custom

    [JsonPropertyName("protoType")]
    public string? ProtoType { get; set; }

    [JsonPropertyName("commonType")]
    public string? CommonType { get; set; }

    [JsonPropertyName("mappings")]
    public Dictionary<string, string>? Mappings { get; set; }

    [JsonPropertyName("defaultToCommon")]
    public string? DefaultToCommon { get; set; }

    [JsonPropertyName("defaultToProto")]
    public string? DefaultToProto { get; set; }

    [JsonPropertyName("toCommonCode")]
    public string? ToCommonCode { get; set; }

    [JsonPropertyName("toProtoCode")]
    public string? ToProtoCode { get; set; }
}

public class AggregateMapping
{
    [JsonPropertyName("protoRoot")]
    public string ProtoRoot { get; set; } = "";

    [JsonPropertyName("commonRoot")]
    public string CommonRoot { get; set; } = "";

    [JsonPropertyName("extractors")]
    public List<ExtractorDef> Extractors { get; set; } = [];
}