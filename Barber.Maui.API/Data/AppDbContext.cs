using Barber.Maui.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace Barber.Maui.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cita> Citas { get; set; }
        public DbSet<Perfil> Perfiles { get; set; }
        public DbSet<Auth> UsuarioPerfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Auth>()
                .HasKey(a => a.Cedula);

        }
    }
}
