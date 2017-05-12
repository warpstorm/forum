using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Forum3.Data.Migrations
{
    public partial class Categories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Boards_ParentId",
                table: "Boards");

            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Messages_ParentId",
                table: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_Boards_ParentId",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Boards");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Boards",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DisplayOrder = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boards_CategoryId",
                table: "Boards",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_LastMessageId",
                table: "Boards",
                column: "LastMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Categories_CategoryId",
                table: "Boards",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Messages_LastMessageId",
                table: "Boards",
                column: "LastMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Categories_CategoryId",
                table: "Boards");

            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Messages_LastMessageId",
                table: "Boards");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Boards_CategoryId",
                table: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_Boards_LastMessageId",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Boards");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Boards",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ParentId",
                table: "Boards",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Boards_ParentId",
                table: "Boards",
                column: "ParentId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Messages_ParentId",
                table: "Boards",
                column: "ParentId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
