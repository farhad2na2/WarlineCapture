using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public static class ArmoryCatalogCategoryFormatter
    {
        public static string Format(ArmoryCatalogCategory category)
        {
            return category switch
            {
                ArmoryCatalogCategory.Aircrafts => "AIRCRAFT",
                ArmoryCatalogCategory.Buildings => "BUILDING",
                ArmoryCatalogCategory.Vehicles => "VEHICLE",
                ArmoryCatalogCategory.Support => "SUPPORT",
                _ => "CHARACTER"
            };
        }
    }
}
