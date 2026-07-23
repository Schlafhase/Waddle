namespace Waddle.Config.Exceptions;

public class NoPenguinMatchedException : Exception
{
    public NoPenguinMatchedException() { }

    public NoPenguinMatchedException(string? message)
        : base(message) { }

    public NoPenguinMatchedException(string? message, Exception? innerException)
        : base(message, innerException) { }
}