using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorrespondenceApi.Migrations
{
    /// <inheritdoc />
    public partial class AutoRegistrationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationRecord_RegistrationYear_RegistrationNumber",
                table: "RegistrationRecord");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "RegistrationRecord",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "RegistrationRecord",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumberPattern",
                table: "Organization",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "{type}-{seq:000}/{year}");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DocumentType",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRecord_OrganizationId_RegistrationYear_RegistrationNumber",
                table: "RegistrationRecord",
                columns: new[] { "OrganizationId", "RegistrationYear", "RegistrationNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationRecord_OrganizationId_RegistrationYear_RegistrationNumber",
                table: "RegistrationRecord");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "RegistrationRecord");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "RegistrationRecord");

            migrationBuilder.DropColumn(
                name: "RegistrationNumberPattern",
                table: "Organization");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRecord_RegistrationYear_RegistrationNumber",
                table: "RegistrationRecord",
                columns: new[] { "RegistrationYear", "RegistrationNumber" },
                unique: true);
        }
    }
}
