using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Models
{
    public class ResetPasswordResponseDto
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public ResetPasswordResponseDto()
        {
            IsSuccess = false;
            ErrorMessage = string.Empty;
        }
    }
}
