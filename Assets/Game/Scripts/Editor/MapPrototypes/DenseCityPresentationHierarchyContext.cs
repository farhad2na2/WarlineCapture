using System;
using Game.Authoring;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class DenseCityPresentationHierarchyContext
    {
        private readonly Transform buildings;
        private readonly Transform civicAndMarket;
        private readonly Transform infrastructure;
        private readonly Transform vegetation;
        private readonly Transform props;
        private readonly Transform horizon;

        private DenseCityPresentationHierarchyContext(DenseCityGeneratedRootAuthoring root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            string error = null;
            bool validRoot = root.TryValidate(out error);
            if (root.Role != DenseCityGeneratedRootRole.EntityPresentationSource || !validRoot)
            {
                throw new InvalidOperationException(
                    $"Dense-city presentation root is invalid: {error ?? "unexpected ownership role"}.");
            }

            buildings = RequireIdentityPath(root.transform, "GameplayBuildings/Buildings");
            civicAndMarket = RequireIdentityPath(root.transform, "GameplayBuildings/CivicAndMarket");
            infrastructure = RequireIdentityPath(root.transform, "RenderOnly/Infrastructure");
            vegetation = RequireIdentityPath(root.transform, "RenderOnly/Vegetation");
            props = RequireIdentityPath(root.transform, "RenderOnly/Props");
            horizon = RequireIdentityPath(root.transform, "RenderOnly/Horizon");
        }

        internal static DenseCityPresentationHierarchyContext Create(
            DenseCityGeneratedRootAuthoring root) =>
            new(root);

        internal Transform ResolveIndependentParent(
            DenseCityPresentationCategory category,
            GeneratedCityBuildingRole buildingRole = GeneratedCityBuildingRole.None)
        {
            return category switch
            {
                DenseCityPresentationCategory.GameplayBuildingIntact or
                    DenseCityPresentationCategory.GameplayBuildingDestroyed =>
                    buildingRole == GeneratedCityBuildingRole.Civic ? civicAndMarket : RequireBuildingRole(buildingRole),
                DenseCityPresentationCategory.Infrastructure => infrastructure,
                DenseCityPresentationCategory.Vegetation => vegetation,
                DenseCityPresentationCategory.Prop => props,
                DenseCityPresentationCategory.Horizon => horizon,
                DenseCityPresentationCategory.BuildingAttachmentIntact or
                    DenseCityPresentationCategory.BuildingAttachmentDestroyed =>
                    throw new InvalidOperationException(
                        "Building attachments must be realized beneath their declared building visual-state owner."),
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }

        internal Transform RequireAttachmentParent(
            DenseCityPresentationCategory category,
            Transform declaredBuildingVisualRoot)
        {
            if (category is not (DenseCityPresentationCategory.BuildingAttachmentIntact or
                DenseCityPresentationCategory.BuildingAttachmentDestroyed))
            {
                throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
            if (declaredBuildingVisualRoot == null)
                throw new ArgumentNullException(nameof(declaredBuildingVisualRoot));
            if (!declaredBuildingVisualRoot.IsChildOf(buildings) &&
                !declaredBuildingVisualRoot.IsChildOf(civicAndMarket))
            {
                throw new InvalidOperationException(
                    "The declared building visual-state owner is outside GameplayBuildings.");
            }

            return declaredBuildingVisualRoot;
        }

        private Transform RequireBuildingRole(GeneratedCityBuildingRole role)
        {
            if (role is <= GeneratedCityBuildingRole.None or > GeneratedCityBuildingRole.Other)
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
            return buildings;
        }

        private static Transform RequireIdentityPath(Transform root, string path)
        {
            Transform current = root;
            string[] segments = path.Split('/');
            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                string segment = segments[segmentIndex];
                Transform match = null;
                int matchCount = 0;
                for (int childIndex = 0; childIndex < current.childCount; childIndex++)
                {
                    Transform child = current.GetChild(childIndex);
                    if (!string.Equals(child.name, segment, StringComparison.Ordinal))
                        continue;
                    match = child;
                    matchCount++;
                }

                if (matchCount != 1 || !HasIdentityTransform(match))
                {
                    throw new InvalidOperationException(
                        $"Dense-city presentation path '{path}' must exist exactly once with identity transforms.");
                }
                current = match;
            }

            return current;
        }

        private static bool HasIdentityTransform(Transform transform) =>
            transform.localPosition == Vector3.zero &&
            transform.localRotation == Quaternion.identity &&
            transform.localScale == Vector3.one;
    }
}
