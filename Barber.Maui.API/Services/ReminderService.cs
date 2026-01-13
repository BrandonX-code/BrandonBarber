using Microsoft.Extensions.Logging;
using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Services
{
    /// <summary>
    /// Servicio en segundo plano que envía recordatorios de citas 15 minutos antes.
    /// Se ejecuta cada minuto para verificar si hay citas próximas.
    /// </summary>
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Verificar cada minuto

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔔 Servicio de recordatorios de citas iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EnviarRecordatorios();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error en ReminderService: {ex.Message}{ex.StackTrace}");
                }
            }

            _logger.LogInformation("🛑 Servicio de recordatorios de citas detenido");
        }

        private async Task EnviarRecordatorios()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // ✅ USAR ZONA HORARIA DE COLOMBIA
                var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                var ahoraColombia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaColombia);

                // ✅ BUSCAR CITAS CONFIRMADAS O COMPLETADAS EN LOS PRÓXIMOS 30 MINUTOS
                // (Ampliamos el rango para evitar perder citas por sincronización)
                var todasLasCitas = await context.Citas.Where(c => c.Estado == "Confirmada" || c.Estado == "Completada").ToListAsync();

                foreach (var cita in todasLasCitas)
                {
                    // ✅ CONVERTIR FECHA A ZONA HORARIA DE COLOMBIA
                    var fechaCitaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc),
                    zonaColombia);

                    // ✅ CALCULAR DIFERENCIA EN MINUTOS
                    var minutosAntesDelaCita = (fechaCitaLocal - ahoraColombia).TotalMinutes;

                    _logger.LogDebug($"📊 Cita de {cita.Nombre}: en {minutosAntesDelaCita:F2} minutos");

                    // ✅ ENVIAR RECORDATORIO SI LA CITA ES EN 15 MINUTOS (rango: 14-16 minutos)
                    if (minutosAntesDelaCita >= 14 && minutosAntesDelaCita <= 16)
                    {
                        // ✅ VERIFICAR SI YA SE ENVIÓ RECORDATORIO RECIENTEMENTE
                        var yaEnviado = await VerificarSiYaSeEnvioRecordatorio(context, cita.Cedula);

                        if (!yaEnviado)
                        {
                            _logger.LogInformation($"📤 Enviando recordatorio para cita de {cita.Nombre} (ID: {cita.Id})");
                            await EnviarRecordatorioCita(cita, fechaCitaLocal, notificationService, context, ahoraColombia);
                        }
                        else
                        {
                            _logger.LogDebug($"ℹ️ Recordatorio ya enviado para {cita.Nombre} (ID: {cita.Id})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en EnviarRecordatorios: {ex.Message}");
            }
        }

        private async Task<bool> VerificarSiYaSeEnvioRecordatorio(AppDbContext context, long cedula)
        {
            try
            {
                var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                var ahoraColombia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaColombia);
                var hace20Minutos = ahoraColombia.AddMinutes(-20);

                // ✅ SI HAY UN TOKEN CON ACTUALIZACIÓN EN LOS ÚLTIMOS 20 MINUTOS, YA SE ENVIÓ
                var yaEnviado = await context.FcmToken
                    .Where(t => t.UsuarioCedula == cedula)
             .AnyAsync(t => t.UltimaActualizacion.HasValue &&
               t.UltimaActualizacion.Value > DateTime.UtcNow.AddMinutes(-20));

                return yaEnviado;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error verificando recordatorio previo: {ex.Message}");
                return false;
            }
        }

        private async Task EnviarRecordatorioCita(
            Cita cita,
                 DateTime fechaCitaLocal,
                 INotificationService notificationService, AppDbContext context, DateTime ahoraColombia)
        {
            try
            {
                // ✅ OBTENER DATOS DEL BARBERO
                var barbero = await context.UsuarioPerfiles
                         .FirstOrDefaultAsync(b => b.Cedula == cita.BarberoId);

                string nombreBarbero = barbero?.Nombre ?? "el barbero";

                // ✅ CONSTRUIR MENSAJE PROFESIONAL Y CLARO
                string titulo = "⏰ Recordatorio de Cita";

                // Incluir servicio si existe
                string servicioInfo = !string.IsNullOrEmpty(cita.ServicioNombre)
               ? $"• Servicio: {cita.ServicioNombre}"
          : "";

                // ✅ MENSAJE ESTRUCTURADO Y PROFESIONAL
                string mensaje = $"¡Hola {cita.Nombre}! 👋" +
          $"Tu cita está a punto de comenzar:" +
         $"👨‍💼 Barbero: {nombreBarbero}" +
       servicioInfo +
       $"📅 Fecha: {fechaCitaLocal:dddd, dd 'de' MMMM 'de' yyyy}" + $"🕐 Hora: {fechaCitaLocal:hh:mm tt}" +
         $"⏱️ Te esperamos en 15 minutos ✂️";

                // ✅ INCLUIR TODOS LOS DATOS PARA LA APLICACIÓN
                var data = new Dictionary<string, string>
                {
                    { "tipo", "recordatorio_cita" },
                    { "citaId", cita.Id.ToString() },
                    { "clienteNombre", cita.Nombre ?? "" },
                    { "clienteCedula", cita.Cedula.ToString() },
                    { "barberoId", cita.BarberoId.ToString() },
                    { "barberoNombre", nombreBarbero },
                    { "fecha", fechaCitaLocal.ToString("yyyy-MM-dd") },
                    { "hora", fechaCitaLocal.ToString("HH:mm") },
                    { "servicio", cita.ServicioNombre ?? "Sin especificar" },
                    { "precio", (cita.ServicioPrecio ?? 0).ToString("F2") }
                };

                // ✅ ENVIAR NOTIFICACIÓN POR FIREBASE
                bool enviado = await notificationService.EnviarNotificacionAsync(
                        cita.Cedula,
                    titulo,
                    mensaje,
            data
                     );

                if (enviado)
                {
                    // ✅ ACTUALIZAR TIMESTAMP PARA NO REENVIAR
                    var tokens = await context.FcmToken
                 .Where(t => t.UsuarioCedula == cita.Cedula)
                .ToListAsync();

                    if (tokens.Any())
                    {
                        foreach (var token in tokens)
                        {
                            token.UltimaActualizacion = DateTime.UtcNow;
                        }
                        await context.SaveChangesAsync();
                    }

                    _logger.LogInformation(
                      $"✅ Recordatorio enviado a {cita.Nombre} para cita a las {fechaCitaLocal:hh:mm tt} con {nombreBarbero}");
                }
                else
                {
                    _logger.LogWarning(
                     $"⚠️ No se pudo enviar recordatorio a {cita.Nombre} (Cédula: {cita.Cedula}) - Verifica si tiene tokens registrados");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
               $"❌ Error enviando recordatorio para cita {cita.Id} de {cita.Nombre}: {ex.Message}{ex.StackTrace}");
            }
        }
    }
}
