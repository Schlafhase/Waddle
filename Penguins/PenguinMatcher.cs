using System.Reflection;
using Microsoft.Extensions.Logging;
using Waddle.Config;
using Waddle.Config.Exceptions;

namespace Penguins;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal class PenguinMappingAttribute(string[] allowedParameters, string penguinName) : Attribute
{
    public string[] AllowedParameters = allowedParameters;
    public string PenguinName = penguinName;
}

internal struct Mapping
{
    public List<string> AllowedParameters;
    public string Name;
    public Func<YamlPenguin, IPenguin> Map;
}

/// <summary>
/// Contains a global list of YamlPenguin to IPenguin mappings.
/// This is used in the last step of parsing the yaml workflow file.
/// </summary>
internal partial class PenguinMatcher(WaddleContext context, int depth = 0)
{
    private List<Mapping>? _mappings;
    private readonly List<string> _alwaysAllowed = ["Name", "IgnoreError", "TimeoutMs", "Hide"];

    private void initialise()
    {
        if (_mappings is not null)
        {
            return;
        }
        _mappings = [];
        foreach (MethodInfo method in GetType().GetMethods())
        {
            if (!(method.GetCustomAttribute<PenguinMappingAttribute>() is { } attr))
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (
                method.ReturnType != typeof(IPenguin)
                || parameters.Length != 1
                || parameters[0].ParameterType != typeof(YamlPenguin)
            )
            {
                throw new CustomAttributeFormatException(
                    "The PenguinMapping attribute can only be used on methods with the signature `IPenguin _(YamlPenguin)`. It should throw a `NoMatchException` if it does not match the penguin and an `InvalidPenguinException` if it detected a malformed penguin."
                );
            }

            context.Logger?.LogDebug("Added matcher: {name}", method.Name);
            Mapping m = new()
            {
                AllowedParameters = [.. attr.AllowedParameters],
                Name = attr.PenguinName,
                Map = yp => (method.Invoke(this, [yp]) as IPenguin)!,
            };
            m.AllowedParameters.AddRange(_alwaysAllowed);
            _mappings.Add(m);
        }
    }

    public IPenguin Match(YamlPenguin yp)
    {
        initialise();
        foreach (Mapping mapping in _mappings!)
        {
            try
            {
                IPenguin match = mapping.Map(yp);

                // If the match was accepted, enforce allowed parameters to avoid confusion
                foreach (FieldInfo field in yp.GetType().GetFields())
                {
                    if (
                        !mapping.AllowedParameters.Contains(field.Name)
                        && field.GetValue(yp) is not null
                    )
                    {
                        throw new InvalidPenguinException(
                            $"The penguin '{yp.Name}' defines too many parameters. Waddle tried to match a `{mapping.Name}` which only allows the following parameters: {string.Join(", ", mapping.AllowedParameters)}. `{field.Name}` was set when `{mapping.Name}` didn't allow it."
                        );
                    }
                }

                match.IgnoreError = yp.IgnoreError;
                match.TimeoutMs = yp.TimeoutMs;
                match.Hide = yp.Hide;
                return match;
            }
            catch (TargetInvocationException e) when (e.InnerException is NoMatchException) { }
        }
        throw new NoPenguinMatchedException(
            $"The penguin '{yp.Name}' does not match any of the known penguins. Reminder: Every penguin needs at least a name and additional parameters depending on the penguin (check the README for details). Additional parameters that are allowed for every penguin are: {string.Join(", ", _alwaysAllowed)}"
        );
    }
}
