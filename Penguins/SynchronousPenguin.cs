using Waddle.Config;

namespace Penguins;

public abstract class SynchronousPenguin(WaddleContext context) : PenguinBase(context)
{
    public abstract void Execute();
}