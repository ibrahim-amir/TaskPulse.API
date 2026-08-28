using Microsoft.Identity.Client;
using TaskPulse.API.ExceptionHandling.ExceptionHandlers;

namespace TaskPulse.API.Repository.Infrastructure
{
    public class BaseServiceClass 
    {
        public readonly CustomExceptionHandler _customExceptionHandler;
        public BaseServiceClass(CustomExceptionHandler customExceptionHandler)
        {
            _customExceptionHandler = customExceptionHandler;
        }
    }
}
