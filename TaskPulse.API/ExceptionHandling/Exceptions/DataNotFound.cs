using System.Net;

namespace TaskPulse.API.ExceptionHandling.Exceptions
{
    public class DataNotFound : Exception
    {
        public DataNotFound(string message) : base(message)
        { }
    }
}
