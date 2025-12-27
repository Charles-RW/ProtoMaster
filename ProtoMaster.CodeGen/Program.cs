using ProtoMaster.CodeGen;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ProtoMaster.CodeGen <config.json> <output-dir>");
    return 1;
}

var configPath = args[0];
var outputDir = args[1];

if (!File.Exists(configPath))
{
    Console.WriteLine($"Error: Config file not found: {configPath}");
    return 1;
}

Directory.CreateDirectory(outputDir);

Console.WriteLine("Generating converters...");
var generator = new MappingCodeGenerator(configPath);
var files = generator.GenerateAll();

foreach (var (fileName, content) in files)
{
    var path = Path.Combine(outputDir, fileName);
    File.WriteAllText(path, content);
    Console.WriteLine($"  Generated: {fileName}");
}

Console.WriteLine($"\nTotal {files.Count} files generated.");
return 0;