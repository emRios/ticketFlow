using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columna Version a Tickets para control optimista
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Agregar columna IsActive a Users para filtrar agentes activos
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Agregar columna LastAssignedAt a Users para desempates
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAssignedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            // Índice para query de ranking de agentes
            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_IsActive_LastAssignedAt",
                table: "Users",
                columns: new[] { "Role", "IsActive", "LastAssignedAt" });

            // Índice para query de carga por agente
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_AssignedTo",
                table: "Tickets",
                columns: new[] { "Status", "AssignedTo" });

            // Índice para control optimista
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Id_Version",
                table: "Tickets",
                columns: new[] { "Id", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Role_IsActive_LastAssignedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Status_AssignedTo",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Id_Version",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastAssignedAt",
                table: "Users");
        }
    }
}
