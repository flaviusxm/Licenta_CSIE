using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace AskNLearn.Application.Common.Attributes
{
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName ?? "")?.ToLower();
                if (string.IsNullOrEmpty(extension) || !_extensions.Contains(extension))
                {
                    return new ValidationResult(GetErrorMessage());
                }
            }

            return ValidationResult.Success;
        }

        public string GetErrorMessage()
        {
            return $"This file extension is not allowed! Allowed extensions are: {string.Join(", ", _extensions)}";
        }
    }
}
