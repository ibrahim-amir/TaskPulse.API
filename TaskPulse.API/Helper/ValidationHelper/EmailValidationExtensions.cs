using FluentValidation;
using FluentValidation.Validators;

namespace TaskPulse.API.Helper.ValidationHelper
{
    public static class EmailValidationExtensions
    {
        public static IRuleBuilderOptions<T, string> IsValidEmail<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .EmailAddress(EmailValidationMode.Net4xRegex)
                .WithMessage("Invalid email format.");
        }
    }
}
