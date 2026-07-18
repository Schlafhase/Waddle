#!/usr/bin/dotnet

using System.Text;
using System.Text.RegularExpressions;

const string templatePath = "_README.md";
const string targetPath = "README.md";

string markdown = await File.ReadAllTextAsync(templatePath);

Dictionary<string, string> replacements = [];
replacements.Add("{disclaimer}", "<!-- GENERATED FILE. DO NOT EDIT -->");
replacements.Add(
    "{configFields}",
    await getCodeRegion("./Waddle.Config/WaddleConfig.cs", "ConfigFields")
);
replacements.Add(
    "{serverConfigFields}",
    await getCodeRegion("./Waddle.Config/WaddleConfig.cs", "ServerConfigFields")
);
replacements.Add(
    "{nixPackage}",
    """
        (buildDotnetGlobalTool {
          pname = "waddle";
          nugetName = "Waddle.Cli";
          version = "0.4.1";
          nugetHash = "sha256-tACHDgvmmXZNwDn7qgcv+iCle1X154HrekdV8KQ7jiQ=";
          dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
        })
    """
);

replacements.Add("{penguinsTable}", await getPenguinsTable());

Regex penguinDescriptionRegex = new Regex(@"{([a-zA-Z]+)PenguinDescription}");
Regex penguinParameterRegex = new Regex(@"{([a-zA-Z]+)PenguinParameters}");

static string? findFileRecursive(string startDir, string file)
{
    string path = Path.GetFullPath(Path.Combine(startDir, file));
    Console.WriteLine(path);
    if (File.Exists(path))
    {
        return path;
    }
    foreach (string d in Directory.EnumerateDirectories(startDir))
    {
        if (findFileRecursive(d, file) is { } found)
        {
            return found;
        }
    }
    return null;
}

foreach (Match m in penguinDescriptionRegex.Matches(markdown))
{
    replacements.Add(
        $"{m.Value}",
        await getPenguinDescription(
            findFileRecursive("./Penguins/", m.Groups[1].Value + "Penguin.cs")
                ?? throw new FileNotFoundException(m.Groups[1] + "Penguin.cs")
        )
    );
}

foreach (Match m in penguinParameterRegex.Matches(markdown))
{
    replacements.Add(
        $"{m.Value}",
        await getPenguinParams(
            findFileRecursive("./Penguins/", m.Groups[1].Value + "Penguin.cs")
                ?? throw new FileNotFoundException(m.Groups[1] + "Penguin.cs")
        )
    );
}

foreach (KeyValuePair<string, string> kvp in replacements)
{
    if (!markdown.Contains(kvp.Key, StringComparison.Ordinal))
    {
        Console.WriteLine($"Could not find '{kvp.Key}' in {templatePath}. Skipping");
    }

    markdown = markdown.Replace(kvp.Key, kvp.Value, StringComparison.Ordinal);
}

await File.WriteAllTextAsync(targetPath, markdown);

return 0;

static string removeIndentation(string text)
{
    List<string> lines =
    [
        .. text.ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => line.Trim())
            .SkipWhile(string.IsNullOrWhiteSpace),
    ];

    int end;
    for (end = lines.Count; end > 0 && string.IsNullOrWhiteSpace(lines[end - 1]); end--)
        ;

    return string.Join(Environment.NewLine, lines.Take(end));
}

static string getGithubLink(string file)
{
    return $"https://github.com/Schlafhase/Waddle/blob/master/{Path.GetRelativePath(".", file)}";
}

static async Task<string> getCodeRegion(string file, string region)
{
    string regionText = await getRegion(file, region);
    return $"""
        > Extracted from [{Path.GetFileName(
            file
        )}]({getGithubLink(file)})
        ```cs
        {regionText}
        ```
        """;
}

static async Task<string> getRegion(string file, string region)
{
    string regionStart = "#region " + region;
    const string regionEnd = "#endregion";

    string source = await File.ReadAllTextAsync(file);

    int start = source.IndexOf(regionStart, StringComparison.Ordinal);
    if (start < 0)
    {
        throw new InvalidOperationException($"Could not find '{regionStart}' in {file}.");
    }

    start = source.IndexOf('\n', start);
    if (start < 0)
    {
        throw new InvalidOperationException($"The '{regionStart}' marker has no content after it.");
    }

    start++;

    int end = source.IndexOf(regionEnd, start, StringComparison.Ordinal);
    if (end < 0)
    {
        throw new InvalidOperationException($"Could not find '{regionEnd}' after '{regionStart}'.");
    }

    string regionText = source[start..end].TrimEnd('\r', '\n');
    return removeIndentation(regionText);
}

static async Task<string> getPenguinDescription(string file)
{
    string regionContent = await getRegion(file, "ReadmeInfo");
    return regionContent.ReplaceLineEndings("\n").Split("\n")[0][2..].Trim();
}

static async Task<string> getPenguinParams(string file)
{
    string regionContent = await getRegion(file, "ReadmeInfo");
    return regionContent.ReplaceLineEndings("\n").Split("\n")[1][2..].Trim();
}

static async Task<string> getPenguinsTable()
{
    StringBuilder sb = new();
    sb.AppendLine("| Penguin | Description | Parameters |");
    sb.AppendLine("|---|---|---|");

    foreach (
        string penguinSource in Directory.GetFiles(
            "./Penguins/",
            "*Penguin.cs",
            new EnumerationOptions() { RecurseSubdirectories = true }
        )
    )
    {
        string filename = Path.GetFileName(penguinSource);
        Console.WriteLine(filename);
        // Interface
        if (filename.StartsWith('I') && char.IsAsciiLetterUpper(filename[1]))
        {
            continue;
        }
        string name = filename.Replace("Penguin.cs", "");
        string description;
        string parameters;
        try
        {
            description = await getPenguinDescription(penguinSource);
        }
        catch (IndexOutOfRangeException)
        {
            description = "*No documentation available*";
        }

        try
        {
            parameters = await getPenguinParams(penguinSource);
        }
        catch (IndexOutOfRangeException)
        {
            parameters = "*No documentation available*";
        }

        sb.AppendLine(
            $"| [{name}]({getGithubLink(penguinSource)}) | {description} | {parameters} |"
        );
    }

    return sb.ToString();
}
