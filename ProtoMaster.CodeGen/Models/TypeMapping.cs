using System.Text.Json.Serialization;

namespace ProtoMaster.CodeGen.Models;

public class TypeMapping
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("protoType")]
    public string ProtoType { get; set; } = "";

    [JsonPropertyName("commonType")]
    public string CommonType { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("fieldMappings")]
    public List<FieldMapping> FieldMappings { get; set; } = [];

    [JsonPropertyName("defaultValues")]
    public DefaultValues? DefaultValues { get; set; }
}