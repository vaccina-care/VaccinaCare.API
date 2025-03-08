using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinaCare.Domain.Migrations
{
    /// <inheritdoc />
    public partial class updatepayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Payment_PaymentId",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_Payment_PaymentMethod_PaymentMethodId",
                table: "Payment");

            migrationBuilder.DropTable(
                name: "PaymentMethod");

            migrationBuilder.DropIndex(
                name: "IX_Payment_PaymentMethodId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Payment");

            migrationBuilder.RenameColumn(
                name: "PaymentType",
                table: "Payment",
                newName: "PaymentMethod");

            migrationBuilder.AddColumn<string>(
                name: "OrderDescription",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrderId",
                table: "Payment",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "Payment",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VnpayPaymentId",
                table: "Payment",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Payment_PaymentId",
                table: "Invoice",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Payment_PaymentId",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "OrderDescription",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "VnpayPaymentId",
                table: "Payment");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "Payment",
                newName: "PaymentType");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Payment",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Payment",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentMethodId",
                table: "Payment",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Payment",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentMethod",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MethodName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payment_PaymentMethodId",
                table: "Payment",
                column: "PaymentMethodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Payment_PaymentId",
                table: "Invoice",
                column: "PaymentId",
                principalTable: "Payment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_PaymentMethod_PaymentMethodId",
                table: "Payment",
                column: "PaymentMethodId",
                principalTable: "PaymentMethod",
                principalColumn: "Id");
        }
    }
}
