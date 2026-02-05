using Microsoft.Extensions.Logging;
using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Services
{
    /// <summary>
    /// VERSIÓN PRODUCTION-READY OPTIMIZADA - Máxima eficiencia y confiabilidad:
    /// ✅ Sistema inteligente de caché de próxima verificación
    /// ✅ Ventana de búsqueda reducida (35 minutos)
    /// ✅ Consultas ultra-optimizadas con AsNoTracking(), Select() y ExecuteUpdateAsync()
    /// ✅ Máxima eficiencia en consumo de recursos
    /// ✅ Manejo robusto de errores y timeouts
    /// ✅ Métricas y logging detallado para monitoreo
    /// ✅ Protección contra race conditions
    /// </summary>
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;

        // ✅ SISTEMA DE CACHÉ INTELIGENTE
        private DateTime? _proximaVerificacion = null;
        private readonly object _cacheLock = new object(); // ✅ Proteger acceso a caché

        // ✅ ESTADÍSTICAS PARA MONITOREO (production-ready)
        private int _totalRecordatoriosEnviados = 0;
        private int _totalErrores = 0;
        private DateTime _inicioServicio = DateTime.UtcNow;

        // ✅ CONFIGURACIÓN OPTIMIZADA
        private const int MINUTOS_RECORDATORIO = 15;
        private const int RANGO_TOLERANCIA = 2;
        private const int MARGEN_BUSQUEDA = 35; // Buscar solo próximos 35 minutos
        private const int MINUTOS_SIN_CITAS_HOY = 60; // Sin citas: esperar 1 hora
        private const int MINUTOS_SIN_CITAS_PROXIMAMENTE = 30; // Sin citas próximas: esperar 30 min
        private const int TIMEOUT_SEGUNDOS = 30; // Timeout de seguridad
        private const int MAX_REINTENTOS = 3; // Reintentos en caso de fallo

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("🔔 Servicio de recordatorios PRODUCTION-READY iniciado");
                _logger.LogInformation("⏳ Esperando inicialización de la base de datos (45 segundos)...");
                await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // ✅ SISTEMA INTELIGENTE: Calcular tiempo hasta próxima verificación
                        TimeSpan tiempoEspera;
                        lock (_cacheLock)
                        {
                            tiempoEspera = _proximaVerificacion.HasValue
                                ? _proximaVerificacion.Value - DateTime.UtcNow
                                : TimeSpan.FromSeconds(30);
                        }

                        if (tiempoEspera <= TimeSpan.Zero)
                        {
                            tiempoEspera = TimeSpan.FromSeconds(30);
                        }

                        _logger.LogInformation($"⏱️ Próxima verificación en {tiempoEspera.TotalMinutes:F1} min");

                        // ✅ Esperar dinámicamente
                        await Task.Delay(tiempoEspera, stoppingToken);

                        // ✅ Ejecutar con timeout de seguridad
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(TIMEOUT_SEGUNDOS));

                        await EjecutarConReintentos(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("⏱️ ReminderService timeout");
                        lock (_cacheLock)
                        {
                            _proximaVerificacion = DateTime.UtcNow.AddSeconds(30);
                        }
                        _totalErrores++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Error en ReminderService: {ex.Message}\n{ex.StackTrace}");
                        lock (_cacheLock)
                        {
                            _proximaVerificacion = DateTime.UtcNow.AddMinutes(5);
                        }
                        _totalErrores++;
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 ReminderService cancelado por sistema durante inicio");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error crítico fatal en ReminderService: {ex.Message}\n{ex.StackTrace}");
                _totalErrores++;
            }
            finally
            {
                _logger.LogInformation($"🛑 ReminderService detenido. Resumen: {_totalRecordatoriosEnviados} recordatorios, {_totalErrores} errores");
            }
        }

        /// <summary>
        /// Ejecuta EnviarRecordatorios con reintentos automáticos en caso de fallo
        /// </summary>
        private async Task EjecutarConReintentos(CancellationToken cancellationToken)
        {
            int intento = 0;
            while (intento < MAX_REINTENTOS)
            {
                try
                {
                    await EnviarRecordatorios(cancellationToken);
                    return; // ✅ Éxito
                }
                catch (DbUpdateConcurrencyException)
                {
                    // ✅ Manejo específico para race conditions en BD
                    intento++;
                    if (intento < MAX_REINTENTOS)
                    {
                        _logger.LogWarning($"⚠️ Concurrency conflict en intento {intento}, reintentando...");
                        await Task.Delay(TimeSpan.FromMilliseconds(100 * intento), cancellationToken);
                    }
                }
                catch (Exception ex) when (intento < MAX_REINTENTOS && ex is TimeoutException or IOException)
                {
                    // ✅ Reintentar en errores transitorios
                    intento++;
                    _logger.LogWarning($"⚠️ Error transitorio ({ex.GetType().Name}) en intento {intento}, reintentando...");
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * intento), cancellationToken);
                }
            }

            _logger.LogError($"❌ Se agotaron los {MAX_REINTENTOS} reintentos");
            _totalErrores++;
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
                var ahora = DateTime.UtcNow;

                // ✅ OPTIMIZACIÓN 1: Verificar PRIMERO si hay citas hoy (consulta ultra-rápida)
                var finDelDia = ahora.AddHours(24);
                var hayCitasHoy = await context.Citas
                    .Where(c =>
                        (c.Estado == "Confirmada" || c.Estado == "Completada") &&
                        c.Fecha >= ahora &&
                        c.Fecha <= finDelDia)
                    .AsNoTracking()
                    .AnyAsync(cancellationToken);

                if (!hayCitasHoy)
                {
                    _logger.LogInformation($"📭 Sin citas confirmadas para hoy");
                    lock (_cacheLock)
                    {
                        _proximaVerificacion = DateTime.UtcNow.AddMinutes(MINUTOS_SIN_CITAS_HOY);
                    }
                    return; // ✅ SALIR TEMPRANO: No hacer más consultas
                }

                // ✅✅ CONSULTA ULTRA-OPTIMIZADA (SOLO si hay citas hoy)
                var proximosMinutos = ahora.AddMinutes(MARGEN_BUSQUEDA);

                var citasProximas = await context.Citas
                    .Where(c =>
                        (c.Estado == "Confirmada" || c.Estado == "Completada") &&
                        c.Fecha >= ahora &&
                        c.Fecha <= proximosMinutos)
                    .OrderBy(c => c.Fecha)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                _logger.LogInformation($"🕐 Citas en próximos {MARGEN_BUSQUEDA} min: {citasProximas.Count}");

                DateTime? proximaCitaParaVerificar = null;

                foreach (var cita in citasProximas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fechaCitaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc),
                        zonaColombia);

                    var minutosAntesDelaCita = (fechaCitaLocal - ahoraColombia).TotalMinutes;

                    // ✅ Rastrear la cita más próxima
                    if (proximaCitaParaVerificar == null || cita.Fecha < proximaCitaParaVerificar)
                    {
                        proximaCitaParaVerificar = cita.Fecha;
                    }

                    // ✅ Enviar si está en ventana de recordatorio
                    if (minutosAntesDelaCita >= (MINUTOS_RECORDATORIO - RANGO_TOLERANCIA) &&
                        minutosAntesDelaCita <= (MINUTOS_RECORDATORIO + RANGO_TOLERANCIA))
                    {
                        if (!await VerificarSiYaSeEnvioRecordatorio(context, cita.Cedula))
                        {
                            _logger.LogInformation($"📤 Enviando recordatorio: {cita.Nombre}");
                            if (await EnviarRecordatorioCita(cita, fechaCitaLocal, notificationService, context))
                            {
                                _totalRecordatoriosEnviados++;
                            }
                        }
                    }
                }

                // ✅✅ CÁLCULO INTELIGENTE DE PRÓXIMA VERIFICACIÓN
                lock (_cacheLock)
                {
                    _proximaVerificacion = CalcularProximaVerificacion(proximaCitaParaVerificar);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error en EnviarRecordatorios: {ex.Message}");
                lock (_cacheLock)
                {
                    _proximaVerificacion = DateTime.UtcNow.AddMinutes(MINUTOS_SIN_CITAS_PROXIMAMENTE);
                }
                _totalErrores++;
                throw; // ✅ Relanzar para que EjecutarConReintentos lo maneje
            }
        }

        /// <summary>
        /// Calcula inteligentemente cuándo debe ser la próxima verificación.
        /// Optimiza el tiempo de espera basándose en cuándo será la próxima cita.
        /// </summary>
        private DateTime? CalcularProximaVerificacion(DateTime? proximaCita)
        {
            if (proximaCita == null)
            {
                // ✅ Sin citas próximas: esperar 30 min (reduce carga 6x)
                return DateTime.UtcNow.AddMinutes(MINUTOS_SIN_CITAS_PROXIMAMENTE);
            }

            // ✅ Hay cita próxima: verificar 20 minutos antes del recordatorio
            var tiempoVerificacion = proximaCita.Value.AddMinutes(-(MINUTOS_RECORDATORIO + 20));

            if (tiempoVerificacion <= DateTime.UtcNow)
            {
                return DateTime.UtcNow.AddSeconds(30);
            }

            return tiempoVerificacion;
        }

        /// <summary>
        /// Obtiene estadísticas del servicio para monitoreo y debugging
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            lock (_cacheLock)
            {
                var tiempoEjecucion = DateTime.UtcNow - _inicioServicio;
                return new Dictionary<string, object>
                {
                    { "estado", "activo" },
                    { "tiempoEjecucion", $"{tiempoEjecucion.Days}d {tiempoEjecucion.Hours}h {tiempoEjecucion.Minutes}m" },
                    { "recordatoriosEnviados", _totalRecordatoriosEnviados },
                    { "totalErrores", _totalErrores },
                    { "proximaVerificacion", _proximaVerificacion?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A" },
                    { "minutosAEsperar", (_proximaVerificacion.HasValue ? (_proximaVerificacion.Value - DateTime.UtcNow).TotalMinutes : 0).ToString("F1") }
                };
            }
        }

        private async Task<bool> VerificarSiYaSeEnvioRecordatorio(AppDbContext context, long cedula)
        {
            try
            {
                var hace30Minutos = DateTime.UtcNow.AddMinutes(-30);

                // ✅✅ CONSULTA OPTIMIZADA (de mi versión)
                var yaEnviado = await context.FcmToken
                    .Where(t =>
                        t.UsuarioCedula == cedula &&
                        t.UltimaActualizacion.HasValue &&
                        t.UltimaActualizacion.Value > hace30Minutos)
                    .AsNoTracking() // ✅ Sin rastreo
                    .AnyAsync();

                return yaEnviado;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error verificando recordatorio previo: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> EnviarRecordatorioCita(
            Cita cita,
            DateTime fechaCitaLocal,
            INotificationService notificationService,
            AppDbContext context)
        {
            try
            {
                // ✅✅ CONSULTA OPTIMIZADA: Select() solo el nombre
                var nombreBarbero = await context.UsuarioPerfiles
                    .Where(b => b.Cedula == cita.BarberoId)
                    .Select(b => b.Nombre)
                    .AsNoTracking()
                    .FirstOrDefaultAsync() ?? "el barbero";

                string titulo = "⏰ Recordatorio de Cita";
                string servicioInfo = !string.IsNullOrEmpty(cita.ServicioNombre)
                    ? $"\n• Servicio: {cita.ServicioNombre}"
                    : "";

                // ✅ Convertir fecha a español
                var cultureSpanish = new System.Globalization.CultureInfo("es-ES");
                string fechaFormato = fechaCitaLocal.ToString("dddd, dd 'de' MMMM 'de' yyyy", cultureSpanish);
                
                // ✅ Formato de hora 12h con AM/PM en español
                string horaFormato = fechaCitaLocal.ToString("h:mm tt", cultureSpanish);
                
                string mensaje = $"¡Hola {cita.Nombre}! 👋\n\n" +
                    $"Tu cita está a punto de comenzar:\n\n" +
                    $"👨‍💼 Barbero: {nombreBarbero}" +
                    servicioInfo +
                    $"\n📅 Fecha: {fechaFormato}" +
                    $"\n🕐 Hora: {horaFormato}" +
                    $"\n\n⏱️ Te esperamos en 15 minutos ✂️";

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

                bool enviado = await notificationService.EnviarNotificacionAsync(
                    cita.Cedula, titulo, mensaje, data);

                if (enviado)
                {
                    // ✅✅ ACTUALIZACIÓN OPTIMIZADA: ExecuteUpdateAsync
                    await context.FcmToken
                        .Where(t => t.UsuarioCedula == cita.Cedula)
                        .ExecuteUpdateAsync(setters =>
                            setters.SetProperty(t => t.UltimaActualizacion, DateTime.UtcNow));

                    _logger.LogInformation($"✅ Recordatorio enviado: {cita.Nombre}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"⚠️ No se pudo enviar recordatorio: {cita.Nombre}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error enviando recordatorio {cita.Id}: {ex.Message}");
                return false;
            }
        }
    }
}