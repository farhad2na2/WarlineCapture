using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static class UiShellSettingsAdapter
        {
        public static bool TrySetAssistantPanelOpen(bool open)
        {
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;
            if (open && !UiShellActionAdapter.IsAssistantRuntimeActive(entityManager, boundary))
                return false;

            if (!entityManager.HasComponent<AssistantStateComponent>(boundary))
                return false;
            AssistantStateComponent assistant = entityManager.GetComponentData<AssistantStateComponent>(boundary);
            byte next = open ? (byte)1 : (byte)0;
            if (assistant.PanelOpen == next)
                return true;

            assistant.PanelOpen = next;
            assistant.UiDirty = 1;
            entityManager.SetComponentData(boundary, assistant);
            return true;
        }

        public static bool TryReadArmoryCategory(out ArmoryCatalogCategory category)
        {
            category = ArmoryCatalogCategory.Characters;
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureArmoryCategoryState(entityManager, boundary);
            category = entityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary).Category;
            return true;
        }

        public static bool TryEnqueueArmoryCategory(ArmoryCatalogCategory category)
        {
            if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
                return false;

            EnsureArmoryCategoryState(entityManager, boundary);
            DynamicBuffer<UiShellArmoryCategoryRequestComponent> requests =
                entityManager.GetBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
            requests.Add(new UiShellArmoryCategoryRequestComponent
            {
                Category = category
            });
            return true;
        }

        private static void EnsureArmoryCategoryState(EntityManager entityManager, Entity boundary)
        {
            if (!entityManager.HasComponent<UiShellArmoryCategoryComponent>(boundary))
            {
                entityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
                {
                    Category = ArmoryCatalogCategory.Characters
                });
            }

            if (!entityManager.HasBuffer<UiShellArmoryCategoryRequestComponent>(boundary))
                entityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        }


        }
    }
}
