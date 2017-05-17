using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Forum3.Data.Migrations
{
    public partial class thing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Views",
                table: "Messages",
                newName: "ViewCount");

            migrationBuilder.RenameColumn(
                name: "Replies",
                table: "Messages",
                newName: "ReplyCount");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ViewCount",
                table: "Messages",
                newName: "Views");

            migrationBuilder.RenameColumn(
                name: "ReplyCount",
                table: "Messages",
                newName: "Replies");
        }
    }
}
