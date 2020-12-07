using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Domain.DTO.Request
{
 public   class VerifyEmailRequest
    {
        [Required(ErrorMessage = "Email not valid")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Token required")]
        public string Token { get; set; }
    }
}
