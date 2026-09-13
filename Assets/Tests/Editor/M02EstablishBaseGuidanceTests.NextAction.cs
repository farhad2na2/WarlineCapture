#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using Game.Catalog.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class M02EstablishBaseGuidanceTests
{
    [Test]
    public void DefenseBuildCueSkipsSelectedTabAndFollowsRealItemThenPlaceClicks()
    {
        UiShellRuntimeGateway.Register(null);
        var root=new GameObject("Drawer",typeof(RectTransform)); root.SetActive(false);
        var content=new GameObject("Content",typeof(RectTransform)); content.transform.SetParent(root.transform,false);
        var card=new GameObject("Card",typeof(RectTransform),typeof(Image),typeof(Button)); card.transform.SetParent(content.transform,false);
        var primary=new GameObject("Place",typeof(RectTransform),typeof(Image),typeof(Button)); primary.transform.SetParent(root.transform,false);
        var barracks=new GameObject("Building_Barrack"); var barrier=new GameObject("Building_Road_Barrier");
        try
        {
            var view=root.AddComponent<BuildDrawerView>(); var item=card.AddComponent<BuildDrawerItemView>();
            SetPrivateField(item,"selectionButton",card.GetComponent<Button>());
            SetPrivateField(view,"drawerRoot",root); SetPrivateField(view,"itemContentRoot",content.transform as RectTransform);
            SetPrivateField(view,"itemTemplate",item); SetPrivateField(view,"buildButton",primary.GetComponent<Button>());
            var tabs=CreateCategoryTabs(root.transform); SetPrivateField(view,"tabs",tabs);
            var catalog=root.AddComponent<BuildDrawerCatalogRuntimeView>(); var command=new TestBuildingUiCommand();
            catalog.ConfigureForTests(view,new TestPrefabSource(Array.Empty<GameObject>(),Array.Empty<GameObject>()),
                new TestPrefabSource(Array.Empty<GameObject>(),new[]{barracks,barrier}));
            catalog.ConfigureCatalogMetadataResolvers(ResolveGuidanceBuilding,null); catalog.BindRuntimeCommands(command,null);
            root.SetActive(true); catalog.RefreshForTests();
            Button next=catalog.ResolveBuildingTutorialTarget(true,true,out var caption);
            Assert.That(caption,Is.EqualTo("tutorial.next.defense"));
            Assert.That(next,Is.Not.Null); Assert.That(next,Is.Not.EqualTo(tabs[0].Button));
            Assert.That(command.PlaceRequests,Is.Zero);
            next.onClick.Invoke(); // This is the actual catalog listener, not an injected acknowledgement.
            next=catalog.ResolveBuildingTutorialTarget(true,true,out caption);
            Assert.That(caption,Is.EqualTo("tutorial.next.place")); Assert.That(next,Is.EqualTo(view.PrimaryActionButton));
            Assert.That(command.PlaceRequests,Is.Zero,"Selection must not spend resources.");
            next.onClick.Invoke(); Assert.That(command.PlaceRequests,Is.EqualTo(1)); Assert.That(command.ConfirmRequests,Is.Zero);
            Assert.That(command.HasPendingBuildingPlacement,Is.True,"Placement remains a separate confirmation.");
            catalog.SelectCategoryForTests(BuildDrawerCategory.Soldiers);
            next=catalog.ResolveBuildingTutorialTarget(true,true,out caption);
            Assert.That(caption,Is.EqualTo("tutorial.next.buildings")); Assert.That(next,Is.EqualTo(tabs[0].Button));
            next.onClick.Invoke();
            Assert.That(catalog.ResolveBuildingTutorialTarget(true,true,out _),Is.Not.EqualTo(tabs[0].Button));
        }
        finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(barracks);UnityEngine.Object.DestroyImmediate(barrier);}
    }

    private static bool ResolveGuidanceBuilding(GameObject prefab,out UiBuildingCatalogMetadata metadata)
    {
        metadata=new UiBuildingCatalogMetadata(prefab.name,"",true,0,15,30,Vector2Int.one,null,null,null,100,false,false,false,false,0,0);
        return true;
    }
}
#endif
