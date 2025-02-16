using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinaCare.Domain.Migrations
{
    /// <inheritdoc />
    public partial class fixpackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaccinePackageDetail_VaccinePackage_PackageId",
                table: "VaccinePackageDetail");

            migrationBuilder.AddForeignKey(
                name: "FK_VaccinePackageDetail_VaccinePackage_PackageId",
                table: "VaccinePackageDetail",
                column: "PackageId",
                principalTable: "VaccinePackage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaccinePackageDetail_VaccinePackage_PackageId",
                table: "VaccinePackageDetail");

            migrationBuilder.AddForeignKey(
                name: "FK_VaccinePackageDetail_VaccinePackage_PackageId",
                table: "VaccinePackageDetail",
                column: "PackageId",
                principalTable: "VaccinePackage",
                principalColumn: "Id");
        }
    }
}
