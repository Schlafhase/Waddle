namespace Penguins;

public enum PenguinState
{
    Idle,
    Working,
    Error,
    IgnoredError,
    Success,
}

public interface IPenguin
{
    public string Name { get; }
    public bool IgnoreError { get; set; }
    public int? TimeoutMs { get; set; }

    public PenguinState State { get; set; }
    public string? Status { get; set; }
    public Action? OnStatusChange {set;}

    public Task Execute(CancellationToken cancellationToken);
}
