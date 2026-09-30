using System;
using Game.Components;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class CH04M01AirCorridorInputProbe
    {
        private static bool Budget => SessionState.GetBool(Active + ".Budget", false);
        private static bool budgetVerified;
        private static int budgetStep, budgetOrders;
        private static double budgetInputAt;
        public static void RunBudgetEnglish()
        {
            SessionState.SetBool(Active + ".Budget", true);
            SessionState.SetBool(Active + ".Recovery", false);
            SessionState.SetBool(Active + ".Manual", true);
            SessionState.SetString(Locale, "en");
            budgetVerified = false; budgetStep = budgetOrders = 0; budgetInputAt = 0;
            Run();
        }
        private static int ReadMaterials(EntityManager em)
        {
            using var q = em.CreateEntityQuery(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
            using var entities = q.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
                if (em.GetComponentData<FactionEconomy>(entity).FactionId == 1)
                {
                    if (em.GetComponentData<FactionEconomy>(entity).Money != 0)
                        throw new InvalidOperationException("Tactical production touched account Credits.");
                    return em.GetComponentData<FactionTacticalMaterialsComponent>(entity).Current;
                }
            throw new InvalidOperationException("Player Materials owner missing.");
        }
        private static bool TickBudget(EntityManager em)
        {
            if (budgetVerified || !UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) || panel.TutorialStep < 4)
                return false;
            if (EditorApplication.timeSinceStartup - budgetInputAt < .5) return true;
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            if (budgetStep == 0)
            {
                if (ReadMaterials(em) != 140) throw new InvalidOperationException("Wrong authored local budget.");
                var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                if (!Click(controls?.BuildButton)) return true;
                budgetStep = 1; budgetInputAt = EditorApplication.timeSinceStartup; return true;
            }
            if (drawer == null || !drawer.IsOpen)
            {
                var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                if (Click(controls?.BuildButton)) budgetInputAt = EditorApplication.timeSinceStartup;
                return true;
            }
            if (budgetStep == 1)
            {
                var catalog = drawer.GetComponent<BuildDrawerCatalogRuntimeView>() ?? drawer.GetComponentInChildren<BuildDrawerCatalogRuntimeView>();
                if (catalog == null) catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>();
                var target = budgetOrders == 0
                    ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Soldiers, "Unit_Chr_Soldier_Male_02_Alt_04")
                    : catalog?.ResolveCatalogTarget(BuildDrawerCategory.Buildings, "Building_Barrack");
                if (target == null) return true;
                if (target != drawer.PrimaryActionButton) { if (Click(target)) budgetInputAt = EditorApplication.timeSinceStartup; return true; }
                int expected = 140 - budgetOrders * 30;
                if (ReadMaterials(em) != expected) throw new InvalidOperationException("Production spend was not exact once: expected=" + expected);
                if (budgetOrders == 0)
                {
                    if (!Click(target)) return true;
                    budgetOrders++; budgetInputAt = EditorApplication.timeSinceStartup;
                    Debug.Log("[AirCorridorBudgetInput] order=" + budgetOrders + " source=visible-production-button");
                    return true;
                }
                if (target.IsInteractable()) throw new InvalidOperationException("Unaffordable Barracks placement remained enabled.");
                string feedback = drawer.InstructionText?.text ?? "";
                if (feedback.IndexOf("material", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Shortage lacked visible Materials feedback: " + feedback);
                // The existing producer has one slot. A 30-Materials rifle
                // leaves 110, making the 120-Materials Barracks unaffordable.
                Shot("materials-shortage");
                budgetStep = 4; budgetInputAt = EditorApplication.timeSinceStartup; return true;
            }
            if (budgetStep == 4)
            {
                var cancel = drawer.ActiveItemView?.CancelButton ?? drawer.CancelButton;
                if (!Click(cancel)) return true;
                budgetStep = 2; budgetInputAt = EditorApplication.timeSinceStartup; return true;
            }
            if (budgetStep == 2)
            {
                if (ReadMaterials(em) != 140) throw new InvalidOperationException("Cancel did not refund exactly 30 Materials: actual=" + ReadMaterials(em));
                if (!drawer.PrimaryActionButton.IsInteractable()) throw new InvalidOperationException("Refund did not restore affordability.");
                Shot("materials-refund");
                budgetStep = 3; budgetInputAt = EditorApplication.timeSinceStartup; return true;
            }
            if (ReadMaterials(em) != 140) throw new InvalidOperationException("Refund changed after cancellation.");
            if (!Click(drawer.CloseButton)) return true;
            budgetVerified = true;
            Debug.Log("[AirCorridorBudgetInput] result=Passed start=140 rifle-order=110 barracks-cost=120 shortage=visible-disabled cancel=140 affordable=restored stable-refund=140 Credits=0 input=normal-UI");
            return false;
        }
    }
}
