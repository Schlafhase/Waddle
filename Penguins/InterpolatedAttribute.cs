namespace Penguins;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class InterpolatedAttribute : Attribute;