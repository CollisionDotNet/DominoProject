using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.ViewModels
{
    // Used for validation on Views.Account.Registration
    public class RegistrationViewModel
    {
        [Required(ErrorMessage = "Не указано имя")]
        [StringLength(maximumLength:16, MinimumLength = 2, ErrorMessage = "Имя должно иметь длину от 2 до 16 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Не указана фамилия")]
        [StringLength(maximumLength: 33, MinimumLength = 2, ErrorMessage = "Фамилия должна иметь длину от 2 до 33 символов")]
        public string Surname { get; set; }       

        [Required(ErrorMessage = "Не указан Email")]
        [EmailAddress(ErrorMessage = "Email указан некорректно")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Не указан номер группы")]
        public string Group { get; set; }

        [Required(ErrorMessage = "Не указан пароль")]
        [RegularExpression(".{6,}", ErrorMessage = "Пароль должен иметь длину от 6 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

    }
}
