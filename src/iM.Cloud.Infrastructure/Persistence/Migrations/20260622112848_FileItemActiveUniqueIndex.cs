using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iM.Cloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FileItemActiveUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems");

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems",
                columns: new[] { "OwnerId", "ParentId", "Name" },
                unique: true,
                filter: "Active = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems");

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems",
                columns: new[] { "OwnerId", "ParentId", "Name" },
                unique: true);
        }
    }
}
