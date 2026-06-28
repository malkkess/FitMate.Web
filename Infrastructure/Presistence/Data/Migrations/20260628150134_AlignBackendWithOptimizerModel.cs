using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignBackendWithOptimizerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MealIngredients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FoodFamily",
                table: "MealIngredients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "NetCarbs",
                table: "MealIngredients",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SaturatedFats",
                table: "MealIngredients",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "SlotIndex",
                table: "MealIngredients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserOptimizerStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MasterSeed = table.Column<int>(type: "int", nullable: false),
                    PlanDayCounter = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOptimizerStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOptimizerStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOptimizerStates_UserId",
                table: "UserOptimizerStates",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOptimizerStates");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "MealIngredients");

            migrationBuilder.DropColumn(
                name: "FoodFamily",
                table: "MealIngredients");

            migrationBuilder.DropColumn(
                name: "NetCarbs",
                table: "MealIngredients");

            migrationBuilder.DropColumn(
                name: "SaturatedFats",
                table: "MealIngredients");

            migrationBuilder.DropColumn(
                name: "SlotIndex",
                table: "MealIngredients");
        }
    }
}
