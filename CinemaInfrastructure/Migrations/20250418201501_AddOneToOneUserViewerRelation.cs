using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOneToOneUserViewerRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Viewers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Viewers_UserId",
                table: "Viewers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewers_AspNetUsers_UserId",
                table: "Viewers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Viewers_AspNetUsers_UserId",
                table: "Viewers");

            migrationBuilder.DropIndex(
                name: "IX_Viewers_UserId",
                table: "Viewers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Viewers");
        }
    }
}
