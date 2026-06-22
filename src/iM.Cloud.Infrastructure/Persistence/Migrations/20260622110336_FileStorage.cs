using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iM.Cloud.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorageObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StorageKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Sha256 = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageObjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StorageObjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileItems_FileItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FileItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileItems_StorageObjects_StorageObjectId",
                        column: x => x.StorageObjectId,
                        principalTable: "StorageObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_OwnerId_ParentId_Name",
                table: "FileItems",
                columns: new[] { "OwnerId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_ParentId",
                table: "FileItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileItems_StorageObjectId",
                table: "FileItems",
                column: "StorageObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageObjects_StorageKey",
                table: "StorageObjects",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileItems");

            migrationBuilder.DropTable(
                name: "StorageObjects");
        }
    }
}
