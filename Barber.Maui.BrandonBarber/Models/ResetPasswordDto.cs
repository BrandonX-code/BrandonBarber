using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Models
{
    public class ResetPasswordDto
    {
        public string UserOrEmail { get; set; }
        public string Code { get; set; }
        public string NewPassword { get; set; }

        public ResetPasswordDto()
        {
            UserOrEmail = string.Empty;
            Code = string.Empty;
            NewPassword = string.Empty;
        }
    }
}
