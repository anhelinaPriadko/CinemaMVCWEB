using System;
using System.ComponentModel.DataAnnotations;

namespace CinemaDomain.Validation
{
    public class MinHourFromNowAttribute : ValidationAttribute
    {
        private readonly int _hoursToAdd;
        private readonly int _mounthToAdd;

        public MinHourFromNowAttribute(int hoursToAdd, int mounthToAdd)
        {
            _hoursToAdd = hoursToAdd;
            _mounthToAdd = mounthToAdd;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime dateTimeValue)
            {
                var minAllowedDateTime = DateTime.Now.AddHours(_hoursToAdd);
                if (dateTimeValue < minAllowedDateTime)
                {
                    return new ValidationResult(
                        $"Час сеансу має бути не раніше ніж {minAllowedDateTime:dd.MM.yyyy HH:mm}!"
                    );
                }

                var maxAllowedDateTime = DateTime.Now.AddMonths(_mounthToAdd);
                if(dateTimeValue > maxAllowedDateTime)
                {
                    return new ValidationResult(
                        $"Час сеансу має бути не пізніше ніж {maxAllowedDateTime:dd.MM.yyyy HH:mm}!"
                    );
                }
            }
            return ValidationResult.Success;
        }
    }
}
