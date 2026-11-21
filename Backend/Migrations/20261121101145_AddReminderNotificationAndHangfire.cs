using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderNotificationAndHangfire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                table: "Reminders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HangfireJobId",
                table: "Reminders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailSent",
                table: "Reminders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RemindBeforeMinutes",
                table: "Reminders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WebhookUrl",
                table: "Reminders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "HangfireJobId",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "IsEmailSent",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "RemindBeforeMinutes",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "WebhookUrl",
                table: "Reminders");
        }
    }
}
