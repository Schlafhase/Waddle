using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Waddle.Config;

public struct WorkflowPenguin
{
    public required string Name;
    public bool IgnoreError;
    public int? TimeoutMs;
    public string? Cmd;
    public string? ServerCmd;
    public string? SendFolder;
    public string? ReceiveFolder;
    public string? SendFile;
    public string? ReceiveFile;
    public string? Destination;
}

public class WaddleWorkflow
{
    public required string Name;
    public required List<WorkflowPenguin> WorkflowPenguins;

    public static WaddleWorkflow FromYaml(string yaml, string name)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return new WaddleWorkflow()
        {
            Name = name,
            WorkflowPenguins = deserializer.Deserialize<List<WorkflowPenguin>>(yaml),
        };
    }
}
