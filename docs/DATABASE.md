# Database

PostgreSQL 16 locally. EF Core migrations live in `backend/src/Tevscare.Infrastructure/Persistence/Migrations`.

The API calls `Database.MigrateAsync()` on startup for PostgreSQL. Tests set `Tevscare:DatabaseProvider=Sqlite` and use `EnsureCreated` instead.

## Tables

Identity tables plus:

- Catalog: `FoodCategories`, `Foods`, `UserFoodPrices`
- Planning: `DietPlans`, `DietPlanDays`, `Meals`, `MealItems`, `UserPlanAssignments`, `ProviderGuidances`, `HabitDefinitions`
- Tracking: `MealLogs`, `MealLogItems`, `WaterEntries`, `WeightEntries`, `ActivityLogs`, `SleepLogs`, `DailyCheckIns`, `HabitChecks`, `AdherenceSnapshots`
- Account: `UserProfiles`, `UserAllergies`, `UserFoodPreferences`, `RefreshTokens`, `PasswordResetTokens`, `UserNotificationSettings`, `NotificationPreferences`, `BudgetSettings`, `ShoppingItemStates`, `Entitlements`, `ProductEvents`

Nutrition columns sit on `Food`. A separate nutrition table was not needed for the MVP.

Enums are stored as strings, except the diet suitability flags which are an integer bit field.

`MealItem.AlternativeGroup` marks choices such as breakfast protein. The default allowed item is what the shopping list counts.

Child rows with client-generated `Guid` keys must be `DbSet.Add`ed. Adding them only through a modified parent marks them as existing and the update affects zero rows.

## Local connection

`Host=localhost;Port=5432;Database=tevscare;Username=tevscare;Password=tevscare_dev_password`

Design-time `dotnet ef` uses `ConnectionStrings__Default` or that same local string. Add a migration from `backend`:

```powershell
dotnet ef migrations add <Name> --project src/Tevscare.Infrastructure --startup-project src/Tevscare.Api --output-dir Persistence/Migrations
```
