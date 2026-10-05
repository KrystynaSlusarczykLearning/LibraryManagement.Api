using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LibraryManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class RolesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "132d1719-e810-4f6d-9fc1-72781b08b0d7", "5647f8af-22fb-4e7b-ae18-c0fb035407e9", "Librarian", "LIBRARIAN" },
                    { "bf542e02-37f0-4fe8-807c-73eed1c09356", "01def283-aa36-4b88-b2ea-a053cab46da2", "Member", "MEMBER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "132d1719-e810-4f6d-9fc1-72781b08b0d7");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bf542e02-37f0-4fe8-807c-73eed1c09356");
        }
    }
}
