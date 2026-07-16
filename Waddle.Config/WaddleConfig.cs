using Microsoft.Extensions.Logging;
using Renci.SshNet.Security;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Waddle.Config;

public struct WaddleConfig
{
    public const string Version = "0.3.0";

    public required string Host;
    public int Port;
    public required string Username;

    public bool UsePassword;
    public string? Keyfile;
    public bool UseSshAgent;

    public string? ServerOutputFileName;
    public string? ClientOutputFileName;

    public string? LogFileName;
    public LogLevel LogLevel;

    public string FinishedIcon;
    public string WaitingIcon;
    public string ErrorIcon;
    public string IgnoredIcon;
    public string NotActiveIcon;

    public required string DefaultWorkflow;

    public bool VerboseErrors;

    /// <summary>
    /// Validates a config object by checking if all required fields are set.
    /// </summary>
    /// <returns>A list of Fieldnames that haven't been set</returns>
    public readonly List<string> Validate()
    {
        List<string> unset = [];
        if (string.IsNullOrWhiteSpace(Host))
        {
            unset.Add(nameof(Host));
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            unset.Add(nameof(Username));
        }
        if (!UsePassword && !UseSshAgent && string.IsNullOrWhiteSpace(Keyfile))
        {
            unset.Add($"One of {nameof(UsePassword)}, {nameof(Keyfile)} or {nameof(UseSshAgent)}");
        }
        if (string.IsNullOrWhiteSpace(DefaultWorkflow))
        {
            unset.Add(nameof(DefaultWorkflow));
        }
        return unset;
    }

    public readonly void ThrowIfInvalid()
    {
        List<string> missing = Validate();
        if (missing.Count != 0)
        {
            throw new MissingFieldException(
                $"The following required fields are missing: {string.Join(", ", missing.Select(m => $"[[[blue]{m}[/]]]"))}"
            );
        }
    }

    public static WaddleConfig FromYaml(string yaml)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<WaddleConfig>(yaml);
        config.ThrowIfInvalid();
        return config;
    }

    public readonly string ToYaml()
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(this);
    }
}