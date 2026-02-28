using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVentaNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioCancelaId",
                table: "Ventas");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioCancelaId",
                table: "Ventas",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Notas",
                table: "Ventas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "MotivoCancelacion",
                table: "Ventas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioCancelaId",
                table: "Ventas",
                column: "UsuarioCancelaId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioCancelaId",
                table: "Ventas");

            migrationBuilder.UpdateData(
                table: "Ventas",
                keyColumn: "UsuarioCancelaId",
                keyValue: null,
                column: "UsuarioCancelaId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioCancelaId",
                table: "Ventas",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Ventas",
                keyColumn: "Notas",
                keyValue: null,
                column: "Notas",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Notas",
                table: "Ventas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Ventas",
                keyColumn: "MotivoCancelacion",
                keyValue: null,
                column: "MotivoCancelacion",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "MotivoCancelacion",
                table: "Ventas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_UsuarioCancelaId",
                table: "Ventas",
                column: "UsuarioCancelaId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
