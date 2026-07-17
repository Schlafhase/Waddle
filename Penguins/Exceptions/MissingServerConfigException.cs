namespace Penguins.Exceptions;

public class MissingServerConfigException : Exception {
    public MissingServerConfigException()
    {
    }

    public MissingServerConfigException(string? message) : base(message)
    {
    }

    public MissingServerConfigException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
