using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SecondDimensionWatcher.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "AnimationInfo",
                table => new
                {
                    Id = table.Column<string>("text", nullable: false),
                    Description = table.Column<string>("text", nullable: true),
                    PublishTime = table.Column<DateTimeOffset>("timestamp with time zone", nullable: false),
                    TorrentUrl = table.Column<string>("text", nullable: true),
                    TorrentData = table.Column<byte[]>("bytea", nullable: true),
                    Hash = table.Column<string>("text", nullable: true),
                    IsTracked = table.Column<bool>("boolean", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_AnimationInfo", x => x.Id); });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "AnimationInfo");
        }
    }
}