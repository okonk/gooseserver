
namespace Goose
{
    public sealed class FatalStartupException : Exception
    {
        public FatalStartupException(string message) : base(message) { }

        public FatalStartupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
