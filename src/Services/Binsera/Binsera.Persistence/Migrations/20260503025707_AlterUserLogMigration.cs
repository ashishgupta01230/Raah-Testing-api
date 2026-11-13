using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Binsera.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlterUserLogMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ClientIp",
                table: "UserLog",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserLog",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "KeepMeSignedIn",
                table: "UserLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviousRefreshTokenHash",
                table: "UserLog",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiresAt",
                table: "UserLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "UserLog",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenIssuedAt",
                table: "UserLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenRevokedAt",
                table: "UserLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "UserLog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "KeepMeSignedIn",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "PreviousRefreshTokenHash",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiresAt",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "RefreshTokenIssuedAt",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "RefreshTokenRevokedAt",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "UserLog");

            migrationBuilder.AlterColumn<string>(
                name: "ClientIp",
                table: "UserLog",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
