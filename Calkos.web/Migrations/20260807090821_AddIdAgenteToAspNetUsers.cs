using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Calkos.web.Migrations
{
    /// <inheritdoc />
    public partial class AddIdAgenteToAspNetUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdAgente",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdAgente",
                table: "AspNetUsers");
        }
    }
}
