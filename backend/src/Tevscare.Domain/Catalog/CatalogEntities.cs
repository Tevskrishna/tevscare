using Tevscare.Domain.Common;
using Tevscare.Domain.Enums;

namespace Tevscare.Domain.Catalog;

public class FoodCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public GroceryCategory GroceryCategory { get; set; }
    public ICollection<Food> Foods { get; set; } = new List<Food>();
}

public class Food : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public FoodCategory Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? LocalName { get; set; }
    public string ServingLabel { get; set; } = "1 serving";
    public decimal ServingQuantity { get; set; } = 1;
    public string ServingUnit { get; set; } = "serving";
    public decimal Calories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbohydratesG { get; set; }
    public decimal FatG { get; set; }
    public decimal FibreG { get; set; }
    public string? MicronutrientsNote { get; set; }
    public string? Allergens { get; set; }
    public string? Tags { get; set; }
    public DietFlags SuitableFor { get; set; } = DietFlags.All;
    public GroceryCategory GroceryCategory { get; set; }
    public decimal? ReferencePriceInr { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProviderNote { get; set; }
}

public class UserFoodPrice : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid FoodId { get; set; }
    public Food Food { get; set; } = null!;
    public decimal PricePerServing { get; set; }
    public string Currency { get; set; } = "INR";
}
