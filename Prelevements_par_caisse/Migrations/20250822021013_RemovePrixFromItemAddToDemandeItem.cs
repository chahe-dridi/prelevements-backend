using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prelevements_par_caisse.Migrations
{
    /// <inheritdoc />
    public partial class RemovePrixFromItemAddToDemandeItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrixUnitaire",
                table: "Items");

            migrationBuilder.AddColumn<bool>(
                name: "Is_Faveur",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrixUnitaire",
                table: "DemandeItems",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Is_Faveur",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrixUnitaire",
                table: "DemandeItems");

            migrationBuilder.AddColumn<decimal>(
                name: "PrixUnitaire",
                table: "Items",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
