using Waddle.Config;

namespace Penguins;

public interface IPenguin
{
    public WaddleContext Context { get; }
    public string Name { get; }
    public bool IgnoreError { get; set;}
    public int? TimeoutMs { get; set;}

    public Task Execute(CancellationToken cancellationToken);
}