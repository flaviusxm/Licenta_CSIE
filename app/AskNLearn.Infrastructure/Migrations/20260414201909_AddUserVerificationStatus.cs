using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskNLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVerificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMemberships_StudyGroups_StudyGroupId",
                table: "GroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_GroupMemberships_StudyGroupId",
                table: "GroupMemberships");

            migrationBuilder.DropColumn(
                name: "StudyGroupId",
                table: "GroupMemberships");

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportedResourceId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedResourceId",
                table: "Reports",
                column: "ReportedResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_LearningResources_ReportedResourceId",
                table: "Reports",
                column: "ReportedResourceId",
                principalTable: "LearningResources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_LearningResources_ReportedResourceId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedResourceId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReportedResourceId",
                table: "Reports");

            migrationBuilder.AddColumn<Guid>(
                name: "StudyGroupId",
                table: "GroupMemberships",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMemberships_StudyGroupId",
                table: "GroupMemberships",
                column: "StudyGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMemberships_StudyGroups_StudyGroupId",
                table: "GroupMemberships",
                column: "StudyGroupId",
                principalTable: "StudyGroups",
                principalColumn: "Id");
        }
    }
}
