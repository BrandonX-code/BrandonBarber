using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gasolutions.Maui.App.Services
{
    public class ReservationService
    {
        private static List<Cita> _reservations = new List<Cita>();

        public static void AddReservation(Cita cita)
        {
            _reservations.RemoveAll(c => c.Id == cita.Id);
            _reservations.Add(cita);
        }

        public static List<Cita> GetReservations()
        {
            return _reservations.OrderByDescending(c => c.Fecha).ToList();
        }

        public static bool ExistsReservation(int id)
        {
            return _reservations.Any(c => c.Id == id);
        }
    }

    public class Cita
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public DateTime Fecha { get; set; }
    }
}
