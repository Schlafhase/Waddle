namespace Waddle.Config.Exceptions;

public class PenguinCustomException : Exception
{
    public PenguinCustomException() { }

    public PenguinCustomException(string? message)
        : base(message) { }

    public PenguinCustomException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
