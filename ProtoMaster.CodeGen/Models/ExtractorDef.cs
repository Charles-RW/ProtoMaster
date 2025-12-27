using System.Text.Json.Serialization;

namespace ProtoMaster.CodeGen.Models;

public class ExtractorDef
{
    [JsonPropertyName("protoPath")]
    public string ProtoPath { get; set; } = "";
    
    [JsonPropertyName("commonPath")]
    public string CommonPath { get; set; } = "";

    [JsonPropertyName("mapping")]
    public string Mapping { get; set; } = "";

    /// <summary>
    /// 赋值模式: "assign" (默认) 或 "addRange" (追加)
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "assign";
}
