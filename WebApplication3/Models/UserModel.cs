using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication3.Models
{
    public class UserModel
    {
        [Display(Name = "User Name")]
        [Required]
        public string userName { get; set; }
        [Display(Name = "Password")]
        [Required]
        public string password { get; set; }
        [Display(Name = "Confirm Password")]
        [Required]
        public string Confirmpassword { get; set; }
    }
}