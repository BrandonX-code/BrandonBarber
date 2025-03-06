using Barber.Maui.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace Barber.Maui.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cita> Citas { get; set; }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);
        //    modelBuilder.Entity<Cita>().HasIndex(x => x.Nombre).IsUnique();
        //}
    }
}
