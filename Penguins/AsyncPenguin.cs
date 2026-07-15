using Waddle.Config;

namespace Penguins
{
    public abstract class AsyncPenguin(WaddleContext context) : PenguinBase(context)
    {
        public abstract Task Execute();
    }
}