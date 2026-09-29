using Game.Editor.MapVariants;

// One wrapper run owns export, generation and validation in sequence.
public static class MapVariantPreparationValidation
{
    public static void Run()
    {
        MapVariantPreparationInventory.ExportAll();
        MapVariantRefineryPreparationSlice.Build();
        MapVariantPreparationTests.RunFocusedValidation();
    }
}
