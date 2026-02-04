using Microsoft.Extensions.Logging;
using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Services
{
    /// <summary>
    /// Servicio optimizado de recordatorios que solo verifica citas próximas.
    /// Usa un sistema de caché y calcula el próximo tiempo de verificación dinámicamente.
    /// </summary>
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;
        private DateTime? _proximaVerificacion = null;
        private const int MINUTOS_RECORDATORIO = 15; // Enviar recordatorio 15 minutos antes
        private const int RANGO_TOLERANCIA = 2; // ±2 minutos de tolerancia
        private const int MARGEN_BUSQUEDA = 35; // Buscar citas en los próximos 35 minutos (15+20 de margen)

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔔 Servicio de recordatorios optimizado iniciado");

            // ✅ Delay inicial para no sobrecargar al inicio
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ✅ Calcular tiempo hasta la próxima verificación
                    var tiempoEspera = _proximaVerificacion.HasValue
                        ? _proximaVerificacion.Value - DateTime.UtcNow
                        : TimeSpan.FromSeconds(30);

                    // Asegurar que no sea negativo
                    if (tiempoEspera <= TimeSpan.Zero)
                    {
                        tiempoEspera = TimeSpan.FromSeconds(30);
                    }

                    _logger.LogInformation($"⏱️ Próxima verificación en {tiempoEspera.TotalSeconds:F0} segundos");

                    // ✅ Esperar hasta el próximo tiempo de verificación
                    await Task.Delay(tiempoEspera, stoppingToken);

                    // ✅ Ejecutar verificación con timeout
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    await EnviarRecordatorios(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⏱️ ReminderService timeout");
                    _proximaVerificacion = DateTime.UtcNow.AddSeconds(30);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error en ReminderService: {ex.Message}");
                    _proximaVerificacion = DateTime.UtcNow.AddSeconds(30);
                }
            }
        }

        private async Task EnviarRecordatorios(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                var ahoraColombia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaColombia);

                // ✅ OPTIMIZACIÓN: Solo buscar citas en los próximos MARGEN_BUSQUEDA minutos
                // Esto reduce significativamente la cantidad de datos a procesar
                var ahora = DateTime.UtcNow;
                var proximosMinutos = ahora.AddMinutes(MARGEN_BUSQUEDA);

                var citasProximas = await context.Citas
                    .Where(c => (c.Estado == "Confirmada" || c.Estado == "Completada")
                        && c.Fecha >= ahora
                        && c.Fecha <= proximosMinutos)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation($"🕐 Hora actual Colombia: {ahoraColombia:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"📊 Citas próximas encontradas: {citasProximas.Count}");

                DateTime? proximaCitaParaVerificar = null;

                foreach (var cita in citasProximas)
                {
                    var fechaCitaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc), zonaColombia);

                    var minutosAntesDelaCita = (fechaCitaLocal - ahoraColombia).TotalMinutes;

                    // ✅ Calcular la próxima cita para verificación
                    if (proximaCitaParaVerificar == null || cita.Fecha < proximaCitaParaVerificar)
                    {
                        proximaCitaParaVerificar = cita.Fecha;
                    }

                    // ✅ Enviar recordatorio si está en el rango de 15 minutos (±RANGO_TOLERANCIA)
                    if (minutosAntesDelaCita >= (MINUTOS_RECORDATORIO - RANGO_TOLERANCIA)
                        && minutosAntesDelaCita <= (MINUTOS_RECORDATORIO + RANGO_TOLERANCIA))
                    {
                        _logger.LogInformation($"⚡ Cita {cita.Id} en rango de recordatorio: {minutosAntesDelaCita:F2} minutos");

                        var yaEnviado = await VerificarSiYaSeEnvioRecordatorio(context, cita.Cedula);

                        if (!yaEnviado)
                        {
                            _logger.LogInformation($"📤 Enviando recordatorio para cita de {cita.Nombre} (ID: {cita.Id})");
                            await EnviarRecordatorioCita(cita, fechaCitaLocal, notificationService, context);
                        }
                    }
                }

                // ✅ Calcular próxima verificación
                _proximaVerificacion = CalcularProximaVerificacion(proximaCitaParaVerificar, ahoraColombia);
                _logger.LogInformation($"✅ Próxima verificación programada para: {_proximaVerificacion:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en EnviarRecordatorios: {ex.Message}");
                _proximaVerificacion = DateTime.UtcNow.AddSeconds(30);
            }
        }

        /// <summary>
        /// Calcula el próximo tiempo en que se debe verificar recordatorios.
        /// Si hay citas próximas, verifica 20 minutos antes de la primera cita.
        /// Si no hay citas, verifica cada 5 minutos.
        /// </summary>
        private DateTime? CalcularProximaVerificacion(DateTime? proximaCita, DateTime ahora)
        {
            if (proximaCita == null)
            {
                // No hay citas próximas, verificar en 5 minutos
                return DateTime.UtcNow.AddMinutes(5);
            }

            // Hay una cita próxima, verificar 20 minutos antes de que se envíe el recordatorio
            // Recordatorio se envía 15 minutos antes, así que verificamos 35 minutos antes
            var tiempoVerificacion = proximaCita.Value.AddMinutes(-(MINUTOS_RECORDATORIO + 20));

            // Asegurar que no sea en el pasado
            if (tiempoVerificacion <= DateTime.UtcNow)
            {
                return DateTime.UtcNow.AddSeconds(30);
            }

            return tiempoVerificacion;
        }

        private async Task<bool> VerificarSiYaSeEnvioRecordatorio(AppDbContext context, long cedula)
        {
            try
            {
                // ✅ SI HAY UN TOKEN CON ACTUALIZACIÓN EN LOS ÚLTIMOS 20 MINUTOS, YA SE ENVIÓ
                var yaEnviado = await context.FcmToken
                    .Where(t => t.UsuarioCedula == cedula)
                    .AnyAsync(t => t.UltimaActualizacion.HasValue &&
                        t.UltimaActualizacion.Value > DateTime.UtcNow.AddMinutes(-20));

                _logger.LogDebug($"🔍 Verificación de recordatorio para cedula {cedula}: {(yaEnviado ? "YA ENVIADO" : "NO ENVIADO")}");
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
            INotificationService notificationService,
            AppDbContext context)
        {
            try
            {
                var barbero = await context.UsuarioPerfiles
                    .FirstOrDefaultAsync(b => b.Cedula == cita.BarberoId);

                string nombreBarbero = barbero?.Nombre ?? "el barbero";

                string titulo = "⏰ Recordatorio de Cita";
                string servicioInfo = !string.IsNullOrEmpty(cita.ServicioNombre) ? $"• Servicio: {cita.ServicioNombre}" : "";

                string mensaje = $"¡Hola {cita.Nombre}! 👋"
                    + $"Tu cita está a punto de comenzar:"
                    + $"👨‍💼 Barbero: {nombreBarbero}"
                    + servicioInfo + ""
                    + $"📅 Fecha: {fechaCitaLocal:dddd, dd 'de' MMMM 'de' yyyy}"
                    + $"🕐 Hora: {fechaCitaLocal:hh:mm tt}"
                    + $"⏱️ Te esperamos en 15 minutos ✂️";

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

                bool enviado = await notificationService.EnviarNotificacionAsync(cita.Cedula, titulo, mensaje, data);

                if (enviado)
                {
                    var tokens = await context.FcmToken.Where(t => t.UsuarioCedula == cita.Cedula).ToListAsync();

                    if (tokens.Any())
                    {
                        foreach (var token in tokens)
                        {
                            token.UltimaActualizacion = DateTime.UtcNow;
                        }
                        await context.SaveChangesAsync();
                    }

                    _logger.LogInformation($"✅ Recordatorio enviado a {cita.Nombre} para cita a las {fechaCitaLocal:hh:mm tt}");
                }
                else
                {
                    _logger.LogWarning($"⚠️ No se envió recordatorio a {cita.Nombre} - Sin tokens registrados");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error enviando recordatorio para cita {cita.Id}: {ex.Message}");
            }
        }
    }
}
