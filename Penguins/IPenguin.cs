
namespace Penguins;

public interface IPenguin
{
    public string Name { get; }
    public bool IgnoreError { get; set; }
    public int? TimeoutMs { get; set; }

    public Task Execute(CancellationToken cancellationToken);
}
