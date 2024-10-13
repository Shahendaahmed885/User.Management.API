using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace User.Management.API.Migrations
{
    /// <inheritdoc />
    public partial class uploadimages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a0bb3e10-6dde-4a9a-af95-3a98de1c338f");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d047ffb7-3f0a-48be-8f31-b3cb5e0dd946");

            migrationBuilder.CreateTable(
                name: "uploadImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uploadImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_uploadImages_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "320e0743-d59c-4e00-925f-12017629ef40", "1", "Admin", "Admin" },
                    { "653db0a2-a654-4e89-b674-2d0d5053873a", "2", "User", "User" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_uploadImages_UserProfileId",
                table: "uploadImages",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uploadImages");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "320e0743-d59c-4e00-925f-12017629ef40");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "653db0a2-a654-4e89-b674-2d0d5053873a");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a0bb3e10-6dde-4a9a-af95-3a98de1c338f", "2", "User", "User" },
                    { "d047ffb7-3f0a-48be-8f31-b3cb5e0dd946", "1", "Admin", "Admin" }
                });
        }
    }
}
