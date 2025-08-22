using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prelevements_par_caisse.Migrations
{
    /// <inheritdoc />
    public partial class AddedDescriptionToDemandeItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DemandeItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "DemandeItems");
        }
    }
}
