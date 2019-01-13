using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BaltazarWeb.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "رمز عبور")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "مرا به یاد داشته باش")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }
}
