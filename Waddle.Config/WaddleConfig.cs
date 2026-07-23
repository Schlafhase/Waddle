using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Waddle.Config;

public struct WaddleServerConfig()
{
    #region ServerConfigFields
    public required string Host;
    public int Port;
    public required string Username;

    public bool UsePassword;
    public string? Keyfile;
    public bool UseSshAgent;

    public string? ServerOutputFileName;
    public char DirectorySeparator = '/';
    #endregion

    [YamlIgnore]
    public readonly string? KeyfileFullPath =>
        Keyfile is null
            ? null
            : Path.GetFullPath(
                Keyfile.StartsWith("~/")
                    ? "/home/" + Environment.UserName + "/" + Keyfile[2..]
                    : Keyfile
            );

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
        if (DirectorySeparator is default(char))
        {
            unset.Add(nameof(DirectorySeparator));
        }
        return unset;
    }
}

public struct WaddleConfig
{
    #region ConfigFields
    public WaddleServerConfig? Server;

    public string? ClientOutputFileName;

    public string? LogFileName;
    public LogLevel LogLevel; // Trace | Debug | Information | Warning | Error | Critical

    [YamlMember(Alias = "FinishedIcon")]
    public string SuccessIcon;
    public string WaitingIcon;
    public string ErrorIcon;
    public string IgnoredIcon;

    [YamlMember(Alias = "NotActiveIcon")]
    public string IdleIcon;

    public required string DefaultWorkflow;
    public List<string>? DefaultShell; // e.g. ["sh", "-c"]

    public bool VerboseErrors;
    #endregion


    /// <summary>
    /// Validates a config object by checking if all required fields are set.
    /// </summary>
    /// <returns>A list of Fieldnames that haven't been set</returns>
    public readonly List<string> Validate()
    {
        List<string> unset = [];
        unset.AddRange(Server?.Validate().Select(field => nameof(Server) + "." + field) ?? []);
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

        WaddleConfig config = deserializer.Deserialize<WaddleConfig>(yaml);
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