using System;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class MatchHudResourceTextRepairTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            MatchHudResourceTextRepair.Build();
            new MatchHudResourceTextRepairTests().LiveValuesSurviveLocaleAndBindingRefresh();
            var resources = new UiShellEcsGatewayResourceHeaderTests();
            int count = 0;
            foreach (var method in resources.GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(TestAttribute), true).Length > 0))
            { method.Invoke(resources, null); count++; }
            new M02EstablishBaseResourceTests().M02HudShowsCreditsAndMaterialsWhileHidingLogistics();
            new M03PresentationTests().AllTwelveAriaLessonsFitBothLanguagesAndTextSizes();
            Debug.Log($"[MatchHudResourceTextRepair] result=Passed bindingLocales=3 headerTests={count} m02Credits=1 ariaLayouts=48");
            ValidationExit.Passed();
        }
        catch (Exception error) { Debug.LogException(error); ValidationExit.Failed(); }
    }

    [Test]
    public void LiveValuesSurviveLocaleAndBindingRefresh()
    {
        string locale = GameLocalization.CurrentLocaleCode;
        World previous = World.DefaultGameObjectInjectionWorld;
        using var world = new World("HudResourceLocaleRepair");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        var instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), "en", false);
            World.DefaultGameObjectInjectionWorld = world;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            var em = world.EntityManager;
            var boundary = em.CreateEntity(typeof(UiShellRootComponent), typeof(UiMatchHudHeaderComponent));
            em.SetComponentData(boundary, new UiMatchHudHeaderComponent { MaterialsText = "80/100", CivilianRiskText = "LOW" });
            var summary = em.AddBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary);
            summary.Add(new BuildingRuntimeFactionUsableFuelSummary { FactionId = FactionIdentity.PlayerFactionId,
                StoredOilBarrels = 12000, StoredFuelBarrels = 25700, OilStorageCapacity = 30000, FuelStorageCapacity = 30000, Version = 1 });
            Transform Slot(string name) => instance.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
            TMP_Text Label(string name) => Slot(name).Find("Label").GetComponent<TMP_Text>();
            TMP_Text Value(string name) => Slot(name).Find("Value").GetComponent<TMP_Text>();
            // Exercise the shared binding path as well as the prefab's unbound numeric text path.
            var numericBinding=Value("FuelSlot").GetComponent<V3LocalizedTextBinding>()??Value("FuelSlot").gameObject.AddComponent<V3LocalizedTextBinding>();
            numericBinding.Configure("","",false);
            var presentation = new MatchHudResourceHeaderPresentation();
            presentation.Bind(Slot("OilSlot").gameObject, Label("MaterialsSlot"), Value("MaterialsSlot"),
                Label("OilSlot"), Value("OilSlot"), Label("FuelSlot"), Value("FuelSlot"), Label("CivilianRiskSlot"), Value("CivilianRiskSlot"), 0);
            foreach (string language in new[] { "en", "fa-IR", "en" })
            {
                GameLocalization.SetLocale(language, false);
                presentation.RefreshNow();
                foreach (var binding in instance.GetComponentsInChildren<V3LocalizedTextBinding>(true)) binding.ApplyLocalization();
                presentation.RefreshNow();
                Assert.AreEqual("12K", Value("OilSlot").text);
                Assert.AreEqual("25.7K", Value("FuelSlot").text);
                Assert.AreEqual(language == "en" ? "Fuel" : "بنزین", GameLocalization.Get("ui.hud.fuel"));
                if (language == "fa-IR")
                {
                    Assert.AreEqual("نفت", GameLocalization.Get("ui.hud.oil"));
                    Assert.AreEqual(V3LocalizedTextBinding.ShapeForRendering("بنزین"), Label("FuelSlot").text);
                }
                Assert.IsFalse(Value("FuelSlot").text.Contains("#"));
                Value("FuelSlot").text="authoring placeholder";
                Value("FuelSlot").GetComponent<V3LocalizedTextBinding>().SetLocalizedValue("25.7K");
                Assert.AreEqual("25.7K",Value("FuelSlot").text,"Reapplying the same live value must repair an intervening prefab/presentation write.");
            }
            var changed = summary[0]; changed.StoredOilBarrels = 0; changed.StoredFuelBarrels = 900; changed.Version++; summary[0] = changed;
            presentation.RefreshNow();
            Assert.AreEqual("0", Value("OilSlot").text);
            Assert.AreEqual("900", Value("FuelSlot").text);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
            World.DefaultGameObjectInjectionWorld = previous;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            GameLocalization.SetLocale(locale, false);
        }
    }
}
