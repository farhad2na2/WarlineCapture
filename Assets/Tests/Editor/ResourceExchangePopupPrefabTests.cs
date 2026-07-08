using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResourceExchangePopupPrefabTests
{
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
    private const string ApprovedSpriteRoot = "Assets/Game/Art/UI/Generated/ResourceExchange/LayeredOneGo/";

    [Test]
    public void ResourceExchangePopupPrefab_ExposesRequiredViewReferences()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab, $"Missing Resource Exchange popup prefab at {PrefabPath}.");

        ResourceExchangePopupView view = prefab.GetComponent<ResourceExchangePopupView>();
        Assert.NotNull(view, "Resource Exchange popup must own ResourceExchangePopupView.");
        ResourceExchangePopupRuntimeView runtimeView = prefab.GetComponent<ResourceExchangePopupRuntimeView>();
        Assert.NotNull(runtimeView, "Resource Exchange popup must own ResourceExchangePopupRuntimeView.");
        Assert.AreSame(view, runtimeView.View, "Runtime presenter must target the serialized Resource Exchange view.");
        Assert.NotNull(view.CloseButton, "Close button must be serialized.");
        Assert.NotNull(view.ExportTabButton, "Export tab must be serialized.");
        Assert.NotNull(view.ImportTabButton, "Import tab must be serialized.");
        Assert.NotNull(view.ConfirmButton, "Confirm button must be serialized.");
        Assert.NotNull(view.AmountDecreaseButton, "Amount decrease button must be serialized.");
        Assert.NotNull(view.AmountIncreaseButton, "Amount increase button must be serialized.");
        Assert.NotNull(view.RushAllButton, "Rush All button must be serialized.");
        Assert.NotNull(view.ClearCompletedButton, "Clear Completed button must be serialized.");
        Assert.NotNull(view.RecipeContentRoot, "Recipe content root must be serialized.");
        Assert.NotNull(view.RecipeCardTemplate, "Recipe card template must be serialized.");
        Assert.NotNull(view.QueueContentRoot, "Queue content root must be serialized.");
        Assert.NotNull(view.QueueRowTemplate, "Queue row template must be serialized.");
        Assert.That(view.StaticRecipeCards.Length, Is.GreaterThanOrEqualTo(6), "Prefab must expose six route card views.");
        Assert.That(view.StaticQueueRows.Length, Is.GreaterThanOrEqualTo(4), "Prefab must expose four exchange queue rows.");
    }

    [Test]
    public void ResourceExchangePopupPrefab_RecipeCardsAndQueueRowsExposeButtons()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        ResourceExchangePopupView view = prefab.GetComponent<ResourceExchangePopupView>();
        Assert.NotNull(view);

        for (int i = 0; i < view.StaticRecipeCards.Length; i++)
        {
            ResourceExchangeRecipeCardView card = view.StaticRecipeCards[i];
            Assert.NotNull(card, $"Recipe card {i} must be serialized.");
            Assert.NotNull(card.SelectionButton, $"Recipe card {i} must expose its selection button.");
            Assert.NotNull(card.FrameImage, $"Recipe card {i} must expose its frame image.");
            Assert.NotNull(card.ThumbnailImage, $"Recipe card {i} must expose its thumbnail image.");
            Assert.NotNull(card.TitleText, $"Recipe card {i} must expose its title text.");
        }

        for (int i = 0; i < view.StaticQueueRows.Length; i++)
        {
            ResourceExchangeQueueItemView row = view.StaticQueueRows[i];
            Assert.NotNull(row, $"Queue row {i} must be serialized.");
            Assert.NotNull(row.RushButton, $"Queue row {i} must expose its rush button.");
            Assert.NotNull(row.CancelButton, $"Queue row {i} must expose its cancel button.");
            Assert.NotNull(row.ProgressFillImage, $"Queue row {i} must expose its progress fill.");
            Assert.NotNull(row.NameText, $"Queue row {i} must expose its name text.");
        }
    }

    [Test]
    public void ResourceExchangePopupPrefab_UsesOnlySeparatedResourceExchangeSprites()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.NotNull(prefab);

        Image[] images = prefab.GetComponentsInChildren<Image>(true);
        Assert.That(images.Length, Is.GreaterThan(20), "Prefab should be built from reusable Image layers.");
        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = images[i].sprite;
            if (sprite == null)
                continue;

            string path = AssetDatabase.GetAssetPath(sprite);
            Assert.IsTrue(
                path.StartsWith(ApprovedSpriteRoot, System.StringComparison.Ordinal),
                $"{images[i].name} uses {path}; Resource Exchange popup must not use target screenshots or unrelated sprite folders.");
        }
    }
}
