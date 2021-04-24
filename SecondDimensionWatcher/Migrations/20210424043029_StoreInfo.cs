using Microsoft.EntityFrameworkCore.Migrations;

namespace SecondDimensionWatcher.Migrations
{
    public partial class StoreInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                table: "AnimationInfo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StorePath",
                table: "AnimationInfo",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFinished",
                table: "AnimationInfo");

            migrationBuilder.DropColumn(
                name: "StorePath",
                table: "AnimationInfo");
        }
    }
}
