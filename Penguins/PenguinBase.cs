using Waddle.Config;

namespace Penguins;

public abstract class PenguinBase(WaddleContext context) : IPenguin
{
    public WaddleContext Context => context;
    public required string Name { get; set; }
    public bool IgnoreError { get; set; }
    public int? TimeoutMs { get; set; }

    public abstract Task Execute(CancellationToken cancellationToken);
}
