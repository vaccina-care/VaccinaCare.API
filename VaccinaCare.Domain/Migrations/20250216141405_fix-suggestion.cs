using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinaCare.Domain.Migrations
{
    /// <inheritdoc />
    public partial class fixsuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaccineSuggestion_Child_ChildId",
                table: "VaccineSuggestion");

            migrationBuilder.AddForeignKey(
                name: "FK_VaccineSuggestion_Child_ChildId",
                table: "VaccineSuggestion",
                column: "ChildId",
                principalTable: "Child",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaccineSuggestion_Child_ChildId",
                table: "VaccineSuggestion");

            migrationBuilder.AddForeignKey(
                name: "FK_VaccineSuggestion_Child_ChildId",
                table: "VaccineSuggestion",
                column: "ChildId",
                principalTable: "Child",
                principalColumn: "Id");
        }
    }
}
