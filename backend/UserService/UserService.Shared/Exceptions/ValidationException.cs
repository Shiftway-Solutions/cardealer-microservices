using System.Collections.Generic;

namespace UserService.Shared.Exceptions
{
    public class ValidationException : AppException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Validation failed", 422)
        {
            Errors = errors;
        }
    }
}
