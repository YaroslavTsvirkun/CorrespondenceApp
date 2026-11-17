using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorrespondenceApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Department_Name",
                table: "Department");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Employee",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Document",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Department",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentDepartmentId",
                table: "Department",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Correspondent",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "AppUser",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ParentOrganizationId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organization_Organization_ParentOrganizationId",
                        column: x => x.ParentOrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employee_OrganizationId",
                table: "Employee",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_OrganizationId",
                table: "Document",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_OrganizationId_Name",
                table: "Department",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Department_ParentDepartmentId",
                table: "Department",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Correspondent_OrganizationId",
                table: "Correspondent",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_OrganizationId",
                table: "AppUser",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Name",
                table: "Organization",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organization_ParentOrganizationId",
                table: "Organization",
                column: "ParentOrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUser_Organization_OrganizationId",
                table: "AppUser",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Correspondent_Organization_OrganizationId",
                table: "Correspondent",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Department_ParentDepartmentId",
                table: "Department",
                column: "ParentDepartmentId",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Organization_OrganizationId",
                table: "Department",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Document_Organization_OrganizationId",
                table: "Document",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employee_Organization_OrganizationId",
                table: "Employee",
                column: "OrganizationId",
                principalTable: "Organization",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUser_Organization_OrganizationId",
                table: "AppUser");

            migrationBuilder.DropForeignKey(
                name: "FK_Correspondent_Organization_OrganizationId",
                table: "Correspondent");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_Department_ParentDepartmentId",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_Organization_OrganizationId",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Document_Organization_OrganizationId",
                table: "Document");

            migrationBuilder.DropForeignKey(
                name: "FK_Employee_Organization_OrganizationId",
                table: "Employee");

            migrationBuilder.DropTable(
                name: "Organization");

            migrationBuilder.DropIndex(
                name: "IX_Employee_OrganizationId",
                table: "Employee");

            migrationBuilder.DropIndex(
                name: "IX_Document_OrganizationId",
                table: "Document");

            migrationBuilder.DropIndex(
                name: "IX_Department_OrganizationId_Name",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Department_ParentDepartmentId",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Correspondent_OrganizationId",
                table: "Correspondent");

            migrationBuilder.DropIndex(
                name: "IX_AppUser_OrganizationId",
                table: "AppUser");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Document");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "ParentDepartmentId",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Correspondent");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AppUser");

            migrationBuilder.CreateIndex(
                name: "IX_Department_Name",
                table: "Department",
                column: "Name",
                unique: true);
        }
    }
}
