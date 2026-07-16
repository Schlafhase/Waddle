using Waddle.Config;

namespace Penguins;

public abstract class PenguinBase : IPenguin
{
    public required string Name { get; set; }
    public bool IgnoreError { get; set; }
    public int? TimeoutMs { get; set; }

    public abstract Task Execute(CancellationToken cancellationToken);
}
