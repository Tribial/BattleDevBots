using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DevBots.Shared.BindingModels
{
    public class LoginBindingModel
    {
        [Required]
        public string EmailOrUsername { get; set; }

        [Required]
        [PasswordPropertyText]
        public string Password { get; set; }
    }
}
