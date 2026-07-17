#!/usr/bin/dotnet

const string templatePath = "_README.md";
const string targetPath = "README.md";

string markdown = await File.ReadAllTextAsync(templatePath);

Dictionary<string, string> replacements = [];
replacements.Add("{disclaimer}", "<!-- GENERATED FILE. DO NOT EDIT -->");
replacements.Add(
    "{configFields}",
    await getRegion("./Waddle.Config/WaddleConfig.cs", "ConfigFields")
);
replacements.Add(
    "{serverConfigFields}",
    await getRegion("./Waddle.Config/WaddleConfig.cs", "ServerConfigFields")
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
