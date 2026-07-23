using Waddle.Config;

namespace Penguins;

public abstract class ServerPenguinBase(WaddleContext context, WaddleServerContext serverContext)
    : PenguinBase(context)
{
    protected WaddleServerContext _serverContext = serverContext;
}