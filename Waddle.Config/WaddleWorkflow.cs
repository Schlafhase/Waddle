using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Waddle.Config;

public struct WorkflowPenguin
{
    public required string Name;
    public bool IgnoreError;
    public string? Cmd;
    public string? ServerCmd;
    public string? SendFolder;
    public string? GetFolder;
}

public class WaddleWorkflow
{
    public required string Name;
    public required List<WorkflowPenguin> WorkflowPenguins;

    public static WaddleWorkflow FromYaml(string yaml, string name)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        return new WaddleWorkflow()
        {
            Name = name,
            WorkflowPenguins = deserializer.Deserialize<List<WorkflowPenguin>>(yaml),
        };
    }
}