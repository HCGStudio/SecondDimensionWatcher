using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SecondDimensionWatcher.Migrations
{
    public partial class AddTrackTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrackTime",
                table: "AnimationInfo",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_AnimationInfo_Hash",
                table: "AnimationInfo",
                column: "Hash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnimationInfo_Hash",
                table: "AnimationInfo");

            migrationBuilder.DropColumn(
                name: "TrackTime",
                table: "AnimationInfo");
        }
    }
}
