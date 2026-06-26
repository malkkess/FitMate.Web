namespace Presistence
{
    public static class FoodHelper
    {
        public static int MapCategoryToId(string category)
        {
            return category?.ToLower() switch
            {
                "v" => 1,
                "f" => 2,
                "p" => 3,
                "c" => 4,
                "d" => 5,
                "s" => 6,
                "fat" => 7,
                _ => 1
            };
        }

        public static string MapFoodFamily(string name, string category)
        {
            var normalized = name.ToLowerInvariant();
            var cat = category.ToUpperInvariant();

            if (cat is "V" or "F" or "C" or "D" or "S" or "FAT")
            {
                return cat switch
                {
                    "V" => "vegetable",
                    "F" => "fruit",
                    "C" => "carb",
                    "D" => "dairy",
                    "S" => "seed",
                    "FAT" => "fat_oil",
                    _ => "other",
                };
            }

            if (IsOffal(normalized))
                return "offal";

            if (normalized.Contains("crab") || normalized.Contains("lobster") ||
                normalized.Contains("shrimp") || normalized.Contains("shellfish"))
                return "seafood";

            if (normalized.Contains("fish") || normalized.Contains("tuna") ||
                normalized.Contains("salmon") || normalized.Contains("mackerel") ||
                normalized.Contains("tilapia"))
            {
                return IsFattyFish(normalized) ? "fish_fatty" : "fish_lean";
            }

            if (normalized.Contains("chicken") || normalized.Contains("turkey") ||
                normalized.Contains("duck") && !normalized.Contains("liver"))
                return "poultry";

            if (normalized.Contains("beef") || normalized.Contains("lamb"))
            {
                return IsFattyRedMeat(normalized) ? "red_meat_fatty" : "red_meat_lean";
            }

            return "other_protein";
        }

        public static double ComputeNetCarbs(double carbs, double fiber)
        {
            return Math.Max(0, carbs - fiber);
        }

        private static bool IsOffal(string name) =>
            name.Contains("liver") || name.Contains("kidney") || name.Contains("offal");

        private static bool IsFattyFish(string name) =>
            name.Contains("mackerel") || name.Contains("salmon") || name.Contains("bluefin");

        private static bool IsFattyRedMeat(string name) =>
            name.Contains("rib") || name.Contains("shortrib") || name.Contains("shoulder");
    }
}
