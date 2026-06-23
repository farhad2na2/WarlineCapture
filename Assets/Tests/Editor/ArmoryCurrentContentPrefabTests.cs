using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArmoryCurrentContentPrefabTests
{
    private const string ArmoryContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab";

    private GameObject instance;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ArmoryContentSectionsExposeCurrentRuntimeViews),
                test => test.ArmoryContentSectionsExposeCurrentRuntimeViews(),
                ref passed);
            RunValidationStep(
                nameof(ArmoryContentListBindsCurrentInspectionPanel),
                test => test.ArmoryContentListBindsCurrentInspectionPanel(),
                ref passed);
            RunValidationStep(
                nameof(ArmoryContentListRefreshesWhenMetadataResolversBindAfterEnable),
                test => test.ArmoryContentListRefreshesWhenMetadataResolversBindAfterEnable(),
                ref passed);

            Debug.Log($"[ArmoryCurrentContentValidation] result=Passed tests={passed}");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ArmoryCurrentContentValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Failed();
        }
    }

    private static void RunValidationStep(
        string name,
        Action<ArmoryCurrentContentPrefabTests> action,
        ref int passed)
    {
        var tests = new ArmoryCurrentContentPrefabTests();
        try
        {
            action(tests);
            passed++;
            Debug.Log($"[ArmoryCurrentContentValidation] step={name} result=Passed");
        }
        finally
        {
            tests.TearDown();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (instance != null)
            UnityEngine.Object.DestroyImmediate(instance);
    }

    [Test]
    public void ArmoryContentSectionsExposeCurrentRuntimeViews()
    {
        UIShellContentSectionsView sections = InstantiateSections();

        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Left, out GameObject left));
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Middle, out GameObject middle));
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Right, out GameObject right));
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Footer, out GameObject footer));
        Assert.NotNull(footer);

        ArmoryCategoryNavigationView navigation = left.GetComponent<ArmoryCategoryNavigationView>();
        Assert.NotNull(navigation, "Armory left section must own the category navigation view.");
        AssertCategoryTabsAssigned(navigation);

        ArmoryContentListView list = middle.GetComponent<ArmoryContentListView>();
        Assert.NotNull(list, "Armory middle section must own the catalog list view.");
        AssertSerializedReference(list, "unitPrefabRegistryConfig");
        AssertSerializedReference(list, "buildingPlacementConfig");
        AssertSerializedReference(list, "contentRoot");
        AssertSerializedReference(list, "itemTemplate");
        AssertCatalogItemTemplateAssigned((ArmoryCatalogItemView)new SerializedObject(list)
            .FindProperty("itemTemplate")
            .objectReferenceValue);

        ArmoryRightContentView rightView = right.GetComponent<ArmoryRightContentView>();
        Assert.NotNull(rightView, "Armory right section must own the right-content view.");
        Assert.NotNull(rightView.InspectionPanel, "Armory right-content view must serialize its inspection panel.");
        AssertInspectionPanelAssigned(rightView.InspectionPanel);
    }

    [Test]
    public void ArmoryContentListBindsCurrentInspectionPanel()
    {
        UIShellContentSectionsView sections = InstantiateSections();
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Middle, out GameObject middle));
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Right, out GameObject right));

        ArmoryContentListView list = middle.GetComponent<ArmoryContentListView>();
        ArmoryInspectionPanelView inspection = right.GetComponent<ArmoryRightContentView>().InspectionPanel;
        Assert.NotNull(list);
        Assert.NotNull(inspection);

        list.ConfigureCatalogMetadataResolvers(
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);
        list.SetInspectionPanel(inspection);
        list.RefreshForTests(ArmoryCatalogCategory.Characters);

        TMP_Text titleText = (TMP_Text)new SerializedObject(inspection)
            .FindProperty("titleText")
            .objectReferenceValue;
        TMP_Text typeText = (TMP_Text)new SerializedObject(inspection)
            .FindProperty("typeText")
            .objectReferenceValue;
        TMP_Text healthText = (TMP_Text)new SerializedObject(inspection)
            .FindProperty("healthValueText")
            .objectReferenceValue;

        Assert.False(string.IsNullOrWhiteSpace(titleText.text), "The current Armory list should populate the inspection title from catalog data.");
        Assert.AreNotEqual("ITEM NAME", titleText.text, "The inspection title should no longer show placeholder copy after binding.");
        Assert.False(string.IsNullOrWhiteSpace(typeText.text), "The current Armory list should populate the inspection type.");
        Assert.False(string.IsNullOrWhiteSpace(healthText.text), "The current Armory list should populate the inspection health value.");
    }

    [Test]
    public void ArmoryContentListRefreshesWhenMetadataResolversBindAfterEnable()
    {
        UIShellContentSectionsView sections = InstantiateSections();
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Middle, out GameObject middle));
        Assert.IsTrue(sections.TryGetSection(UIShellContentSectionId.Right, out GameObject right));

        ArmoryContentListView list = middle.GetComponent<ArmoryContentListView>();
        ArmoryInspectionPanelView inspection = right.GetComponent<ArmoryRightContentView>().InspectionPanel;
        Assert.NotNull(list);
        Assert.NotNull(inspection);

        list.SetInspectionPanel(inspection);
        list.ConfigureCatalogMetadataResolvers(
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetBuildingMetadata,
            UiCatalogAuthoringMetadataUiSystemHelper.TryGetUnitMetadata);

        TMP_Text titleText = (TMP_Text)new SerializedObject(inspection)
            .FindProperty("titleText")
            .objectReferenceValue;
        ArmoryCatalogItemView itemTemplate = (ArmoryCatalogItemView)new SerializedObject(list)
            .FindProperty("itemTemplate")
            .objectReferenceValue;

        Assert.True(itemTemplate.gameObject.activeSelf, "Binding metadata resolvers after OnEnable should repopulate the Armory catalog list.");
        Assert.False(string.IsNullOrWhiteSpace(titleText.text), "Binding metadata resolvers after OnEnable should populate the inspection title.");
        Assert.AreNotEqual("ITEM NAME", titleText.text, "The inspection title should no longer show placeholder copy after late resolver binding.");
    }

    private UIShellContentSectionsView InstantiateSections()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoryContentPrefabPath);
        Assert.NotNull(prefab, $"Missing Armory content prefab at {ArmoryContentPrefabPath}.");

        instance = UnityEngine.Object.Instantiate(prefab);
        instance.name = prefab.name;

        UIShellContentSectionsView sections = instance.GetComponent<UIShellContentSectionsView>();
        Assert.NotNull(sections, "Armory content must expose serialized shell sections.");
        return sections;
    }

    private static void AssertCategoryTabsAssigned(ArmoryCategoryNavigationView navigation)
    {
        SerializedObject serialized = new(navigation);
        SerializedProperty tabs = serialized.FindProperty("tabs");
        Assert.NotNull(tabs);
        Assert.GreaterOrEqual(tabs.arraySize, 4);

        HashSet<ArmoryCatalogCategory> categories = new();
        for (int i = 0; i < tabs.arraySize; i++)
        {
            SerializedProperty tab = tabs.GetArrayElementAtIndex(i);
            categories.Add((ArmoryCatalogCategory)tab.FindPropertyRelative("category").enumValueIndex);
            Assert.NotNull(tab.FindPropertyRelative("button").objectReferenceValue, $"Armory category tab {i} needs a button reference.");
            Assert.NotNull(tab.FindPropertyRelative("frame").objectReferenceValue, $"Armory category tab {i} needs a frame reference.");
        }

        Assert.IsTrue(categories.Contains(ArmoryCatalogCategory.Characters));
        Assert.IsTrue(categories.Contains(ArmoryCatalogCategory.Vehicles));
        Assert.IsTrue(categories.Contains(ArmoryCatalogCategory.Aircrafts));
        Assert.IsTrue(categories.Contains(ArmoryCatalogCategory.Buildings));
    }

    private static void AssertCatalogItemTemplateAssigned(ArmoryCatalogItemView item)
    {
        Assert.NotNull(item, "Armory catalog list must serialize an item template.");
        AssertSerializedReference(item, "selectionButton");
        AssertSerializedReference(item, "frameImage");
        AssertSerializedReference(item, "defaultFrameSprite");
        AssertSerializedReference(item, "selectedFrameSprite");
        AssertSerializedReference(item, "titleText");
        AssertSerializedReference(item, "typeText");

        SerializedProperty visuals = new SerializedObject(item).FindProperty("categoryVisuals");
        Assert.NotNull(visuals);
        Assert.Greater(visuals.arraySize, 0);
    }

    private static void AssertInspectionPanelAssigned(ArmoryInspectionPanelView inspection)
    {
        AssertSerializedReference(inspection, "titleText");
        AssertSerializedReference(inspection, "typeText");
        AssertSerializedReference(inspection, "descriptionText");
        AssertSerializedReference(inspection, "healthValueText");
        AssertSerializedReference(inspection, "damageValueText");
        AssertSerializedReference(inspection, "rangeValueText");
        AssertSerializedReference(inspection, "speedValueText");
        AssertSerializedReference(inspection, "moveCapabilityText");
        AssertSerializedReference(inspection, "patrolCapabilityText");
        AssertSerializedReference(inspection, "attackCapabilityText");
        AssertSerializedReference(inspection, "holdCapabilityText");

        SerializedProperty visuals = new SerializedObject(inspection).FindProperty("categoryVisuals");
        Assert.NotNull(visuals);
        Assert.Greater(visuals.arraySize, 0);
    }

    private static void AssertSerializedReference(UnityEngine.Object target, string propertyName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
        Assert.NotNull(property, $"{target.name} is missing serialized property {propertyName}.");
        Assert.NotNull(property.objectReferenceValue, $"{target.name}.{propertyName} must be assigned.");
    }
}
