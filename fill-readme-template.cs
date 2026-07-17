#!/usr/bin/dotnet

const string templatePath = "_README.md";
const string targetPath = "README.md";

string markdown = await File.ReadAllTextAsync(templatePath);

Dictionary<string, string> replacements = [];
replacements.Add(
    "{configFields}",
    await getRegion("./Waddle.Config/WaddleConfig.cs", "ConfigFields")
);
replacements.Add(
    "{serverConfigFields}",
    await getRegion("./Waddle.Config/WaddleConfig.cs", "ServerConfigFields")
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
    IEnumerable<string> lines = text.ReplaceLineEndings("\n")
        .Split('\n')
        .Select(line => line.Trim());

    return string.Join(Environment.NewLine, lines);
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
