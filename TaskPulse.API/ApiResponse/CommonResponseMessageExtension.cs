using TaskPulse.API.CommonResponseMessages;

namespace TaskPulse.API.CommonResponseMessageExtensions
{
    public static class CommonResponseMessageExtension
    {
        public static string ToText(this CommonResponseMessage message) => message switch
        {
            CommonResponseMessage.Ok => "Request completed successfully.",
            CommonResponseMessage.Created => "Resource created successfully.",
            CommonResponseMessage.NotFound => "The requested resource was not found.",
            CommonResponseMessage.ValidationFailed => "One or more validation errors occurred.",
            CommonResponseMessage.Unauthorized => "You are not authorized to perform this action.",
            CommonResponseMessage.ServerError => "An unexpected error occurred.",
            _ => message.ToString()
        };
    }
}
