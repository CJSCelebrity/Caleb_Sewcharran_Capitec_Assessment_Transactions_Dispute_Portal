using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CapitecDisputes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerScopeAndTransactionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "disputes",
                keyColumn: "Id",
                keyValue: new Guid("0199c0de-0001-7000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "disputes",
                keyColumn: "Id",
                keyValue: new Guid("0199c0de-0001-7000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "disputes",
                keyColumn: "Id",
                keyValue: new Guid("0199c0de-0001-7000-8000-000000000003"));

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "disputes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "dispute_status_changes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_status_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispute_status_changes_disputes_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "disputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MerchantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disputes_CustomerId",
                table: "disputes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_status_changes_DisputeId",
                table: "dispute_status_changes",
                column: "DisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_CustomerId",
                table: "transactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_Reference",
                table: "transactions",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispute_status_changes");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_disputes_CustomerId",
                table: "disputes");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "disputes");

            migrationBuilder.InsertData(
                table: "disputes",
                columns: new[] { "Id", "CreatedAt", "MerchantName", "Reason", "ResolvedAt", "Status", "TransactionAmount", "TransactionCurrency", "TransactionDate", "TransactionId" },
                values: new object[,]
                {
                    { new Guid("0199c0de-0001-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 13, 7, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Takealot", "Charged twice for a single order.", null, "Initiated", 1499.99m, "ZAR", new DateTimeOffset(new DateTime(2026, 10, 12, 9, 14, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("0199c0de-1001-7000-8000-000000000001") },
                    { new Guid("0199c0de-0001-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 10, 10, 6, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Uber Eats", "Order never arrived but the card was charged.", null, "UnderInvestigation", 320.00m, "ZAR", new DateTimeOffset(new DateTime(2026, 10, 9, 18, 2, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("0199c0de-1001-7000-8000-000000000002") },
                    { new Guid("0199c0de-0001-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 10, 2, 8, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Spotify", "Subscription billed after cancellation.", new DateTimeOffset(new DateTime(2026, 10, 7, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Resolved", 89.50m, "ZAR", new DateTimeOffset(new DateTime(2026, 10, 1, 11, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("0199c0de-1001-7000-8000-000000000003") }
                });
        }
    }
}
