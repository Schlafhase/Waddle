using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Waddle.Config;

public struct WaddleConfig
{
    public required string Host;
    public int Port;
    public required string Username;
    public bool UsePassword;
    public string? Keyfile;
    public bool UseSshAgent;
    public string? ServerOutputFileName;
    public string? ClientOutputFileName;

    public string FinishedIcon;
    public string WaitingIcon;
    public string ErrorIcon;
    public string IgnoredIcon;
    public string NotActiveIcon;

    public static WaddleConfig FromYaml(string yaml)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<WaddleConfig>(yaml);
    }

    public readonly string ToYaml()
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(this);
    }
}