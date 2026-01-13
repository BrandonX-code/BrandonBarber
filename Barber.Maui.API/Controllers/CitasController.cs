using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Barber.Maui.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/citas")]
[ApiController]
public class CitasController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public CitasController(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private void ConvertirCitaAFormatoLocal(Cita cita)
    {
        cita.Fecha = DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc).ToLocalTime();
    }
    private async Task EnriquecerCitaConServicio(Cita cita)
    {
        if (cita.ServicioId.HasValue)
        {
            var servicio = await _context.Servicios.FindAsync(cita.ServicioId.Value);
            if (servicio != null)
            {
                cita.ServicioNombre = servicio.Nombre;
                cita.ServicioPrecio = servicio.Precio;
                // ✅ AGREGAR LA IMAGEN DEL SERVICIO
                cita.ServicioImagen = servicio.Imagen;
            }
        }
    }

    [HttpGet("Barberos/{idbarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitas(int idbarberia)
    {
        var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idbarberia)
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();

        var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
        var barberoIds = barberos.Select(b => b.Cedula).ToList();

        var citas = await _context.Citas
            .Where(c => barberoIds.Contains(c.BarberoId))
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        foreach (var cita in citas)
        {
            ConvertirCitaAFormatoLocal(cita);
            cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citas);
    }

    [HttpGet("todas")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetTodasLasCitas()
    {
        try
        {
            var citas = await _context.Citas
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            var barberoIds = citas.Select(c => c.BarberoId).Distinct().ToList();
            var barberos = await _context.UsuarioPerfiles
                .Where(b => barberoIds.Contains(b.Cedula))
                .ToDictionaryAsync(b => b.Cedula, b => b.Nombre);

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberos.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener todas las citas.", error = ex.Message });
        }
    }

    [HttpGet("barberia/{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarberia(int idBarberia)
    {
        try
        {
            var barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idBarberia)
                .Select(b => new { b.Cedula, b.Nombre })
                .ToListAsync();

            var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
            var barberoIds = barberos.Select(b => b.Cedula).ToList();

            var citas = await _context.Citas
                .Where(c => barberoIds.Contains(c.BarberoId))
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas de la barbería.", error = ex.Message });
        }
    }

    [HttpGet("by-date-range/{fechaInicio}/{fechaFin}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorRangoFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            fechaInicio = fechaInicio.ToUniversalTime();
            fechaFin = fechaFin.ToUniversalTime();

            var citas = await _context.Citas
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var barberoIds = citas.Select(c => c.BarberoId).Distinct().ToList();
            var barberos = await _context.UsuarioPerfiles
                .Where(b => barberoIds.Contains(b.Cedula))
                .ToDictionaryAsync(b => b.Cedula, b => b.Nombre);

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberos.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas por rango de fechas.", error = ex.Message });
        }
    }

    [HttpGet("by-date-range/{fechaInicio}/{fechaFin}/{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorRangoFechasYBarberia(DateTime fechaInicio, DateTime fechaFin, int idBarberia)
    {
        try
        {
            fechaInicio = fechaInicio.ToUniversalTime();
            fechaFin = fechaFin.ToUniversalTime();

            var barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idBarberia)
                .Select(b => new { b.Cedula, b.Nombre })
                .ToListAsync();

            var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
            var barberoIds = barberos.Select(b => b.Cedula).ToList();

            var citas = await _context.Citas
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && barberoIds.Contains(c.BarberoId))
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas por fecha y barbería.", error = ex.Message });
        }
    }

    [HttpGet("{cedula}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorCedula(long cedula)
    {
        var citas = await _context.Citas
          .Where(c => c.Cedula == cedula)
          .ToListAsync();

        foreach (var cita in citas)
        {
            ConvertirCitaAFormatoLocal(cita);
            var barbero = await _context.UsuarioPerfiles
           .FirstOrDefaultAsync(b => b.Cedula == cita.BarberoId);

            cita.BarberoNombre = barbero?.Nombre ?? "No encontrado";

            // ✅ SIEMPRE ENRIQUECER CON DATOS DEL SERVICIO
            // Esto asegura que incluso citas antiguas muestren datos del servicio
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citas);
    }

    [HttpGet("by-date/{fecha}&{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorFecha(DateTime fecha, int idBarberia)
    {
        // 🔥 CORREGIR: Usar zona horaria de Colombia para la comparación
        var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        
        var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idBarberia && b.Rol == "barbero")
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();

    var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
     var barberoIds = barberos.Select(b => b.Cedula).ToList();

        var citas = await _context.Citas
            .Where(c => barberoIds.Contains(c.BarberoId))
     .ToListAsync(); // Traer todas y filtrar en memoria

        // 🔥 FILTRAR EN MEMORIA usando zona horaria correcta
        var citasFiltradas = citas
        .Where(c => 
            {
          var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc), zonaColombia);
      return fechaLocal.Date == fecha.Date;
       })
 .OrderBy(c => c.Fecha)
            .ToList();

        foreach (var cita in citasFiltradas)
        {
       ConvertirCitaAFormatoLocal(cita);
            cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citasFiltradas);
    }

    [HttpGet("barbero/{barberoId}/fecha/{fecha}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarberoYFecha(long barberoId, DateTime fecha)
    {
        // 🔥 CORREGIR: Usar zona horaria de Colombia para la comparación
        var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

        var citas = await _context.Citas
   .Where(c => c.BarberoId == barberoId)
       .ToListAsync(); // Traer todas y filtrar en memoria

        // 🔥 FILTRAR EN MEMORIA usando zona horaria correcta
    var citasFiltradas = citas
.Where(c => 
            {
  var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc), zonaColombia);
       return fechaLocal.Date == fecha.Date;
            })
   .OrderBy(c => c.Fecha)
      .ToList();

        foreach (var cita in citasFiltradas)
        {
            ConvertirCitaAFormatoLocal(cita);
          await EnriquecerCitaConServicio(cita);
        }

        return Ok(citasFiltradas);
    }

    [HttpGet("barbero/{barberoId}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarbero(long barberoId)
    {
        try
        {
            var citas = await _context.Citas
                .Where(c => c.BarberoId == barberoId)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var barbero = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(b => b.Cedula == barberoId);

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barbero?.Nombre ?? "No encontrado";
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas del barbero.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Cita>> CrearCita([FromBody] Cita nuevaCita)
    {
        if (nuevaCita == null || string.IsNullOrWhiteSpace(nuevaCita.Nombre))
        {
            return BadRequest("Datos inválidos.");
        }

        // 🔥 AGREGAR ESTO: Convertir la fecha a UTC antes de guardar
        nuevaCita.Fecha = DateTime.SpecifyKind(nuevaCita.Fecha, DateTimeKind.Utc);

        bool existeCita = await _context.Citas
            .AnyAsync(c => c.Fecha == nuevaCita.Fecha && c.BarberoId == nuevaCita.BarberoId);

        if (existeCita)
        {
            return Conflict("Ya existe una cita en esta fecha y hora.");
        }

        try
        {
            // ✅ ENRIQUECER LA CITA CON DATOS DEL SERVICIO (SI NO VIENEN DEL CLIENTE)
            if (nuevaCita.ServicioId.HasValue && nuevaCita.ServicioId > 0)
            {
                var servicio = await _context.Servicios.FindAsync(nuevaCita.ServicioId.Value);
                if (servicio != null)
                {
                    // ✅ Si no vienen del cliente, obtener del servicio
                    if (string.IsNullOrEmpty(nuevaCita.ServicioNombre))
                        nuevaCita.ServicioNombre = servicio.Nombre;
                    if (nuevaCita.ServicioPrecio == null || nuevaCita.ServicioPrecio == 0)
                        nuevaCita.ServicioPrecio = servicio.Precio;
                    if (string.IsNullOrEmpty(nuevaCita.ServicioImagen))
                        nuevaCita.ServicioImagen = servicio.Imagen;
                }
            }

            _context.Citas.Add(nuevaCita);
            await _context.SaveChangesAsync();

            Console.WriteLine($"\n📢 === NUEVA CITA CREADA ===");
          Console.WriteLine($"📢 Cita ID: {nuevaCita.Id}");
   Console.WriteLine($"📢 Cliente: {nuevaCita.Nombre} ({nuevaCita.Cedula})");
 Console.WriteLine($"📢 Barbero: {nuevaCita.BarberoId}");

    // 🔥 NOTIFICACIÓN AL BARBERO
        var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
      var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(nuevaCita.Fecha, zonaColombia);

  var data = new Dictionary<string, string>
    {
           { "tipo", "nueva_cita" },
     { "citaId", nuevaCita.Id.ToString() },
     { "clienteNombre", nuevaCita.Nombre ?? "" }
      };

            Console.WriteLine($"📤 Enviando notificación al barbero {nuevaCita.BarberoId}");
   try
    {
  bool enviado = await _notificationService.EnviarNotificacionAsync(
    nuevaCita.BarberoId,
   "Nueva Cita Pendiente",
  $"{nuevaCita.Nombre} ha solicitado una cita para el {fechaLocal:dd/MM/yyyy - hh:mm tt}",
  data
    );

   if (enviado)
  {
      Console.WriteLine($"✅ Notificación al barbero enviada exitosamente");
   }
           else
 {
         Console.WriteLine($"⚠️ No se pudo enviar notificación al barbero (posiblemente sin tokens registrados)");
       }
     }
   catch (Exception exNotif)
     {
 Console.WriteLine($"❌ Error enviando notificación: {exNotif.Message}");
 }

         Console.WriteLine($"📢 === FIN NUEVA CITA ===\n");

 ConvertirCitaAFormatoLocal(nuevaCita);
  return StatusCode(201, nuevaCita);
    }
        catch (Exception ex)
     {
    Console.WriteLine($"❌ ERROR EN CrearCita: {ex.Message}");
          Console.WriteLine($"❌ Stack: {ex.StackTrace}");
      return StatusCode(500, new { message = "Error al guardar la cita.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarCita(int id)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
            return NotFound();

        _context.Citas.Remove(cita);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] EstadoRequest req)
    {
       var cita = await _context.Citas.FirstOrDefaultAsync(c => c.Id == id);
      if (cita == null) return NotFound();

     var barbero = await _context.UsuarioPerfiles
      .FirstOrDefaultAsync(u => u.Cedula == cita.BarberoId);

    var servicio = await _context.Servicios
     .FirstOrDefaultAsync(s => s.Id == cita.ServicioId);

        string barberoNombre = barbero?.Nombre ?? "el barbero";
     string servicioNombre = servicio?.Nombre ?? "tu servicio";

        var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc),
            zonaColombia);

       var estadoAnterior = cita.Estado;

     cita.Estado = req.Estado;
        await _context.SaveChangesAsync();

    Console.WriteLine($"\n📢 === CAMBIO DE ESTADO DE CITA ===");
      Console.WriteLine($"📢 Cita ID: {cita.Id}");
            Console.WriteLine($"📢 Cliente: {cita.Nombre} ({cita.Cedula})");
   Console.WriteLine($"📢 Estado anterior: {estadoAnterior}");
   Console.WriteLine($"📢 Estado nuevo: {req.Estado}");

// 🔔 NOTIFICACIONES AL CLIENTE
   try
   {
   if (estadoAnterior == "ReagendarPendiente" && req.Estado == "Confirmada")
          {
        Console.WriteLine($"📤 Enviando: Reagendamiento aceptado");
  await _notificationService.EnviarNotificacionAsync(
    cita.Cedula,
 "Reagendamiento aceptado",
         $"Tu nuevo horario con {barberoNombre} fue aprobado para el {fechaLocal:dd/MM/yyyy - hh:mm tt}",
   new Dictionary<string, string>
       {
      { "tipo", "reagendamiento_aceptado" },
      { "citaId", cita.Id.ToString() }
         }
     );
          Console.WriteLine($"✅ Notificación enviada");
     }
          else if (estadoAnterior == "ReagendarPendiente" && req.Estado == "Cancelada")
        {
     Console.WriteLine($"📤 Enviando: Reagendamiento rechazado");
  await _notificationService.EnviarNotificacionAsync(
       cita.Cedula,
     "Reagendamiento rechazado",
        $"El barbero no pudo aceptar el nuevo horario solicitado.",
 new Dictionary<string, string>
{
            { "tipo", "reagendamiento_rechazado" },
      { "citaId", cita.Id.ToString() }
     }
 );
          Console.WriteLine($"✅ Notificación enviada");
        }
 else if (req.Estado == "Confirmada" || req.Estado == "Completada")
   {
        Console.WriteLine($"📤 Enviando: Cita confirmada");
 await _notificationService.EnviarNotificacionAsync(
     cita.Cedula,
    "Cita confirmada",
    $"Tu cita con {barberoNombre} para {servicioNombre} el {fechaLocal:dd/MM/yyyy - hh:mm tt} fue confirmada",
     new Dictionary<string, string>
  {
   { "tipo", "cita_confirmada" }
    }
             );
      Console.WriteLine($"✅ Notificación enviada");
    }
    else if (req.Estado == "Cancelada")
     {
    Console.WriteLine($"📤 Enviando: Cita rechazada");
 await _notificationService.EnviarNotificacionAsync(
       cita.Cedula,
"Cita rechazada",
     $"Tu cita con {barberoNombre} para {servicioNombre} el {fechaLocal:dd/MM/yyyy - hh:mm tt} fue rechazada",
     new Dictionary<string, string>
    {
  { "tipo", "cita_rechazada" }
    }
         );
     Console.WriteLine($"✅ Notificación enviada");
       }
  }
    catch (Exception ex)
{
      Console.WriteLine($"❌ Error enviando notificación: {ex.Message}");
         }

            Console.WriteLine($"📢 === FIN CAMBIO DE ESTADO ===\n");
   return Ok();
    }

    // ✅ NUEVO MÉTODO: ACTUALIZAR CITA COMPLETA
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarCita(int id, [FromBody] Cita citaActualizada)
    {
        var cita = await _context.Citas.FirstOrDefaultAsync(c => c.Id == id);
        if (cita == null) return NotFound();

        try
        {
bool existeCita = await _context.Citas.AnyAsync(c =>
    c.Fecha == citaActualizada.Fecha &&
   c.BarberoId == citaActualizada.BarberoId &&
       c.Id != id);

       if (existeCita)
 return Conflict("Ya existe una cita en esta fecha y hora con ese barbero.");

     var estadoAnterior = cita.Estado;

       cita.Fecha = DateTime.SpecifyKind(citaActualizada.Fecha, DateTimeKind.Utc);
     cita.BarberoId = citaActualizada.BarberoId;
    cita.ServicioId = citaActualizada.ServicioId;

     await EnriquecerCitaConServicio(cita);

      _context.Citas.Update(cita);
 await _context.SaveChangesAsync();

 var barbero = await _context.UsuarioPerfiles
   .FirstOrDefaultAsync(b => b.Cedula == cita.BarberoId);

var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
     var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(cita.Fecha, zonaColombia);

      Console.WriteLine($"\n📢 === CITA ACTUALIZADA ===");
  Console.WriteLine($"📢 Cita ID: {cita.Id}");
  Console.WriteLine($"📢 Cliente: {cita.Nombre} ({cita.Cedula})");
      Console.WriteLine($"📢 Barbero: {cita.BarberoId}");
      Console.WriteLine($"📢 Nueva fecha: {fechaLocal:dd/MM/yyyy - hh:mm tt}");

    // 🔔 Notificación SOLO al barbero
try
  {
    if (estadoAnterior == "ReagendarPendiente")
       {
       Console.WriteLine($"📤 Enviando: Solicitud de reagendamiento");
     await _notificationService.EnviarNotificacionAsync(
  cita.BarberoId,
     "Solicitud de reagendamiento",
         $"{cita.Nombre} propuso un nuevo horario para el {fechaLocal:dd/MM/yyyy - hh:mm tt}",
        new Dictionary<string, string>
      {
        { "tipo", "reagendamiento_solicitado" },
      { "citaId", cita.Id.ToString() }
     }
     );
    Console.WriteLine($"✅ Notificación enviada");
        }
        else
 {
         Console.WriteLine($"📤 Enviando: Cita modificada");
      await _notificationService.EnviarNotificacionAsync(
      cita.BarberoId,
    "Cita modificada",
    $"{cita.Nombre} modificó su cita para el {fechaLocal:dd/MM/yyyy - hh:mm tt}",
      new Dictionary<string, string>
    {
    { "tipo", "cita_modificada" },
  { "citaId", cita.Id.ToString() }
 }
   );
Console.WriteLine($"✅ Notificación enviada");
   }
  }
 catch (Exception exNotif)
   {
      Console.WriteLine($"❌ Error enviando notificación: {exNotif.Message}");
  }

 Console.WriteLine($"📢 === FIN ACTUALIZACIÓN CITA ===\n");

   ConvertirCitaAFormatoLocal(cita);
    return Ok(cita);
}
    catch (Exception ex)
 {
Console.WriteLine($"❌ ERROR EN ActualizarCita: {ex.Message}");
     Console.WriteLine($"❌ Stack: {ex.StackTrace}");
       return StatusCode(500, new { message = "Error al actualizar la cita.", error = ex.Message });
    }
   }

    public class EstadoRequest { public string Estado { get; set; } = string.Empty; }
}

public class EstadoUpdateDto
{
    public string Estado { get; set; } = string.Empty;
}
