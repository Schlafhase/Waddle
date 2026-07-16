using Waddle.Config;

namespace Penguins;

public interface IPenguin
{
    public WaddleContext Context { get; }
    public string Name { get; }
    public bool IgnoreError { get; }
    public int? TimeoutMs { get; }

    public Task Execute(CancellationToken cancellationToken);
}
