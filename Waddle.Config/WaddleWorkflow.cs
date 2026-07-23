using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Waddle.Config;

public struct YamlPenguin
{
    // Any penguin can define this
    public required string Name;
    public bool IgnoreError;
    public int? TimeoutMs;
    public bool Hide;

    public string? Error;
    public string? IfFalsy;
    public string? IfTruthy;

    // RunCommand
    public string? Cmd;
    public List<string>? Shell;

    public string? ServerCmd;

    public string Variable;

    // File stuff
    public string? SendFolder;
    public string? SendCompressed;
    public string? ReceiveFolder;
    public string? ReceiveCompressed;
    public string? SendFile;
    public string? ReceiveFile;
    public string? Destination;

    // Nested workflow
    public string? Workflow;
    public List<YamlPenguin> Children;
}

public static class WaddleWorkflow
{
    public static List<YamlPenguin> FromYaml(string yaml)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeInspector(n => new CamelOrPascalCaseTypeInspector(n))
            .Build();

        List<YamlPenguin> workflow = deserializer.Deserialize<List<YamlPenguin>>(yaml);
        ArgumentNullException.ThrowIfNull(workflow);
        return workflow;
    }

    public static List<YamlPenguin> FromWorkflowName(string workflowName, ILogger? logger = null)
    {
        return FromWorkflowName(workflowName, out _, logger);
    }

    public static List<YamlPenguin> FromWorkflowName(
        string workflowName,
        out string sourceFile,
        ILogger? logger = null
    )
    {
        List<string> allowedFileEndings = [".w.yaml", ".w.yml", ".yaml", ".yml"];
        bool hasFileEnding = allowedFileEndings.Any(workflowName.EndsWith);
        logger?.LogTrace("Finding workflow file for `{workflow}`", workflowName);

        string yaml = "";
        sourceFile = "";
        if (hasFileEnding)
        {
            logger?.LogTrace("Checking `{file}`", workflowName);
            if (File.Exists(workflowName))
            {
                logger?.LogInformation("Using `{file}` as workflow file", workflowName);
                yaml = File.ReadAllText(workflowName);
                sourceFile = workflowName;
            }
        }
        else
        {
            foreach (string ending in allowedFileEndings)
            {
                logger?.LogTrace("Checking `{file}`", workflowName + ending);
                if (File.Exists(workflowName + ending))
                {
                    logger?.LogInformation(
                        "Using `{file}` as workflow file",
                        workflowName + ending
                    );
                    yaml = File.ReadAllText(workflowName + ending);
                    sourceFile = workflowName + ending;
                    break;
                }
            }
        }
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new ArgumentException(
                $"The requested workflow ({workflowName}) is empty or doesn't exist. Create a .yaml, .yml, .w.yaml or .w.yml file to create it."
            );
        }
        logger?.LogTrace("Source file: {file}", sourceFile);

        return FromYaml(yaml);
    }
}
