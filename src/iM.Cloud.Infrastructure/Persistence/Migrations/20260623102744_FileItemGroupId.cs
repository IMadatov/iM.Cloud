using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iM.Cloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FileItemGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "FileItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_GroupId",
                table: "FileItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_GroupId_ParentId_Name",
                table: "FileItems",
                columns: new[] { "GroupId", "ParentId", "Name" },
                unique: true,
                filter: "Active = 1 AND GroupId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems",
                columns: new[] { "OwnerId", "ParentId", "Name" },
                unique: true,
                filter: "Active = 1 AND GroupId IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FileItems_Groups_GroupId",
                table: "FileItems",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileItems_Groups_GroupId",
                table: "FileItems");

            migrationBuilder.DropIndex(
                name: "IX_FileItems_GroupId",
                table: "FileItems");

            migrationBuilder.DropIndex(
                name: "IX_FileItems_GroupId_ParentId_Name",
                table: "FileItems");

            migrationBuilder.DropIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "FileItems");

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems",
                columns: new[] { "OwnerId", "ParentId", "Name" },
                unique: true,
                filter: "Active = 1");
        }
    }
}
