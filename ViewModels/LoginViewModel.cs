using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.ViewModels
{
    // Used for validation on Views.Account.Login
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Не указан Email")]
        [EmailAddress(ErrorMessage = "Email указан некорректно")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Не указан пароль")]
        [RegularExpression(".{6,}", ErrorMessage = "Пароль должен иметь длину от 6 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
