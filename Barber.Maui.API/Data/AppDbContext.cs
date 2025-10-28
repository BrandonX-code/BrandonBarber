using Barber.Maui.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace Barber.Maui.API.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Cita> Citas { get; set; }
        //public DbSet<Perfil> Perfiles { get; set; }
        public DbSet<Auth> UsuarioPerfiles { get; set; }
        public DbSet<SolicitudAdmin> SolicitudesAdmin { get; set; }
        public DbSet<Barberia> Barberias { get; set; }
        public DbSet<Disponibilidad> Disponibilidad { get; set; }
        public DbSet<ImagenGaleria> ImagenesGaleria { get; set; }
        public DbSet<ServicioModel> Servicios { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tu configuración existente...

            modelBuilder.Entity<Auth>(e =>
            {
                e.HasKey(e => e.Cedula);
            });

            // Configuración para PasswordReset
            modelBuilder.Entity<PasswordReset>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(10);
                entity.Property(e => e.ExpiryDate).IsRequired();
                entity.Property(e => e.IsUsed).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
