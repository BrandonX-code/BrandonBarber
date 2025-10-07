namespace Barber.Maui.BrandonBarber.Models
{
    public class PlantillaDisponibilidadModel
    {
        public long BarberoId { get; set; }
        // Horarios por día de la semana
        // Key: "Lunes", "Martes", etc.
        // Value: Dictionary con horarios {"6:00 AM - 12:00 PM": true, ...}
        public Dictionary<string, Dictionary<string, bool>> HorariosPorDia { get; set; } = new();
    }
}