using Waddle.Config;

namespace Penguins;

public abstract class PenguinBase(WaddleContext context) : IPenguin
{
    public WaddleContext Context => context;
    public required string Name { get; init; }
    public bool IgnoreError { get; init; }
    public int? TimeoutMs { get; init; }

        public abstract Task Execute(CancellationToken cancellationToken);
}
