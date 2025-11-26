namespace Barber.Maui.API.Services
{
    public interface INotificationService
    {
        Task<bool> EnviarNotificacionAsync(long usuarioCedula, string titulo, string mensaje, Dictionary<string, string>? data = null);
        Task<bool> RegistrarTokenAsync(long usuarioCedula, string token);
    }
}