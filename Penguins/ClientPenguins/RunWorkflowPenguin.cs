using Microsoft.Extensions.Logging;
using Penguins.ClientPenguins;
using Penguins.ServerPenguins;
using Waddle.Config;
using Waddle.Config.Exceptions;

#region ReadmeInfo
// Runs a workflow from a filepath or a list of penguins
// `workflow` (string), `children` (List\<IPenguin\>)
#endregion

namespace Penguins
{
    internal partial class PenguinMatcher
    {
        [PenguinMapping(["Workflow", "Children"], "RunWorkflowPenguin")]
        public IPenguin MatchRunWorkflowPenguin(YamlPenguin yp)
        {
            return yp switch
            {
                { Workflow: not null, Children: not null } => throw new InvalidPenguinException(
                    "Workflow penguin can define either `Workflow` or `Children` but not both."
                ),
                { Workflow: string workflow } => new RunWorkflowPenguin(
                    context,
                    WaddleWorkflow.FromWorkflowName(
                        workflow,
                        out string sourceFile,
                        context.Logger
                    ),
                    sourceFile,
                    depth + 1
                )
                {
                    Name = yp.Name,
                },
                { Children: List<YamlPenguin> workflow } => new RunWorkflowPenguin(
                    context,
                    workflow
                )
                {
                    Name = yp.Name,
                },
                _ => throw new NoMatchException(),
            };
        }
    }
}

namespace Penguins.ClientPenguins
{
    public class RunWorkflowPenguin : PenguinBase
    {
        public List<YamlPenguin> Workflow;
        public string? Source;
        public List<IPenguin> Penguins = [];
        public Action? OnPenguinsChange;

        private bool _usesServer;
        private readonly PenguinMatcher _matcher;

        public RunWorkflowPenguin(
            WaddleContext context,
            List<YamlPenguin> workflow,
            string? sourceFile = null,
            int depth = 0
        )
            : base(context)
        {
            if (depth >= 255)
            {
                throw new StackOverflowException(
                    "Nesting workflows deeper than 255 levels is not allowed."
                );
            }

            Source = sourceFile;
            Workflow = workflow;
            _context = context;
            _matcher = new(_context, depth);

            string? dir = !string.IsNullOrWhiteSpace(Source) ? Path.GetDirectoryName(Source) : null;
            using var _ = new Cd(dir, context.Logger);
            foreach (YamlPenguin yp in Workflow)
            {
                IPenguin p = toIPenguin(yp);
                Penguins.Add(p);
            }
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            using var _ = new Cd(Path.GetDirectoryName(Source), _context.Logger);

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

                    p.ExecutePre();
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
                        if (IgnoreError)
                        {
                            State = PenguinState.IgnoredError;
                            Status = e.Message;
                        }
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

        private IPenguin toIPenguin(YamlPenguin yp)
        {
            IPenguin p = _matcher.Match(yp);
            if (p is ServerPenguinBase)
            {
                _usesServer = true;
            }

            _context.Logger?.LogDebug("Created IPenguin {name}", p.Name);
            p.TimeoutMs = yp.TimeoutMs;
            p.IgnoreError = yp.IgnoreError;
            p.State = PenguinState.Idle;
            return p;
        }
    }

    // Helper class run a bit of code in a different directory
    file class Cd : IDisposable
    {
        private readonly string? _previousDir;
        private readonly ILogger? _logger;

        public Cd(string? dir, ILogger? logger)
        {
            _logger = logger;
            if (string.IsNullOrWhiteSpace(dir))
            {
                _logger?.LogTrace("Not changing directory because dir was null");
                return;
            }
            _previousDir = Directory.GetCurrentDirectory();
            _logger?.LogDebug("Current directory: {dir}", _previousDir);
            _logger?.LogDebug("Changing directory: {dir}", dir);
            Directory.SetCurrentDirectory(dir);
        }

        public void Dispose()
        {
            if (string.IsNullOrWhiteSpace(_previousDir))
            {
                return;
            }
            _logger?.LogDebug("Restoring directory: {dir}", _previousDir);
            Directory.SetCurrentDirectory(_previousDir);
        }
    }
}
