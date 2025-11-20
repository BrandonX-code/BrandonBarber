using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts; // Si usas CommunityToolkit para Snackbar

namespace Barber.Maui.BrandonBarber.Utils
{
    public static class AppUtils
    {
        public static bool IsValidEmail(string email)
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }

        public static bool IsPasswordSecure(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;
            if (!password.Any(char.IsUpper))
                return false;
            if (!password.Any(char.IsLower))
                return false;
            if (!password.Any(char.IsDigit))
                return false;
            return true;
        }

        public static async Task MostrarSnackbar(string mensaje, Color background, Color textColor)
        {
            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = background,
                TextColor = textColor,
                CornerRadius = new CornerRadius(30),
                Font = Font.OfSize("Arial", 16),
                CharacterSpacing = 0
            };

            var snackbar = Snackbar.Make(mensaje, duration: TimeSpan.FromSeconds(3), visualOptions: snackbarOptions);
            await snackbar.Show();
        }
    }
}