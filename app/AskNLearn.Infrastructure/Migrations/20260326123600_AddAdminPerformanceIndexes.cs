using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskNLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_Status_SubmittedAt",
                table: "VerificationRequests",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status_CreatedAt",
                table: "Reports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ModerationStatus_CreatedAt",
                table: "Posts",
                columns: new[] { "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ModerationStatus_CreatedAt",
                table: "Messages",
                columns: new[] { "ModerationStatus", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_Status_SubmittedAt",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Status_CreatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ModerationStatus_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ModerationStatus_CreatedAt",
                table: "Messages");
        }
    }
}
