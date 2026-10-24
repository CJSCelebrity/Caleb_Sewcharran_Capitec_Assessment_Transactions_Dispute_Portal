using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CapitecDisputes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MerchantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "disputes",
                columns: new[] { "Id", "CreatedAt", "MerchantName", "Reason", "ResolvedAt", "Status", "TransactionAmount", "TransactionCurrency", "TransactionDate", "TransactionId" },
                values: new object[,]
                {
                    { new Guid("0199c0de-0001-7000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 10, 13, 7, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Takealot", "Charged twice for a single order.", null, "Initiated", 1499.99m, "ZAR", new DateTimeOffset(new DateTime(2026, 10, 12, 9, 14, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("0199c0de-1001-7000-8000-000000000001") },
                    { new Guid("0199c0de-0001-7000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 10, 10, 6, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Uber Eats", "Order never arrived but the card was charged.", null, "UnderInvestigation", 320.00m, "ZAR", new DateTimeOffset(new DateTime(2026, 10, 9, 18, 2, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("0199c0de-1001-7000-8000-000000000002") },
                    { new Guid("0199c0de-0001-7000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 10, 2, 8, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Spotify", "Subscription billed after cancellation.", new DateTimeOffset(new DateTime(2026, 10, 7, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Resolved", 89.50m, "ZAR", new DateTimeOffset(new DateTime(2026, 10, 1, 11, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("0199c0de-1001-7000-8000-000000000003") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_disputes_Status",
                table: "disputes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_TransactionId",
                table: "disputes",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disputes");
        }
    }
}
