using System;

namespace Goose
{
    public sealed class FatalStartupException : Exception
    {
        public FatalStartupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
