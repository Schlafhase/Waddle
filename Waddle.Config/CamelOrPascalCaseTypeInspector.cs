using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;

namespace Waddle.Config;

public class CamelOrPascalCaseTypeInspector(ITypeInspector innerTypeInspector) : ITypeInspector
{
    private readonly ITypeInspector _innerTypeInspector =
        innerTypeInspector ?? throw new ArgumentNullException(nameof(innerTypeInspector));

    public string GetEnumName(Type enumType, string name)
    {
        return _innerTypeInspector.GetEnumName(enumType, name);
    }

    public string GetEnumValue(object enumValue)
    {
        return _innerTypeInspector.GetEnumValue(enumValue);
    }

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        return _innerTypeInspector.GetProperties(type, container);
    }

    public IPropertyDescriptor GetProperty(
        Type type,
        object? container,
        string name,
        [MaybeNullWhen(true)] bool ignoreUnmatched,
        bool caseInsensitivePropertyMatching
    )
    {
        if (name.Length < 1)
        {
            throw new NotImplementedException(
                "GetProperty is not implemented for empty property names."
            );
        }

        // First letter is case-insensitive
        IEnumerable<IPropertyDescriptor> candidates = GetProperties(type, container)
            .Where(p =>
                p.Name.Equals(char.ToLower(name[0]) + name[1..])
                || p.Equals(char.ToUpper(name[0]) + name[1..])
            );


        using IEnumerator<IPropertyDescriptor> enumerator = candidates.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            if (ignoreUnmatched)
            {
                return null!;
            }

            throw new SerializationException(
                $"Found unexpected property `{name}` while parsing `{type.FullName}`"
            );
        }

        IPropertyDescriptor property = enumerator.Current;

        if (enumerator.MoveNext())
        {
            throw new SerializationException(
                $"Multiple properties with the name `{name}` cannot be defined on the same type. Properties are camelCase or PascalCase which means that `HelloWorld` is equivalent to `helloWorld`"
            );
        }

        return property;
    }

    public bool HasParseMethod(Type type)
    {
        return _innerTypeInspector.HasParseMethod(type);
    }

    public object? Parse(string value, Type expectedType)
    {
        return _innerTypeInspector.Parse(value, expectedType);
    }
}
