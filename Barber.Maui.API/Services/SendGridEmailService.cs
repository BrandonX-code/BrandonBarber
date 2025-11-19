using SendGrid;
using SendGrid.Helpers.Mail;

namespace Barber.Maui.API.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"]!;
            _fromEmail = configuration["EmailSettings:FromEmail"]!;
            _fromName = configuration["EmailSettings:FromName"]!;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var subject = "Recuperación de Contraseña - Barber Go";
                var htmlContent = GetEmailTemplate(userName, resetToken);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                Console.WriteLine($"📧 SendGrid Status: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSolicitudAprobadaEmailAsync(string toEmail, string nombre, string linkRegistro)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var subject = "Solicitud de Administrador Aprobada - Barber Go";
                var htmlContent = GetSolicitudAprobadaTemplate(nombre, linkRegistro);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSolicitudRechazadaEmailAsync(string toEmail, string nombre, string motivo)
        {
            try
            {
                Console.WriteLine($"🔑 API Key (primeros 10 chars): {_apiKey?.Substring(0, Math.Min(10, _apiKey?.Length ?? 0))}");
                Console.WriteLine($"📧 From Email: {_fromEmail}");
                Console.WriteLine($"📧 To Email: {toEmail}");

                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var subject = "Solicitud de Administrador Rechazada - Barber Go";
                var htmlContent = GetSolicitudRechazadaTemplate(nombre, motivo);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
                var response = await client.SendEmailAsync(msg);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error completo: {ex.Message}");
                Console.WriteLine($"❌ InnerException: {ex.InnerException?.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private string GetEmailTemplate(string userName, string resetToken)
        {
            return $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>Recuperación de Contraseña</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background: linear-gradient(135deg, #0E2A36, #FF6F91); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                            .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                            .token-box {{ background: #E0F7FA; padding: 20px; margin: 20px 0; border-left: 4px solid #FF6F91; font-family: monospace; font-size: 18px; text-align: center; }}
                            .button {{ display: inline-block; background: #FF6F91; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                            .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>🔒 Recuperación de Contraseña</h1>
                                <h2>Barber Go</h2>
                            </div>
                            <div class='content'>
                                <h2>¡Hola {userName}!</h2>
                                <p>Hemos recibido una solicitud para restablecer tu contraseña. Si no fuiste tú, puedes ignorar este correo.</p>
            
                                <p><strong>Tu código de recuperación es:</strong></p>
                                <div class='token-box'>
                                    {resetToken}
                                </div>
            
                                <p><strong>⏰ Este código expira en 30 minutos.</strong></p>
            
                                <p>Para restablecer tu contraseña:</p>
                                <ol>
                                    <li>Abre la aplicación Barber Go</li>
                                    <li>Ve a la pantalla de recuperación de contraseña</li>
                                    <li>Ingresa tu email y el código de arriba</li>
                                    <li>Establece tu nueva contraseña</li>
                                </ol>
            
                                <p>Si tienes problemas, contacta a nuestro equipo de soporte.</p>
                            </div>
                            <div class='footer'>
                                <p>Este es un email automático, por favor no responder.</p>
                                <p>&copy; 2024 Barber Go. Todos los derechos reservados.</p>
                            </div>
                        </div>
                    </body>
                    </html>";
        }

        private string GetSolicitudAprobadaTemplate(string nombre, string linkRegistro)
        {
            return $@"
             <html>
             <body>
             <h2>¡Hola {nombre}!</h2>
             <p>Tu solicitud para ser administrador ha sido <b>aprobada</b>.</p>
             <p>Haz clic en el siguiente enlace para completar tu registro como administrador y crear tu cuenta:</p>
             <a href='{linkRegistro}' style='background:#4CAF50;color:white;padding:10px20px;text-decoration:none;border-radius:5px;'>Registrarse como Administrador</a>
             <p>Si tienes dudas, responde a este correo.</p>
             <br>
             <small>Barber Go App</small>
             </body>
             </html>";
        }

        private string GetSolicitudRechazadaTemplate(string nombre, string motivo)
        {
            return $@"
             <html>
             <body>
             <h2>¡Hola {nombre}!</h2>
             <p>Lamentamos informarte que tu solicitud para ser administrador ha sido <b>rechazada</b>.</p>
             <p>Motivo: <b>{motivo}</b></p>
             <p>Si tienes dudas, responde a este correo.</p>
             <br>
             <small>Barber Go App</small>
             </body>
             </html>";
        }
    }
}