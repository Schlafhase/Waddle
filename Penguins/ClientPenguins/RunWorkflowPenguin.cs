using Waddle.Config;

namespace Penguins.ClientPenguins;

public class RunWorkflowPenguin(WaddleContext context) : PenguinBase
{
    public required string Workflow;

    // TODO: probably needs to be integrated into the workflow runner directly
    public override Task Execute(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
