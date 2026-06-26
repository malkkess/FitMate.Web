using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Contracts;
using DomainLayer.Models;
using Microsoft.EntityFrameworkCore;
using Presistence.Data;

namespace Presistence
{

    //use dbContext to seed data into the database , use the dbContext to add entities to the database and save changes.
    public class DataSeeding(FitMateDbContext dbContext) : IDataSeeding
    {
        public async Task SeedDataAsync()
        {
            try
            {
                if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
                {
                     await dbContext.Database.MigrateAsync();
                }
                if (!dbContext.FoodExchanges.Any())
                {
                    var exchanges = new List<FoodExchange>
                {
                    new FoodExchange {  Name = "Vegetables" },
                    new FoodExchange {  Name = "Fruits" },
                    new FoodExchange {  Name = "Proteins" },
                    new FoodExchange {  Name = "Carbohydrates" },
                    new FoodExchange {  Name = "Dairy" },
                    new FoodExchange {  Name = "Seeds & Nuts" },
                    new FoodExchange {  Name = "Fats & Oils" }
                };
                    await dbContext.FoodExchanges.AddRangeAsync(exchanges);
                    await dbContext.SaveChangesAsync();
                }
                if (!dbContext.FoodItems.Any())
                {
                    string[] fileNames = {
                    "Cleaned_Vegetables.json",
                    "Cleaned_Fruits.json",
                    "Cleaned_Meats.json",
                    "Cleaned_Dairy__Eggs__3.json",
                    "Cleaned_Nuts & Seeds.json",
                    "Cleaned_Rice & pasta.json",
                    "Cleaned_Fats & Oils.json"

                };



                    var basePath = @"..\Infrastructure\Presistence\Data\DataSeedData\";

                    foreach (var fileName in fileNames)
                    {
                        var fullPath = Path.Combine(basePath, fileName);

                        if (File.Exists(fullPath))
                        {
                            var jsonData = File.OpenRead(fullPath);
                            var foodItemsList = await System.Text.Json.JsonSerializer.DeserializeAsync<List<FoodItem>>(jsonData);

                            if (foodItemsList != null)
                            {
                                foreach (var item in foodItemsList)
                                {
                                    item.Category = string.IsNullOrWhiteSpace(item.Category)
                                        ? "V"
                                        : item.Category.ToUpperInvariant();
                                    item.FoodFamily = FoodHelper.MapFoodFamily(item.Name, item.Category);
                                    item.NetCarbs = FoodHelper.ComputeNetCarbs(item.Carbs, item.Fiber);
                                    item.FoodExchangeId = FoodHelper.MapCategoryToId(item.Category);

                                    await dbContext.FoodItems.AddAsync(item);
                                }
                            }
                        }
                    }

                    await dbContext.SaveChangesAsync();

                }

                await BackfillFoodMetadataAsync();
            }
            catch (Exception ex)
            {

            }
        }

        private async Task BackfillFoodMetadataAsync()
        {
            var itemsNeedingUpdate = dbContext.FoodItems
                .Where(f => string.IsNullOrEmpty(f.Category) || string.IsNullOrEmpty(f.FoodFamily))
                .ToList();

            if (itemsNeedingUpdate.Count == 0)
                return;

            foreach (var item in itemsNeedingUpdate)
            {
                if (string.IsNullOrEmpty(item.Category))
                    item.Category = MapExchangeIdToCategory(item.FoodExchangeId);

                item.FoodFamily = FoodHelper.MapFoodFamily(item.Name, item.Category);
                item.NetCarbs = FoodHelper.ComputeNetCarbs(item.Carbs, item.Fiber);
            }

            await dbContext.SaveChangesAsync();
        }

        private static string MapExchangeIdToCategory(int exchangeId) => exchangeId switch
        {
            1 => "V",
            2 => "F",
            3 => "P",
            4 => "C",
            5 => "D",
            6 => "S",
            7 => "FAT",
            _ => "V",
        };
    }
}
