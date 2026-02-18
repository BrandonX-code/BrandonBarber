//using Microsoft.Extensions.Logging;
//using Barber.Maui.API.Data;
//using Barber.Maui.API.Models;
//using Microsoft.EntityFrameworkCore;

//namespace Barber.Maui.API.Services
//{
//    /// <summary>
//    /// VERSIÓN ULTRA-OPTIMIZADA para Railway Hobby Plan
//    /// Cambios principales:
//    /// 1. Verificaciones dinámicas: solo cuando hay citas programadas
//    /// 2. Consulta ÚNICA ultra-eficiente con todos los datos necesarios
//    /// 3. Cacheo de próxima cita para evitar consultas innecesarias
//    /// 4. Batch processing de notificaciones
//    /// 5. Reducción drástica de queries a la BD
//    /// </summary>
//    public class ReminderService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<ReminderService> _logger;

//        // Sistema de caché mejorado
//        private DateTime? _proximaCitaAgendada = null;
//        private DateTime _ultimaVerificacionCache = DateTime.MinValue;
//        private const int MINUTOS_CACHE_VALIDO = 10; // Renovar caché cada 10 min

//        // Configuración optimizada
//        private const int MINUTOS_RECORDATORIO = 15;
//        private const int RANGO_TOLERANCIA = 2;
//        private const int VENTANA_BUSQUEDA_INICIAL = 120; // 2 horas para cache
//        private const int VENTANA_ENVIO = 35; // Solo para envíos

//        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("🚀 ReminderService ULTRA-OPTIMIZADO iniciado (Railway Hobby)");
//            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    var tiempoEspera = await CalcularTiempoEsperaInteligente();

//                    _logger.LogInformation($"⏱️ Siguiente verificación en {tiempoEspera.TotalMinutes:F1} min");

//                    await Task.Delay(tiempoEspera, stoppingToken);

//                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
//                    cts.CancelAfter(TimeSpan.FromSeconds(25));

//                    await ProcesarRecordatorios(cts.Token);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogWarning("⏱️ Timeout - reiniciando");
//                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError($"❌ Error: {ex.Message}");
//                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
//                }
//            }
//        }

//        /// <summary>
//        /// Calcula inteligentemente cuánto esperar antes de la próxima verificación
//        /// Evita consultas innecesarias a la BD
//        /// </summary>
//        private async Task<TimeSpan> CalcularTiempoEsperaInteligente()
//        {
//            var ahora = DateTime.UtcNow;

//            // ✅ OPTIMIZACIÓN 1: Renovar caché solo si es necesario
//            bool cacheExpirado = (ahora - _ultimaVerificacionCache).TotalMinutes >= MINUTOS_CACHE_VALIDO;

//            if (cacheExpirado || _proximaCitaAgendada == null)
//            {
//                _proximaCitaAgendada = await ObtenerProximaCitaAgendada();
//                _ultimaVerificacionCache = ahora;
//            }

//            // Si no hay citas próximas, esperar bastante tiempo
//            if (_proximaCitaAgendada == null)
//            {
//                _logger.LogInformation("📭 Sin citas en las próximas 2 horas");
//                return TimeSpan.FromMinutes(MINUTOS_CACHE_VALIDO);
//            }

//            // Calcular cuándo verificar (30 min antes del recordatorio)
//            var momentoVerificacion = _proximaCitaAgendada.Value
//                .AddMinutes(-(MINUTOS_RECORDATORIO + 20));

//            var tiempoHastaVerificacion = momentoVerificacion - ahora;

//            // Si ya pasó el momento, verificar ahora
//            if (tiempoHastaVerificacion <= TimeSpan.Zero)
//            {
//                return TimeSpan.FromSeconds(5);
//            }

//            // Limitar espera máxima para no estar demasiado tiempo sin verificar
//            return tiempoHastaVerificacion > TimeSpan.FromMinutes(MINUTOS_CACHE_VALIDO)
//                ? TimeSpan.FromMinutes(MINUTOS_CACHE_VALIDO)
//                : tiempoHastaVerificacion;
//        }

//        /// <summary>
//        /// CONSULTA ULTRA-OPTIMIZADA: Solo busca la próxima cita en las próximas 2 horas
//        /// Reduce drasticamente el uso de BD
//        /// </summary>
//        private async Task<DateTime?> ObtenerProximaCitaAgendada()
//        {
//            try
//            {
//                using var scope = _serviceProvider.CreateScope();
//                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//                var ahora = DateTime.UtcNow;
//                var limite = ahora.AddMinutes(VENTANA_BUSQUEDA_INICIAL);

//                var proximaFecha = await context.Citas
//                    .AsNoTracking() // ✅ AsNoTracking ANTES del Where
//                    .Where(c =>
//                        (c.Estado == "Confirmada" || c.Estado == "Completada") &&
//                        c.Fecha >= ahora &&
//                        c.Fecha <= limite)
//                    .OrderBy(c => c.Fecha)
//                    .Select(c => c.Fecha) // ✅ Select al final
//                    .FirstOrDefaultAsync();

//                return proximaFecha == default ? null : (DateTime?)proximaFecha;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"❌ Error obteniendo próxima cita: {ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// PROCESAMIENTO OPTIMIZADO: Una sola query para todo lo necesario
//        /// </summary>
//        private async Task ProcesarRecordatorios(CancellationToken cancellationToken)
//        {
//            try
//            {
//                using var scope = _serviceProvider.CreateScope();
//                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

//                var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
//                var ahoraColombia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaColombia);
//                var ahora = DateTime.UtcNow;
//                var limite = ahora.AddMinutes(VENTANA_ENVIO);

//                // ✅ QUERY ÚNICA OPTIMIZADA: Join con barbero y filtro de recordatorios previos
//                var hace30Min = ahora.AddMinutes(-30);

//                var citasParaNotificar = await context.Citas
//                    .Where(c =>
//                        (c.Estado == "Confirmada" || c.Estado == "Completada") &&
//                        c.Fecha >= ahora &&
//                        c.Fecha <= limite)
//                    .Join(
//                        context.UsuarioPerfiles,
//                        cita => cita.BarberoId,
//                        barbero => barbero.Cedula,
//                        (cita, barbero) => new { cita, barbero.Nombre })
//                    .GroupJoin(
//                        context.FcmToken.Where(t =>
//                            t.UltimaActualizacion.HasValue &&
//                            t.UltimaActualizacion.Value > hace30Min),
//                        x => x.cita.Cedula,
//                        token => token.UsuarioCedula,
//                        (x, tokens) => new
//                        {
//                            x.cita.Id,
//                            x.cita.Cedula,
//                            x.cita.Nombre,
//                            x.cita.Fecha,
//                            x.cita.BarberoId,
//                            BarberoNombre = x.Nombre,
//                            x.cita.ServicioNombre,
//                            x.cita.ServicioPrecio,
//                            RecordatorioEnviado = tokens.Any()
//                        })
//                    .AsNoTracking()
//                    .ToListAsync(cancellationToken);

//                _logger.LogInformation($"📊 Procesando {citasParaNotificar.Count} citas");

//                // ✅ Procesar en batch para reducir operaciones
//                var citasAEnviar = new List<dynamic>();

//                foreach (var cita in citasParaNotificar)
//                {
//                    var fechaCitaLocal = TimeZoneInfo.ConvertTimeFromUtc(
//                        DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc),
//                        zonaColombia);

//                    var minutosAntes = (fechaCitaLocal - ahoraColombia).TotalMinutes;

//                    // Verificar si está en ventana de recordatorio
//                    if (minutosAntes >= (MINUTOS_RECORDATORIO - RANGO_TOLERANCIA) &&
//                        minutosAntes <= (MINUTOS_RECORDATORIO + RANGO_TOLERANCIA) &&
//                        !cita.RecordatorioEnviado)
//                    {
//                        citasAEnviar.Add(new
//                        {
//                            cita.Id,
//                            cita.Cedula,
//                            cita.Nombre,
//                            FechaLocal = fechaCitaLocal,
//                            cita.BarberoId,
//                            cita.BarberoNombre,
//                            cita.ServicioNombre,
//                            cita.ServicioPrecio
//                        });
//                    }
//                }

//                // ✅ Enviar notificaciones en batch
//                if (citasAEnviar.Any())
//                {
//                    await EnviarNotificacionesBatch(citasAEnviar, notificationService, context);
//                }

//                // Actualizar caché
//                _proximaCitaAgendada = await ObtenerProximaCitaAgendada();
//                _ultimaVerificacionCache = ahora;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"❌ Error procesando recordatorios: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Envía notificaciones en batch y actualiza tokens en una sola operación
//        /// </summary>
//        private async Task EnviarNotificacionesBatch(
//            List<dynamic> citas,
//            INotificationService notificationService,
//            AppDbContext context)
//        {
//            var cedulas = new List<long>();

//            foreach (var cita in citas)
//            {
//                try
//                {
//                    string titulo = "⏰ Recordatorio de Cita";
//                    string servicioInfo = !string.IsNullOrEmpty(cita.ServicioNombre)
//                        ? $"\n• Servicio: {cita.ServicioNombre}"
//                        : "";

//                    string mensaje = $"¡Hola {cita.Nombre}! 👋\n\n" +
//                        $"Tu cita está a punto de comenzar:\n\n" +
//                        $"👨‍💼 Barbero: {cita.BarberoNombre}" +
//                        servicioInfo +
//                        $"\n📅 {cita.FechaLocal:dddd, dd 'de' MMMM}" +
//                        $"\n🕐 {cita.FechaLocal:hh:mm tt}" +
//                        $"\n\n⏱️ Te esperamos en 15 minutos ✂️";

//                    var data = new Dictionary<string, string>
//                    {
//                        { "tipo", "recordatorio_cita" },
//                        { "citaId", cita.Id.ToString() },
//                        { "clienteCedula", cita.Cedula.ToString() },
//                        { "barberoId", cita.BarberoId.ToString() },
//                        { "fecha", ((DateTime)cita.FechaLocal).ToString("yyyy-MM-dd") },
//                        { "hora", ((DateTime)cita.FechaLocal).ToString("HH:mm") }
//                    };

//                    bool enviado = await notificationService.EnviarNotificacionAsync(
//                        cita.Cedula, titulo, mensaje, data);

//                    if (enviado)
//                    {
//                        cedulas.Add(cita.Cedula);
//                        _logger.LogInformation($"✅ Enviado: {cita.Nombre}");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError($"❌ Error enviando a {cita.Nombre}: {ex.Message}");
//                }
//            }

//            // ✅ ACTUALIZACIÓN EN BATCH: Una sola operación para todos los tokens
//            if (cedulas.Any())
//            {
//                try
//                {
//                    await context.FcmToken
//                        .Where(t => cedulas.Contains(t.UsuarioCedula))
//                        .ExecuteUpdateAsync(setters =>
//                            setters.SetProperty(t => t.UltimaActualizacion, DateTime.UtcNow));

//                    _logger.LogInformation($"📝 Tokens actualizados: {cedulas.Count}");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError($"❌ Error actualizando tokens: {ex.Message}");
//                }
//            }
//        }
//    }
//}