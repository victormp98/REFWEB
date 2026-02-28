using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarDireccionEnvio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_AspNetUsers_UserId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Direcciones_DireccionEnvioId",
                table: "Pedidos");

            migrationBuilder.RenameColumn(
                name: "DireccionEnvioId",
                table: "Pedidos",
                newName: "DireccionEntregaId");

            migrationBuilder.RenameIndex(
                name: "IX_Pedidos_DireccionEnvioId",
                table: "Pedidos",
                newName: "IX_Pedidos_DireccionEntregaId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Clientes",
                newName: "UsuarioId");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_UserId",
                table: "Clientes",
                newName: "IX_Clientes_UsuarioId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_AspNetUsers_UsuarioId",
                table: "Clientes",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Direcciones_DireccionEntregaId",
                table: "Pedidos",
                column: "DireccionEntregaId",
                principalTable: "Direcciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_AspNetUsers_UsuarioId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Direcciones_DireccionEntregaId",
                table: "Pedidos");

            migrationBuilder.RenameColumn(
                name: "DireccionEntregaId",
                table: "Pedidos",
                newName: "DireccionEnvioId");

            migrationBuilder.RenameIndex(
                name: "IX_Pedidos_DireccionEntregaId",
                table: "Pedidos",
                newName: "IX_Pedidos_DireccionEnvioId");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "Clientes",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_UsuarioId",
                table: "Clientes",
                newName: "IX_Clientes_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_AspNetUsers_UserId",
                table: "Clientes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Direcciones_DireccionEnvioId",
                table: "Pedidos",
                column: "DireccionEnvioId",
                principalTable: "Direcciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
