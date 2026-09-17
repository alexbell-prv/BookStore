using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Migrations.BookStore
{
    /// <inheritdoc />
    public partial class SeedDummyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Authors",
                columns: new[] { "AuthorId", "Name" },
                values: new object[,]
                {
                    { 1, "Ursula K. Le Guin" },
                    { 2, "Octavia E. Butler" },
                    { 3, "Gabriel García Márquez" },
                    { 4, "Mary Shelley" },
                    { 5, "James Baldwin" }
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "BookId", "AuthorId", "SubTitle", "Title" },
                values: new object[,]
                {
                    { 1, 1, null, "The Left Hand of Darkness" },
                    { 2, 1, null, "The Dispossessed" },
                    { 3, 2, null, "Kindred" },
                    { 4, 2, null, "Parable of the Sower" },
                    { 5, 3, null, "One Hundred Years of Solitude" },
                    { 6, 3, null, "Love in the Time of Cholera" },
                    { 7, 4, null, "Frankenstein" },
                    { 8, 5, null, "Go Tell It on the Mountain" },
                    { 9, 5, null, "The Fire Next Time" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "BookId",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            migrationBuilder.DeleteData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValues: new object[] { 1, 2, 3, 4, 5 });
        }
    }
}
