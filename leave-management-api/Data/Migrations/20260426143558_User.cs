using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace leave_management_api.Data.Migrations
{
    /// <inheritdoc />
    public partial class User : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId1",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagerId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagerId1",
                table: "Users");

            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                ALTER COLUMN "ManagerId" TYPE uuid
                USING NULL::uuid;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Users"
                ALTER COLUMN "DepartmentId" TYPE uuid
                USING NULL::uuid;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId",
                table: "Users",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagerId",
                table: "Users");

            migrationBuilder.AlterColumn<long>(
                name: "ManagerId",
                table: "Users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DepartmentId",
                table: "Users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManagerId1",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId1",
                table: "Users",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerId1",
                table: "Users",
                column: "ManagerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId1",
                table: "Users",
                column: "DepartmentId1",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerId1",
                table: "Users",
                column: "ManagerId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
