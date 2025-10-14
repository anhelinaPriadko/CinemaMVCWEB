using System.ComponentModel.DataAnnotations;

namespace CinemaInfrastructure.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
        [Display (Name ="Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
        [Display(Name = "Рік народження")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        [Display(Name = "Підтвердження паролю")]
        [DataType(DataType.Password)]
        public string PasswordConfirm { get; set; }

        [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Ім'я має бути від 3 до 30 символів")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Поле не повинно бути порожнім!")]
        [DataType(DataType.Date)]
        [CheckDateBirth(14)]
        public DateOnly DateOfBirth { get; set; }
    }

    public class CheckDateBirth : ValidationAttribute
    {
        private readonly int _min_age;

        public CheckDateBirth(int min_age)
        {
            _min_age = min_age;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if(value is DateOnly dateTimeValue)
            {
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                int years = today.Year - dateTimeValue.Year;
                int months = today.Month - dateTimeValue.Month;
                int days = today.Day - dateTimeValue.Day;

                // Якщо день народження ще не настав у цьому році
                if (months < 0 || (months == 0 && days < 0))
                {
                    years--;
                }

                if(years < _min_age)
                {
                    return new ValidationResult("Користувач має бути не молодше 14 років!");
                }
            }

            return ValidationResult.Success;
        }
    }

}
