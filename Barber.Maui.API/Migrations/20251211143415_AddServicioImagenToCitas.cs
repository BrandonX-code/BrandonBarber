using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barber.Maui.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServicioImagenToCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServicioImagen",
                table: "Citas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServicioImagen",
                table: "Citas");
        }
    }
}
