using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AskNLearn.Application.Common.Attributes
{
    public class AllowedMimeTypesAttribute : ValidationAttribute
    {
        private readonly string[] _mimeTypes;

        public AllowedMimeTypesAttribute(string[] mimeTypes)
        {
            _mimeTypes = mimeTypes;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var contentType = file.ContentType?.ToLower();
                if (string.IsNullOrEmpty(contentType) || !_mimeTypes.Contains(contentType))
                {
                    return new ValidationResult(GetErrorMessage());
                }
            }

            return ValidationResult.Success;
        }

        public string GetErrorMessage()
        {
            return $"This file type is not allowed! Allowed types are: {string.Join(", ", _mimeTypes)}";
        }
    }
}
