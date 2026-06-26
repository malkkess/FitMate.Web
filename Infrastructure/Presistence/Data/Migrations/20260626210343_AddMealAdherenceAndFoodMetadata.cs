using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Presistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMealAdherenceAndFoodMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MealIngredients",
                table: "MealIngredients");

            migrationBuilder.AddColumn<int>(
                name: "DiabetesStatus",
                table: "HealthProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE HealthProfiles SET DiabetesStatus = 2 WHERE HasDiabetes = 1");

            migrationBuilder.DropColumn(
                name: "HasDiabetes",
                table: "HealthProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MealIngredients",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "FoodItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FoodFamily",
                table: "FoodItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MealIngredients",
                table: "MealIngredients",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MealAdherenceLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MealPlanId = table.Column<int>(type: "int", nullable: false),
                    PlannedCalories = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    PlannedProtein = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    PlannedCarbs = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    PlannedFats = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    PlannedFiber = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    EatenCalories = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    EatenProtein = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    EatenCarbs = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    EatenFats = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    EatenFiber = table.Column<double>(type: "float(18)", precision: 18, scale: 2, nullable: false),
                    AdherenceScore = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealAdherenceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealAdherenceLogs_MealPlans_MealPlanId",
                        column: x => x.MealPlanId,
                        principalTable: "MealPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MealAdherenceLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MealAdherenceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MealAdherenceLogId = table.Column<int>(type: "int", nullable: false),
                    MealIngredientId = table.Column<int>(type: "int", nullable: false),
                    IsEaten = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealAdherenceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealAdherenceItems_MealAdherenceLogs_MealAdherenceLogId",
                        column: x => x.MealAdherenceLogId,
                        principalTable: "MealAdherenceLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealAdherenceItems_MealIngredients_MealIngredientId",
                        column: x => x.MealIngredientId,
                        principalTable: "MealIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealIngredients_MealId_FoodItemId",
                table: "MealIngredients",
                columns: new[] { "MealId", "FoodItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealAdherenceItems_MealAdherenceLogId_MealIngredientId",
                table: "MealAdherenceItems",
                columns: new[] { "MealAdherenceLogId", "MealIngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealAdherenceItems_MealIngredientId",
                table: "MealAdherenceItems",
                column: "MealIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_MealAdherenceLogs_MealPlanId",
                table: "MealAdherenceLogs",
                column: "MealPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_MealAdherenceLogs_UserId_LogDate",
                table: "MealAdherenceLogs",
                columns: new[] { "UserId", "LogDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealAdherenceItems");

            migrationBuilder.DropTable(
                name: "MealAdherenceLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MealIngredients",
                table: "MealIngredients");

            migrationBuilder.DropIndex(
                name: "IX_MealIngredients_MealId_FoodItemId",
                table: "MealIngredients");

            migrationBuilder.DropColumn(
                name: "DiabetesStatus",
                table: "HealthProfiles");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "FoodFamily",
                table: "FoodItems");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MealIngredients",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "HasDiabetes",
                table: "HealthProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MealIngredients",
                table: "MealIngredients",
                columns: new[] { "MealId", "FoodItemId" });
        }
    }
}
