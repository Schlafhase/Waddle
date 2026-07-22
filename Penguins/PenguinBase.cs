using Waddle.Config;

namespace Penguins;

public abstract class PenguinBase : IPenguin
{
    public required string Name { get; set; }
    public bool IgnoreError { get; set; }
    public int? TimeoutMs { get; set; }

    public PenguinState State { get => field; set {field = value; OnStatusChange?.Invoke();} }
    public Action? OnStatusChange { private get; set;}

    public string? Status
    {
        get => field;
        set
        {
            field = value;
            OnStatusChange?.Invoke();
        }
    }
    public abstract Task Execute(CancellationToken cancellationToken);
}