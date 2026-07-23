using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Waddle.Config;

namespace Penguins;

public abstract partial class PenguinBase(WaddleContext context) : IPenguin
{
    [GeneratedRegex(@"\$\$|\$\{([a-zA-Z_][a-zA-Z_0-9]*)\}")]
    private static partial Regex variablePattern();

    public required string Name { get; set; }
    public bool IgnoreError { get; set; }
    public int? TimeoutMs { get; set; }

    public PenguinState State
    {
        get => field;
        set
        {
            field = value;
            OnStatusChange?.Invoke();
        }
    }
    public Action? OnStatusChange { private get; set; }

    public string? Status
    {
        get => field;
        set
        {
            field = value;
            OnStatusChange?.Invoke();
        }
    }

    protected WaddleContext _context = context;

    public virtual void ExecutePre()
    {
        InterpolateProperties();
    }

    public abstract Task Execute(CancellationToken cancellationToken);

    protected void InterpolateProperties()
    {
        foreach (PropertyInfo prop in GetType().GetProperties())
        {
            if (prop.GetCustomAttribute<InterpolatedAttribute>() is null)
            {
                continue;
            }
            if (prop.GetValue(this) is not string raw)
            {
                continue;
            }

            _context.Logger?.LogDebug("Interpolating property: {prop}", prop.Name);

            string resolved = variablePattern()
                .Replace(
                    raw,
                    match =>
                    {
                        if (match.Value == "$$")
                        {
                            return "$";
                        }

                        string name = match.Groups[1].Value;
                        if (_context.Variables.TryGetValue(name, out string? result))
                        {
                            _context.Logger?.LogDebug("Inserted {repl} into template", result);
                            return result;
                        }
                        throw new InvalidOperationException(
                            $"Penguin `{Name}` tried to access undefined variable: {name}"
                        );
                    }
                );
            prop.SetValue(this, resolved);
        }
    }
}
