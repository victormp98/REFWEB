using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCorteCajaUsuarioCierre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CortesCaja_AspNetUsers_UsuarioCierreId",
                table: "CortesCaja");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioCierreId",
                table: "CortesCaja",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_CortesCaja_AspNetUsers_UsuarioCierreId",
                table: "CortesCaja",
                column: "UsuarioCierreId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CortesCaja_AspNetUsers_UsuarioCierreId",
                table: "CortesCaja");

            migrationBuilder.UpdateData(
                table: "CortesCaja",
                keyColumn: "UsuarioCierreId",
                keyValue: null,
                column: "UsuarioCierreId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioCierreId",
                table: "CortesCaja",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_CortesCaja_AspNetUsers_UsuarioCierreId",
                table: "CortesCaja",
                column: "UsuarioCierreId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
