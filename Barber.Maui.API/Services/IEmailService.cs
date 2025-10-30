namespace Barber.Maui.API.Services
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
        Task<bool> SendSolicitudAprobadaEmailAsync(string toEmail, string nombre, string linkRegistro);
        Task<bool> SendSolicitudRechazadaEmailAsync(string toEmail, string nombre, string motivo);

    }
}
