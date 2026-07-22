using Microsoft.Extensions.Logging;
using Penguins.ServerPenguins;
using Waddle.Config;

namespace Penguins.ClientPenguins;

#region ReadmeInfo
// Runs a workflow from a filepath or a list of penguins
// `workflow` (string), `children` (List\<IPenguin\>)
#endregion

public class RunWorkflowPenguin : PenguinBase
{
    public List<YamlPenguin> Workflow;
    private bool _usesServer;
    public List<IPenguin> Penguins = [];
    public Action? OnPenguinsChange;
    private readonly WaddleContext _context;

    public RunWorkflowPenguin(WaddleContext context, List<YamlPenguin> workflow, int depth = 0)
    {
        Workflow = workflow;
        _context = context;
        foreach (YamlPenguin yp in Workflow)
        {
            IPenguin p = toIPenguin(yp, depth);
            Penguins.Add(p);
        }
    }

    // TODO: probably needs to be integrated into the workflow runner directly
    public override async Task Execute(CancellationToken cancellationToken)
    {
        if (_usesServer)
        {
            await _context.ServerOrThrow.Connect();
        }
        foreach (IPenguin p in Penguins)
        {
            _context.Logger?.LogTrace("Entering new workflow penguin");
            p.OnStatusChange = OnPenguinsChange;
            p.State = PenguinState.Working;

            using CancellationTokenSource tokenSource = p.TimeoutMs is { } timeout
                ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout))
                : new CancellationTokenSource();

            try
            {
                _context.Logger?.LogInformation("Running {name}", p.Name);
                RunWorkflowPenguin? rwp = p is RunWorkflowPenguin _rwp ? _rwp : null;
                rwp?.OnPenguinsChange = OnPenguinsChange;
                await p.Execute(tokenSource.Token);
                p.State = PenguinState.Success;
                p.Status = "";
                rwp?.OnPenguinsChange = null;
            }
            catch (Exception e)
            {
                string message = e is TaskCanceledException or OperationCanceledException
                    ? "Penguin timed out."
                    : e.Message;
                if (!p.IgnoreError)
                {
                    p.State = PenguinState.Error;
                    throw;
                }
                _context.Logger?.LogWarning(
                    "Ignored error while running {name}: {err}",
                    p.Name,
                    e.Message
                );
                p.State = PenguinState.IgnoredError;
                p.Status = message.Replace("\n", " ");
            }

            p.OnStatusChange = null;
        }
    }

    private IPenguin toIPenguin(YamlPenguin yp, int depth = 0)
    {
        if (depth >= 255)
        {
            throw new StackOverflowException(
                "Nesting workflows deeper than 255 levels is not allowed."
            );
        }

        WaddleServerContext getServerContext()
        {
            _usesServer = true;
            return _context.ServerOrThrow;
        }

        IPenguin p = yp switch
        {
            { Cmd: { } cmd } => new RunCommandPenguin(_context)
            {
                Name = yp.Name,
                Command = cmd,
                Shell = yp.Shell,
            },

            { ServerCmd: { } serverCmd } => new RunServerCommandPenguin(
                _context,
                getServerContext()
            )
            {
                Name = yp.Name,
                Command = serverCmd,
            },

            { ReceiveFolder: { } receiveFolder, Destination: { } destination } =>
                new ReceiveFolderPenguin(_context, getServerContext())
                {
                    Name = yp.Name,
                    Source = receiveFolder,
                    Destination = destination,
                },

            { SendFolder: { } sendFolder, Destination: { } destination } => new SendFolderPenguin(
                _context,
                getServerContext()
            )
            {
                Name = yp.Name,
                Source = sendFolder,
                Destination = destination,
            },
            { SendFile: { } sendFile, Destination: { } destination } => new SendFilePenguin(
                _context,
                getServerContext()
            )
            {
                Name = yp.Name,
                Source = sendFile,
                Destination = destination,
            },
            { ReceiveFile: { } receiveFile, Destination: { } destination } =>
                new ReceiveFilePenguin(_context, getServerContext())
                {
                    Name = yp.Name,
                    Source = receiveFile,
                    Destination = destination,
                },
            { Workflow: { } workflow } => new RunWorkflowPenguin(
                _context,
                WaddleWorkflow.FromWorkflowName(workflow),
                depth + 1
            )
            {
                Name = yp.Name,
            },
            { Children: { } children } => new RunWorkflowPenguin(_context, children, depth + 1)
            {
                Name = yp.Name,
            },
            _ => throw new ArgumentException(
                $"The workflow contains invalid penguins. {(yp.Name is not null ? "Invalid penguin:" : "")} {yp.Name}"
            ),
        };
        _context.Logger?.LogDebug("Created IPenguin {name}", p.Name);
        p.TimeoutMs = yp.TimeoutMs;
        p.IgnoreError = yp.IgnoreError;
        p.State = PenguinState.Idle;
        return p;
    }
}
