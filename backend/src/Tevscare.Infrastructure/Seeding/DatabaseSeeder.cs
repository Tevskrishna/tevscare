using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tevscare.Domain.Account;
using Tevscare.Domain.Catalog;
using Tevscare.Domain.Enums;
using Tevscare.Domain.Identity;
using Tevscare.Domain.Planning;
using Tevscare.Infrastructure.Identity;
using Tevscare.Infrastructure.Persistence;
using Tevscare.Infrastructure.Services;

namespace Tevscare.Infrastructure.Seeding;

public static class CatalogConstants
{
    public const string StarterPlanName = "TEVSCARE 15-day starter plan";
}

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roles,
        bool seedDemoUsers,
        CancellationToken cancellationToken = default)
    {
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedCatalogAsync(db, cancellationToken);
        if (seedDemoUsers)
        {
            await SeedDemoUsersAsync(db, users, cancellationToken);
        }
    }

    private static async Task SeedCatalogAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.DietPlans.AnyAsync(p => p.Name == CatalogConstants.StarterPlanName, cancellationToken))
        {
            return;
        }

        var categories = new Dictionary<string, FoodCategory>();
        FoodCategory Category(string slug, string name, GroceryCategory grocery, int order)
        {
            var category = new FoodCategory { Name = name, Slug = slug, GroceryCategory = grocery, SortOrder = order };
            categories[slug] = category;
            return category;
        }

        var categoryList = new[]
        {
            Category("nuts", "Nuts & Seeds", GroceryCategory.NutsAndSeeds, 1),
            Category("protein", "Protein", GroceryCategory.Protein, 2),
            Category("grains", "Grains", GroceryCategory.Grains, 3),
            Category("pulses", "Pulses", GroceryCategory.Pulses, 4),
            Category("dairy", "Dairy", GroceryCategory.Dairy, 5),
            Category("vegetables", "Vegetables", GroceryCategory.Vegetables, 6),
            Category("fruits", "Fruits", GroceryCategory.Fruits, 7),
            Category("pantry", "Pantry", GroceryCategory.Other, 8)
        };

        var foods = new Dictionary<string, Food>();
        Food Add(
            string key,
            string category,
            string name,
            string? local,
            string label,
            string unit,
            decimal kcal,
            decimal protein,
            decimal carbs,
            decimal fat,
            decimal fibre,
            string? allergens,
            string? tags,
            DietFlags flags,
            decimal? price,
            string? note = null)
        {
            var food = new Food
            {
                Category = categories[category],
                Name = name,
                LocalName = local,
                ServingLabel = label,
                ServingQuantity = 1,
                ServingUnit = unit,
                Calories = kcal,
                ProteinG = protein,
                CarbohydratesG = carbs,
                FatG = fat,
                FibreG = fibre,
                Allergens = allergens,
                Tags = tags,
                SuitableFor = flags,
                GroceryCategory = categories[category].GroceryCategory,
                ReferencePriceInr = price,
                ProviderNote = note,
                MicronutrientsNote = "Figures are approximate planning estimates, not a laboratory analysis."
            };
            foods[key] = food;
            return food;
        }

        const DietFlags all = DietFlags.All;
        const DietFlags vegPlus = DietFlags.VegetarianAndAbove;
        const DietFlags eggPlus = DietFlags.EggetarianAndAbove;
        const DietFlags meat = DietFlags.NonVegetarian;

        Add("almonds", "nuts", "Soaked peeled almonds", "బాదం", "About 15 g, soaked and peeled", "serving", 90, 3, 3, 8, 2, "tree nuts", "breakfast,nuts", all, 15);
        Add("walnuts", "nuts", "Walnuts", "అక్రోటు", "About 10 g", "serving", 65, 2, 1, 7, 1, "tree nuts", "breakfast,nuts", all, 12);
        Add("pumpkin-seeds", "nuts", "Pumpkin seeds", null, "Small handful", "serving", 80, 4, 2, 7, 1, null, "snack,seeds", all, 32);
        Add("chia", "nuts", "Chia seeds", null, "1 tablespoon", "serving", 60, 2, 5, 4, 4, null, "seeds", all, 20);
        Add("sesame", "nuts", "Sesame seeds", null, "1 tablespoon", "serving", 52, 2, 2, 4, 1, "sesame", "seeds", all, 8);
        Add("dry-coconut", "nuts", "Dry coconut", null, "1 tablespoon", "serving", 60, 1, 2, 6, 1, "tree nuts", "pantry", all, 6);
        Add("peanuts", "nuts", "Soaked peanuts (pallilu)", "పల్లీలు", "Small handful, soaked", "serving", 170, 8, 5, 14, 2, "peanuts", "breakfast,protein-choice", all, 12);
        Add("dates", "fruits", "Dates", null, "2 pieces", "serving", 130, 1, 32, 0, 2, null, "dry-fruit", all, 12);
        Add("anjeer", "fruits", "Anjeer (fig)", null, "2 pieces", "serving", 90, 1, 20, 0, 3, null, "dry-fruit", all, 15);
        Add("eggs", "protein", "Boiled eggs", "గుడ్లు", "2 boiled eggs", "serving", 140, 12, 1, 10, 0, "egg", "breakfast,protein-choice", eggPlus, 18);
        Add("sprouts", "pulses", "Sprouts (molakalu)", "మొలకలు", "1 small cup", "cup", 60, 5, 10, 1, 3, null, "breakfast,protein-choice", all, 15);
        Add("idly", "grains", "Idly", "ఇడ్లీ", "1 idly", "piece", 40, 2, 8, 0, 1, null, "breakfast", all, 10, "Ask your provider if the batter or cooking fat needs a substitution.");
        Add("dosa", "grains", "Dosa", "దోశ", "1 plain dosa", "piece", 120, 3, 18, 4, 1, null, "breakfast", all, 10, "Plain dosa, not a deep-fried variation.");
        Add("pesar-dosa", "grains", "Pesar dosa", "పెసర దోశ", "1 moong dal dosa", "piece", 110, 6, 16, 2, 2, null, "breakfast", all, 10);
        Add("ragi-java", "grains", "Ragi java", "రాగి జావ", "1 cup", "cup", 80, 2, 16, 1, 2, null, "breakfast", all, 12);
        Add("buttermilk", "dairy", "Buttermilk", "మజ్జిగ", "1 cup", "cup", 40, 3, 4, 1, 0, "milk", "dairy", vegPlus, 8, "Skip this if you avoid dairy. Water is the plan's substitution.");
        Add("curd", "dairy", "Curd", "పెరుగు", "1 small cup", "cup", 90, 5, 6, 4, 0, "milk", "dairy", vegPlus, 25);
        Add("roti-mg", "grains", "Multigrain roti", null, "2 rotis", "serving", 180, 6, 32, 3, 4, "gluten", "lunch,dinner", all, 25);
        Add("roti-wheat", "grains", "Wheat roti", null, "2 rotis", "serving", 170, 6, 30, 3, 3, "gluten", "lunch,dinner", all, 25);
        Add("oats", "grains", "Oats", null, "30 g dry", "serving", 110, 4, 19, 2, 3, "gluten", "pantry", all, 12);
        Add("wheat-rava", "grains", "Wheat rava", null, "40 g", "serving", 140, 4, 28, 1, 2, "gluten", "pantry", all, 8);
        Add("wheat-flour", "grains", "Wheat flour", null, "40 g", "serving", 140, 4, 28, 1, 2, "gluten", "pantry", all, 6);
        Add("multigrain-flour", "grains", "Multigrain flour", null, "40 g", "serving", 140, 5, 26, 2, 3, "gluten", "pantry", all, 10);
        Add("toor", "pulses", "Toor dal", null, "1 cooked serving", "serving", 120, 7, 18, 1, 4, null, "lunch", all, 30);
        Add("moong", "pulses", "Moong dal", null, "1 cooked serving", "serving", 110, 8, 16, 1, 4, null, "lunch,dinner", all, 30);
        Add("urad", "pulses", "Urad dal", null, "1 cooked serving", "serving", 130, 8, 18, 1, 5, null, "dinner", all, 30);
        Add("rajma", "pulses", "Rajma", null, "1 cooked serving", "serving", 140, 8, 22, 1, 6, null, "lunch", all, 35);
        Add("bengal-gram", "pulses", "Bengal gram", null, "1 cooked serving", "serving", 150, 8, 22, 3, 6, null, "lunch", all, 40);
        Add("meal-maker", "protein", "Meal maker", null, "1 cooked serving", "serving", 120, 12, 8, 4, 3, "soy", "lunch", all, 40);
        Add("leafy", "vegetables", "Green leafy vegetables", null, "1 cooked serving", "serving", 40, 3, 5, 1, 3, null, "vegetable", all, 35);
        Add("drumstick-leaves", "vegetables", "Drumstick leaves", null, "1 cooked serving", "serving", 40, 3, 5, 1, 3, null, "vegetable", all, 35);
        Add("mixed-veg", "vegetables", "Mixed vegetables", null, "1 cooked serving", "serving", 70, 3, 10, 2, 4, null, "vegetable", all, 40);
        Add("sweet-potato", "vegetables", "Sweet potato", null, "1 small", "serving", 90, 2, 20, 0, 3, null, "vegetable", all, 30);
        Add("salad", "vegetables", "Raw salad", null, "1 bowl before the meal", "bowl", 30, 1, 6, 0, 2, null, "salad", all, 30, "Plan guidance: have a raw salad before lunch and dinner.");
        Add("cucumber", "vegetables", "Cucumber", null, "1 serving", "serving", 15, 1, 3, 0, 1, null, "vegetable", all, 10);
        Add("carrot", "vegetables", "Carrot", null, "1 serving", "serving", 25, 1, 6, 0, 2, null, "vegetable", all, 10);
        Add("mint", "vegetables", "Mint", null, "1 small bunch", "serving", 8, 1, 1, 0, 1, null, "vegetable", all, 8);
        Add("lemon", "vegetables", "Lemon", null, "1 lemon", "piece", 10, 0, 3, 0, 1, null, "vegetable", all, 5);
        Add("banana", "fruits", "Banana", null, "1 small", "piece", 90, 1, 23, 0, 3, null, "fruit", all, 15);
        Add("jackfruit", "fruits", "Jackfruit", null, "A small portion", "serving", 90, 1, 22, 0, 2, null, "fruit", all, 30);
        Add("sapota", "fruits", "Sapota", null, "1 small", "piece", 80, 0, 20, 1, 3, null, "fruit", all, 25);
        Add("custard-apple", "fruits", "Custard apple", null, "1 small", "piece", 90, 1, 22, 0, 3, null, "fruit", all, 30);
        Add("mango", "fruits", "Mango", null, "A few slices", "serving", 90, 1, 22, 0, 2, null, "fruit", all, 30);
        Add("seasonal-fruit", "fruits", "Seasonal fruit", null, "1 serving", "serving", 80, 1, 18, 0, 3, null, "fruit", all, 25);
        Add("green-tea", "pantry", "Green tea", null, "1 cup", "cup", 2, 0, 0, 0, 0, null, "drink", all, 8);
        Add("ghee", "pantry", "Pure ghee", null, "1 teaspoon", "teaspoon", 45, 0, 0, 5, 0, "milk", "fat", vegPlus, 8, "Plan guidance: keep added fat small.");
        Add("cumin", "pantry", "Cumin", null, "1 teaspoon", "teaspoon", 8, 0, 1, 0, 1, null, "spice", all, 3);
        Add("fennel", "pantry", "Fennel seeds", null, "1 teaspoon", "teaspoon", 7, 0, 1, 0, 1, null, "spice", all, 4);
        Add("coriander", "pantry", "Coriander", null, "1 teaspoon", "teaspoon", 5, 0, 1, 0, 1, null, "spice", all, 3);
        Add("cinnamon", "pantry", "Cinnamon", null, "A small piece", "serving", 6, 0, 2, 0, 1, null, "spice", all, 5);
        Add("ginger", "pantry", "Ginger", null, "1 small piece", "serving", 8, 0, 2, 0, 0, null, "spice", all, 5);
        Add("cardamom", "pantry", "Cardamom", null, "2 pods", "serving", 6, 0, 1, 0, 1, null, "spice", all, 8);
        Add("pink-salt", "pantry", "Pink salt", null, "A pinch", "serving", 0, 0, 0, 0, 0, null, "seasoning", all, 1, "Plan guidance: keep salt modest.");
        Add("chicken", "protein", "Chicken", null, "1 cooked serving", "serving", 165, 25, 0, 7, 0, null, "non-veg", meat, 80);
        Add("fish", "protein", "Fish", null, "1 cooked serving", "serving", 140, 22, 0, 5, 0, "fish", "non-veg", meat, 90);
        Add("prawn", "protein", "Prawn", null, "1 cooked serving", "serving", 100, 20, 1, 1, 0, "crustacean", "non-veg", meat, 100);
        Add("mutton", "protein", "Mutton", null, "1 cooked serving", "serving", 200, 20, 0, 14, 0, null, "provider-reference", meat, null, "The source plan says not to include mutton. That is provider guidance, not a universal medical rule. It is not part of the starter plan.");
        Add("potato", "vegetables", "Potato", null, "1 serving", "serving", 130, 3, 28, 0, 2, null, "provider-reference", all, null, "The source plan says to avoid potato. That is provider guidance for this plan, not a universal medical rule.");
        Add("chamadumpa", "vegetables", "Chamadumpa (colocasia)", "చామదుంప", "1 serving", "serving", 110, 2, 26, 0, 3, null, "provider-reference", all, null, "The source plan says to avoid chamadumpa. That is provider guidance for this plan, not a universal medical rule.");
        Add("maida", "grains", "Maida", null, "40 g", "serving", 145, 4, 30, 1, 1, "gluten", "provider-reference", all, null, "The source material describes maida as something to avoid in this plan. TEVSCARE stores that as provider guidance, not as a medical fact.");
        Add("sugar", "pantry", "Sugar", null, "1 tablespoon", "serving", 48, 0, 12, 0, 0, null, "provider-reference", all, null, "The source material says to avoid added sugar in this plan. That is provider guidance, not a diagnosis.");

        var plan = new DietPlan
        {
            Name = CatalogConstants.StarterPlanName,
            Description = "A 15-day starter plan based on the diet-care breakfast rotation, with lunch, snacks, dinner, water, activity and sleep notes a provider can edit. It is planning content, not medical treatment.",
            DurationDays = 15,
            IsPublished = true,
            Disclaimer = ProductDisclaimer.Text
        };

        var breakfasts = new (string Key, decimal Qty, string Title)[]
        {
            ("idly", 2, "2 idly"),
            ("dosa", 2, "2 dosa"),
            ("ragi-java", 1, "Ragi java with buttermilk"),
            ("pesar-dosa", 2, "2 pesar dosa"),
            ("idly", 2, "2 idly"),
            ("dosa", 2, "2 dosa"),
            ("ragi-java", 1, "Ragi java with buttermilk"),
            ("idly", 2, "2 idly"),
            ("pesar-dosa", 2, "2 pesar dosa"),
            ("dosa", 2, "2 dosa"),
            ("ragi-java", 1, "Ragi java with buttermilk"),
            ("idly", 2, "2 idly"),
            ("pesar-dosa", 2, "2 pesar dosa"),
            ("dosa", 2, "2 dosa"),
            ("ragi-java", 1, "Ragi java with buttermilk")
        };

        var fruits = new[] { "banana", "seasonal-fruit", "sapota", "banana", "mango", "seasonal-fruit", "custard-apple", "banana", "jackfruit", "seasonal-fruit", "banana", "sapota", "seasonal-fruit", "banana", "seasonal-fruit" };
        var lunches = new[]
        {
            new[] { "roti-mg", "toor", "leafy", "salad" },
            new[] { "roti-wheat", "rajma", "sweet-potato", "salad" },
            new[] { "roti-mg", "moong", "drumstick-leaves", "salad" },
            new[] { "roti-wheat", "bengal-gram", "leafy", "salad" },
            new[] { "roti-mg", "meal-maker", "mixed-veg", "salad" }
        };
        var lunchTitles = new[] { "Roti, dal and greens", "Roti, rajma and sweet potato", "Roti, moong dal and drumstick leaves", "Roti and Bengal gram", "Roti, meal maker and vegetables" };
        var dinners = new[]
        {
            new[] { "roti-wheat", "moong", "mixed-veg", "salad", "curd" },
            new[] { "roti-mg", "urad", "leafy", "salad", "curd" },
            new[] { "roti-wheat", "moong", "sweet-potato", "salad", "curd" },
            new[] { "roti-mg", "urad", "mixed-veg", "salad", "curd" },
            new[] { "roti-wheat", "moong", "leafy", "salad", "curd" }
        };
        var dinnerTitles = new[] { "Lighter roti and moong dal", "Roti, urad dal and greens", "Roti, dal and sweet potato", "Roti, dal and vegetables", "Roti, dal and leafy vegetables" };

        for (var dayNumber = 1; dayNumber <= 15; dayNumber++)
        {
            var day = new DietPlanDay
            {
                DayNumber = dayNumber,
                Notes = "Plan guidance: eat slowly, keep added oil small, and leave a gap before sleep. This is the provider plan, not a medical instruction.",
                WaterNote = "Spread your personal water goal through the day. Hourly reminders are optional and can be changed in settings.",
                ActivityNote = dayNumber % 3 == 0
                    ? "20 minutes of comfortable walking plus gentle mobility."
                    : "30 minutes of comfortable walking at a pace that lets you talk.",
                SleepNote = "Aim for your sleep goal and start winding down before your sleep time."
            };

            var breakfast = breakfasts[dayNumber - 1];
            var breakfastItems = new List<(string Key, decimal Qty, string? Group, bool Default, string? Note)>
            {
                ("almonds", 1, null, false, "Soaked and peeled."),
                ("walnuts", 1, null, false, null),
                ("eggs", 1, "breakfast-protein", true, "Choose one protein option."),
                ("peanuts", 1, "breakfast-protein", true, "Soaked pallilu."),
                ("sprouts", 1, "breakfast-protein", false, "A small cup of molakalu."),
                (breakfast.Key, breakfast.Qty, null, false, breakfast.Key == "dosa" ? "Plain dosa." : null)
            };
            if (breakfast.Key == "ragi-java")
            {
                breakfastItems.Add(("buttermilk", 1, null, false, "With the ragi java. Use water if you avoid dairy."));
            }

            day.Meals.Add(MakeMeal(MealType.Breakfast, breakfast.Title, new TimeOnly(8, 0), "Breakfast base plus today's rotation. Protein is a choice: eggs, soaked peanuts, or sprouts.", breakfastItems, foods));
            day.Meals.Add(MakeMeal(MealType.MidMorning, "Fruit", new TimeOnly(10, 30), "Fruit from the plan list. Portion is a normal serving.", [("fruit", 1, null, false, null)], foods, fruits[dayNumber - 1]));
            day.Meals.Add(MakeMeal(MealType.Lunch, lunchTitles[(dayNumber - 1) % lunchTitles.Length], new TimeOnly(13, 0), "Have the raw salad before the meal. Keep oil and salt modest.", lunches[(dayNumber - 1) % lunches.Length].Select(k => (k, 1m, (string?)null, false, (string?)null)).ToList(), foods));
            day.Meals.Add(MakeMeal(MealType.AfternoonSnack, "Green tea and seeds", new TimeOnly(16, 30), "A light snack. Skip packaged sweets and fried snacks.", [("green-tea", 1m, null, false, null), ("pumpkin-seeds", 1m, null, false, null)], foods));
            day.Meals.Add(MakeMeal(MealType.Dinner, dinnerTitles[(dayNumber - 1) % dinnerTitles.Length], new TimeOnly(19, 30), "Keep dinner inside your eating window. Curd is optional if you use dairy.", dinners[(dayNumber - 1) % dinners.Length].Select(k => (k, 1m, (string?)null, false, k == "curd" ? (string?)"Optional dairy." : null)).ToList(), foods));
            plan.Days.Add(day);
        }

        plan.Guidance = Guidance(plan);
        db.AddRange(categoryList);
        db.AddRange(foods.Values);
        db.Add(plan);
        db.AddRange(Habits());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Meal MakeMeal(
        MealType type,
        string title,
        TimeOnly time,
        string notes,
        IReadOnlyList<(string Key, decimal Qty, string? Group, bool Default, string? Note)> items,
        IReadOnlyDictionary<string, Food> foods,
        string? overrideFirstKey = null)
    {
        var meal = new Meal { MealType = type, Title = title, ScheduledTime = time, Notes = notes };
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var key = i == 0 && overrideFirstKey is not null ? overrideFirstKey : item.Key;
            var food = foods[key];
            meal.Items.Add(new MealItem
            {
                Food = food,
                DisplayName = food.Name,
                Quantity = item.Qty,
                Unit = food.ServingUnit,
                SortOrder = i,
                AlternativeGroup = item.Group,
                IsDefaultAlternative = item.Default,
                Note = item.Note
            });
        }

        return meal;
    }

    private static List<ProviderGuidance> Guidance(DietPlan plan)
    {
        (GuidanceCategory Category, string Title, string Body, string? Condition, int Order)[] rows =
        [
            (GuidanceCategory.Hydration, "Personal water goal", "Spread water through the day. A planning suggestion is about 35 ml per kilogram of body weight, kept between 2 and 3.5 litres unless a clinician has set another goal. Your saved goal is what the tracker uses. Do not force a high volume.", null, 1),
            (GuidanceCategory.MealHabit, "Raw salad before meals", "The plan suggests a raw salad before lunch and dinner.", null, 2),
            (GuidanceCategory.MealHabit, "Oil", "Keep added oil small. The plan avoids deep-fried food.", null, 3),
            (GuidanceCategory.Restriction, "Packaged and fast food", "The plan suggests leaving out deep-fried, packaged and fast food.", null, 4),
            (GuidanceCategory.MealHabit, "Salt", "Keep salt modest. Pink salt is listed as a pantry item, not as a reason to add more salt.", null, 5),
            (GuidanceCategory.Restriction, "Sugar and maida", "The source material says to avoid added sugar, sweets and maida in this plan. TEVSCARE stores that as plan guidance, not as a medical claim.", null, 6),
            (GuidanceCategory.MealHabit, "Protein with main meals", "Breakfast includes a protein choice: two boiled eggs, soaked peanuts (pallilu), or a small cup of sprouts (molakalu). Lunch and dinner include a pulse, dairy or meal-maker serving.", null, 7),
            (GuidanceCategory.Sleep, "Sleep", "Keep a regular sleep window and start winding down before your sleep time.", null, 8),
            (GuidanceCategory.Activity, "Daily movement", "The plan includes a comfortable daily walk. Stop if you feel unwell and ask a clinician about the right activity for you.", null, 9),
            (GuidanceCategory.General, "Consistency", "Repeating the plan matters more than a perfect day. Log what you actually ate.", null, 10),
            (GuidanceCategory.Restriction, "Soft drinks", "The plan does not include soft drinks.", null, 11),
            (GuidanceCategory.MealHabit, "Fruit", "Fruit in this plan includes banana, jackfruit, sapota, custard apple, mango and other seasonal fruit, in ordinary portions.", null, 12),
            (GuidanceCategory.MealHabit, "Late meals", "The plan suggests finishing dinner inside your eating window rather than late at night.", null, 13),
            (GuidanceCategory.MealHabit, "Eat slowly", "Chew thoroughly and give the meal your attention.", null, 14),
            (GuidanceCategory.Restriction, "Potato and chamadumpa", "The source plan says to avoid potato and chamadumpa (colocasia). They stay in the food reference so a provider can change the rule. They are not in the starter meals.", null, 15),
            (GuidanceCategory.Restriction, "Mutton", "The source plan says not to include mutton. Chicken, fish, prawn and eggs remain available in the food reference for providers. Mutton is not in the starter meals.", null, 16),
            (GuidanceCategory.Restriction, "Thyroid or kidney-stone guidance", "Some provider notes restrict foods for people with thyroid conditions or kidney stones. TEVSCARE does not apply those limits automatically and does not diagnose either condition. Confirm any restriction with a qualified clinician before you follow it.", "thyroid-or-kidney-stones", 17),
            (GuidanceCategory.General, "Balanced plate", "Build the plate from vegetables, a grain or roti, a protein food and fruit across the day. The plate is a planning picture, not a prescription.", null, 18),
            (GuidanceCategory.General, "Pantry", "Cumin, fennel, coriander, cinnamon, ginger, cardamom, chia, sesame, dry coconut, oats, wheat rava, flours and a little ghee are in the food reference. Use spices lightly. Prices are yours to edit.", null, 19)
        ];

        return rows.Select(row => new ProviderGuidance
        {
            DietPlan = plan,
            Category = row.Category,
            Title = row.Title,
            Body = row.Body,
            Label = "Plan guidance",
            ConditionKey = row.Condition,
            SortOrder = row.Order
        }).ToList();
    }

    private static HabitDefinition[] Habits() =>
    [
        new() { Code = "water_goal", Title = "Water goal", Description = "Log water toward your personal goal.", SortOrder = 1 },
        new() { Code = "salad_before_meals", Title = "Salad before meals", Description = "Raw salad before lunch and dinner.", SortOrder = 2 },
        new() { Code = "protein_with_meals", Title = "Protein with meals", Description = "Include the planned protein at main meals.", SortOrder = 3 },
        new() { Code = "limit_oil", Title = "Limited oil", Description = "Keep added oil small and skip deep-fried food.", SortOrder = 4 },
        new() { Code = "skip_sugary_drinks", Title = "No soft drinks", Description = "Skip soft drinks and sugary drinks today.", SortOrder = 5 },
        new() { Code = "daily_movement", Title = "Daily walk", Description = "Complete today's planned movement.", SortOrder = 6 },
        new() { Code = "sleep_window", Title = "Sleep window", Description = "Keep tonight's sleep window.", SortOrder = 7 },
        new() { Code = "eat_slowly", Title = "Eat slowly", Description = "Chew thoroughly at meals.", SortOrder = 8 },
        new() { Code = "no_late_meal", Title = "No late meal", Description = "Finish dinner inside your eating window.", SortOrder = 9 }
    ];

    private static async Task SeedDemoUsersAsync(AppDbContext db, UserManager<ApplicationUser> users, CancellationToken cancellationToken)
    {
        var plan = await db.DietPlans.SingleAsync(p => p.Name == CatalogConstants.StarterPlanName, cancellationToken);
        await EnsureUserAsync(db, users, "demo@tevscare.app", "Demo1234!", "Avery Demo", Roles.User, true, plan.Id, cancellationToken);
        await EnsureUserAsync(db, users, "nutritionist@tevscare.app", "Coach1234!", "Nutrition Coach", Roles.Nutritionist, true, plan.Id, cancellationToken);
        await EnsureUserAsync(db, users, "admin@tevscare.app", "Admin1234!", "TEVSCARE Admin", Roles.Admin, true, plan.Id, cancellationToken);
    }

    private static async Task EnsureUserAsync(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        string email,
        string password,
        string name,
        string role,
        bool onboarded,
        Guid planId,
        CancellationToken cancellationToken)
    {
        if (await users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = name,
            Timezone = "Asia/Kolkata",
            CreatedAtUtc = DateTime.UtcNow
        };

        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        await users.AddToRoleAsync(user, role);
        db.Profiles.Add(new UserProfile
        {
            UserId = user.Id,
            Age = 32,
            HeightCm = 165,
            CurrentWeightKg = 72,
            StartingWeightKg = 72,
            TargetWeightKg = 65,
            DietaryPreference = DietaryPreference.Eggetarian,
            ActivityLevel = ActivityLevel.Light,
            WaterGoalMl = 2500,
            SleepGoalMinutes = 450,
            ActivityGoalMinutes = 30,
            OnboardingCompleted = onboarded
        });
        db.Budgets.Add(new BudgetSettings { UserId = user.Id, DailyBudgetAmount = 675, Currency = "INR" });
        db.NotificationSettings.Add(NotificationDefaults.Create(user.Id));
        db.Entitlements.Add(AccountFactory.FreeEntitlement(user.Id));
        db.PlanAssignments.Add(new UserPlanAssignment
        {
            UserId = user.Id,
            DietPlanId = planId,
            StartDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimezoneClockSafe())),
            Status = PlanAssignmentStatus.Active
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static TimeZoneInfo TimezoneClockSafe()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Kolkata", out var zone))
        {
            return zone;
        }

        return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
    }
}
