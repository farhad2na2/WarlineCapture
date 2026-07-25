using System;
using System.Collections.Generic;
using Game.Configs;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityRealizedBuildingOwner
    {
        internal DenseCityRealizedBuildingOwner(
            DenseCityBuildingBakeRecord building,
            Transform intactPresentationRoot,
            GameObject sourcePrefab,
            GeneratedCityBuildingRole role,
            bool applyMaterialVariants,
            bool reservesOpenGroundClearance)
        {
            Building = building;
            IntactPresentationRoot = intactPresentationRoot != null
                ? intactPresentationRoot
                : throw new ArgumentNullException(nameof(intactPresentationRoot));
            SourcePrefab = sourcePrefab != null
                ? sourcePrefab
                : throw new ArgumentNullException(nameof(sourcePrefab));
            if (role is <= GeneratedCityBuildingRole.None or > GeneratedCityBuildingRole.Other)
                throw new ArgumentOutOfRangeException(nameof(role));
            Role = role;
            ApplyMaterialVariants = applyMaterialVariants;
            ReservesOpenGroundClearance = reservesOpenGroundClearance;
        }

        internal DenseCityBuildingBakeRecord Building { get; }
        internal Transform IntactPresentationRoot { get; }
        internal GameObject SourcePrefab { get; }
        internal GeneratedCityBuildingRole Role { get; }
        internal bool ApplyMaterialVariants { get; }
        internal bool ReservesOpenGroundClearance { get; }
    }

    internal readonly struct DenseCityRealizedBuildingAttachment
    {
        internal DenseCityRealizedBuildingAttachment(
            DenseCityPresentationBakeRecord presentation,
            Transform presentationRoot)
        {
            Presentation = presentation;
            PresentationRoot = presentationRoot != null
                ? presentationRoot
                : throw new ArgumentNullException(nameof(presentationRoot));
        }

        internal DenseCityPresentationBakeRecord Presentation { get; }
        internal Transform PresentationRoot { get; }
    }

    internal sealed class DenseCityGenerationTransactionContext : IDisposable
    {
        private readonly Dictionary<int, int> nextBuildingSequenceByDistrict = new();
        private readonly Dictionary<int, int> nextAttachmentSequenceByDistrict = new();
        private readonly Dictionary<int, int> nextInfrastructureSequenceByDistrict = new();
        private readonly List<DenseCityRealizedBuildingOwner> realizedBuildingOwners = new();
        private readonly HashSet<string> realizedBuildingStableKeys = new(StringComparer.Ordinal);
        private readonly HashSet<Transform> realizedBuildingRoots = new();
        private readonly List<DenseCityRealizedBuildingAttachment> realizedBuildingAttachments = new();
        private readonly HashSet<Transform> realizedAttachmentRoots = new();
        private bool disposed;

        internal DenseCityGenerationTransactionContext(
            int buildingCapacity,
            int surfaceCapacity,
            int presentationCapacity)
        {
            Records = new DenseCityGenerationRecordSet(
                buildingCapacity,
                surfaceCapacity,
                presentationCapacity);
        }

        internal DenseCityGenerationRecordSet Records { get; }

        internal IReadOnlyList<DenseCityRealizedBuildingOwner> RealizedBuildingOwners
        {
            get
            {
                RequireActive();
                return realizedBuildingOwners;
            }
        }

        internal IReadOnlyList<DenseCityRealizedBuildingAttachment> RealizedBuildingAttachments
        {
            get
            {
                RequireActive();
                return realizedBuildingAttachments;
            }
        }

        internal bool TryPlaceBuilding(
            int districtId,
            Func<int, DenseCityBuildingRecordGroup> createGroup,
            Func<bool> realize)
        {
            return TryPlaceBuilding(districtId, createGroup, realize, out _);
        }

        internal bool TryPlaceBuilding(
            int districtId,
            Func<int, DenseCityBuildingRecordGroup> createGroup,
            Func<bool> realize,
            out DenseCityBuildingBakeRecord acceptedBuilding)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = nextBuildingSequenceByDistrict.TryGetValue(districtId, out int nextSequence)
                ? nextSequence
                : 0;
            if (sequenceStart > int.MaxValue - 5)
                throw new InvalidOperationException("Dense-city building sequence capacity is exhausted.");

            DenseCityBuildingRecordGroup group = createGroup(sequenceStart);
            nextBuildingSequenceByDistrict[districtId] = sequenceStart + 5;
            bool accepted = DenseCityBuildingPlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
            acceptedBuilding = accepted ? group.Building : default;
            return accepted;
        }

        internal bool TryPlaceInfrastructure(
            int districtId,
            Func<int, DenseCityInfrastructureRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = GetInfrastructureSequenceStart(districtId, 2);
            DenseCityInfrastructureRecordGroup group = createGroup(sequenceStart);
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + 2;
            return DenseCityInfrastructurePlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal bool TryPlaceVisualBlocker(
            int districtId,
            Func<int, DenseCityVisualBlockerRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = GetInfrastructureSequenceStart(districtId, 2);
            DenseCityVisualBlockerRecordGroup group = createGroup(sequenceStart);
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + 2;
            return DenseCityVisualBlockerPlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal bool TryPlaceRoad(
            int districtId,
            int shoulderCount,
            Func<int, DenseCityRoadRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (shoulderCount < 0 || shoulderCount > 64)
                throw new ArgumentOutOfRangeException(nameof(shoulderCount));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int requiredCount = checked(2 + shoulderCount);
            int sequenceStart = GetInfrastructureSequenceStart(districtId, requiredCount);
            DenseCityRoadRecordGroup group = createGroup(sequenceStart);
            if (group.Shoulders.Length != shoulderCount)
            {
                throw new InvalidOperationException(
                    $"Road transaction declared {shoulderCount} shoulders but created {group.Shoulders.Length}.");
            }
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + requiredCount;
            return DenseCityRoadPlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal bool TryPlaceSurface(
            int districtId,
            Func<int, DenseCitySurfaceBakeRecord> createSurface,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createSurface == null)
                throw new ArgumentNullException(nameof(createSurface));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = GetInfrastructureSequenceStart(districtId, 1);
            DenseCitySurfaceBakeRecord surface = createSurface(sequenceStart);
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + 1;
            return DenseCitySurfacePlacementTransaction.TryCommitAndRealize(
                Records,
                surface,
                realize);
        }

        internal bool TryPlaceCanalWater(
            int districtId,
            Func<int, DenseCityCanalWaterRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = GetInfrastructureSequenceStart(districtId, 3);
            DenseCityCanalWaterRecordGroup group = createGroup(sequenceStart);
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + 3;
            return DenseCityCanalWaterPlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal bool TryPlaceTerrainVisuals(
            int districtId,
            int presentationCount,
            Func<int, DenseCityTerrainVisualRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (presentationCount <= 0 || presentationCount > 16)
                throw new ArgumentOutOfRangeException(nameof(presentationCount));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int requiredCount = checked(1 + presentationCount);
            int sequenceStart = GetInfrastructureSequenceStart(districtId, requiredCount);
            DenseCityTerrainVisualRecordGroup group = createGroup(sequenceStart);
            if (group.Presentations.Length != presentationCount)
            {
                throw new InvalidOperationException(
                    $"Terrain visual transaction declared {presentationCount} presentations but created " +
                    $"{group.Presentations.Length}.");
            }
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + requiredCount;
            return DenseCityTerrainVisualPlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal bool TryPlaceRenderOnlyPresentation(
            int districtId,
            Func<int, DenseCityPresentationBakeRecord> createPresentation,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createPresentation == null)
                throw new ArgumentNullException(nameof(createPresentation));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequence = GetInfrastructureSequenceStart(districtId, 1);
            DenseCityPresentationBakeRecord presentation = createPresentation(sequence);
            DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(presentation.Category);
            nextInfrastructureSequenceByDistrict[districtId] = sequence + 1;
            return DenseCityRenderOnlyPresentationPlacementTransaction.TryCommitAndRealize(
                Records,
                presentation,
                realize);
        }

        internal bool TryPlacePresentationOnlyTerrainVisuals(
            int districtId,
            int presentationCount,
            Func<int, DenseCityPresentationBakeRecord[]> createPresentations,
            Func<bool> realize)
        {
            return TryPlacePresentationOnlyVisuals(
                districtId,
                checked(1 + presentationCount),
                presentationCount,
                createPresentations,
                realize);
        }

        internal bool TryPlacePresentationOnlyVisuals(
            int districtId,
            int reservedSequenceCount,
            int presentationCount,
            Func<int, DenseCityPresentationBakeRecord[]> createPresentations,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (reservedSequenceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(reservedSequenceCount));
            if (presentationCount <= 0 || presentationCount > 16)
                throw new ArgumentOutOfRangeException(nameof(presentationCount));
            if (reservedSequenceCount < presentationCount)
                throw new ArgumentOutOfRangeException(nameof(reservedSequenceCount));
            if (createPresentations == null)
                throw new ArgumentNullException(nameof(createPresentations));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = GetInfrastructureSequenceStart(districtId, reservedSequenceCount);
            DenseCityPresentationBakeRecord[] presentations = createPresentations(sequenceStart);
            if (presentations == null || presentations.Length != presentationCount)
            {
                throw new InvalidOperationException(
                    $"Presentation-only terrain transaction declared {presentationCount} presentations.");
            }
            for (int index = 0; index < presentations.Length; index++)
            {
                DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(
                    presentations[index].Category);
            }

            nextInfrastructureSequenceByDistrict[districtId] =
                checked(sequenceStart + reservedSequenceCount);
            return DenseCityRenderOnlyPresentationGroupPlacementTransaction.TryCommitAndRealize(
                Records,
                presentations,
                realize);
        }

        internal bool TryPlaceRenderOnlyPresentation(
            int districtId,
            DenseCityPresentationHierarchyContext presentationHierarchy,
            Func<int, DenseCityPresentationBakeRecord> createPresentation,
            Func<Transform, Transform> realizeUnderExplicitParent)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (presentationHierarchy == null)
                throw new ArgumentNullException(nameof(presentationHierarchy));
            if (createPresentation == null)
                throw new ArgumentNullException(nameof(createPresentation));
            if (realizeUnderExplicitParent == null)
                throw new ArgumentNullException(nameof(realizeUnderExplicitParent));

            int sequence = GetInfrastructureSequenceStart(districtId, 1);
            DenseCityPresentationBakeRecord presentation = createPresentation(sequence);
            DenseCityRenderOnlyPresentationRecordFactory.RequireRenderOnlyCategory(presentation.Category);
            nextInfrastructureSequenceByDistrict[districtId] = sequence + 1;
            Transform realizedRoot = null;
            try
            {
                bool accepted = DenseCityRenderOnlyPresentationPlacementTransaction.TryCommitAndRealize(
                    Records,
                    presentation,
                    () =>
                    {
                        Transform parent = presentationHierarchy.ResolveIndependentParent(
                            presentation.Category);
                        realizedRoot = realizeUnderExplicitParent(parent);
                        if (realizedRoot == null)
                            return false;
                        presentationHierarchy.RequireIndependentRoot(
                            presentation.Category,
                            realizedRoot);
                        RequireWorldMatrix(presentation, realizedRoot);
                        return true;
                    });
                if (!accepted && realizedRoot != null)
                    UnityEngine.Object.DestroyImmediate(realizedRoot.gameObject);
                return accepted;
            }
            catch
            {
                if (realizedRoot != null)
                    UnityEngine.Object.DestroyImmediate(realizedRoot.gameObject);
                throw;
            }
        }

        internal bool TryPlaceBridge(
            int districtId,
            Func<int, DenseCityBridgeRecordGroup> createGroup,
            Func<bool> realize)
        {
            RequireActive();
            if (districtId < 0)
                throw new ArgumentOutOfRangeException(nameof(districtId));
            if (createGroup == null)
                throw new ArgumentNullException(nameof(createGroup));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));

            int sequenceStart = GetInfrastructureSequenceStart(districtId, 4);
            DenseCityBridgeRecordGroup group = createGroup(sequenceStart);
            nextInfrastructureSequenceByDistrict[districtId] = sequenceStart + 4;
            return DenseCityBridgePlacementTransaction.TryCommitAndRealize(
                Records,
                group,
                realize);
        }

        internal void RegisterRealizedBuildingOwner(
            DenseCityBuildingBakeRecord building,
            Transform intactPresentationRoot,
            GameObject sourcePrefab,
            GeneratedCityBuildingRole role,
            bool applyMaterialVariants = true,
            bool reservesOpenGroundClearance = true)
        {
            RequireActive();
            if (intactPresentationRoot == null)
                throw new ArgumentNullException(nameof(intactPresentationRoot));
            string stableKey = building.Identity.StableKey;
            if (string.IsNullOrEmpty(stableKey))
                throw new ArgumentException("A committed building record is required.", nameof(building));
            if (realizedBuildingStableKeys.Contains(stableKey) || realizedBuildingRoots.Contains(intactPresentationRoot))
            {
                throw new InvalidOperationException(
                    $"Dense-city realized building ownership is duplicated: '{stableKey}'.");
            }

            realizedBuildingOwners.Add(new DenseCityRealizedBuildingOwner(
                building,
                intactPresentationRoot,
                sourcePrefab,
                role,
                applyMaterialVariants,
                reservesOpenGroundClearance));
            realizedBuildingStableKeys.Add(stableKey);
            realizedBuildingRoots.Add(intactPresentationRoot);
        }

        internal DenseCityRealizedBuildingOwner GetRequiredRealizedBuildingOwner(Transform intactPresentationRoot)
        {
            RequireActive();
            if (intactPresentationRoot == null)
                throw new ArgumentNullException(nameof(intactPresentationRoot));
            for (int index = 0; index < realizedBuildingOwners.Count; index++)
            {
                if (realizedBuildingOwners[index].IntactPresentationRoot == intactPresentationRoot)
                    return realizedBuildingOwners[index];
            }

            throw new InvalidOperationException(
                $"Dense-city realized building root is not registered: '{intactPresentationRoot.name}'.");
        }

        internal bool TryPlaceBuildingAttachment(
            DenseCityRealizedBuildingOwner owner,
            GameObject attachmentPrefab,
            Transform attachmentRoot,
            Matrix4x4 worldMatrix,
            DenseCityPresentationCategory category,
            Func<bool> realize)
        {
            RequireActive();
            if (attachmentPrefab == null)
                throw new ArgumentNullException(nameof(attachmentPrefab));
            if (attachmentRoot == null)
                throw new ArgumentNullException(nameof(attachmentRoot));
            if (realize == null)
                throw new ArgumentNullException(nameof(realize));
            if (category != DenseCityPresentationCategory.BuildingAttachmentIntact)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(category),
                    "Only attachments beneath the registered intact presentation root are currently accepted.");
            }
            string ownerStableKey = owner.Building.Identity.StableKey;
            if (!realizedBuildingStableKeys.Contains(ownerStableKey) ||
                !realizedBuildingRoots.Contains(owner.IntactPresentationRoot))
            {
                throw new InvalidOperationException(
                    $"Dense-city attachment owner is not registered: '{ownerStableKey}'.");
            }
            if (attachmentRoot.parent != owner.IntactPresentationRoot)
            {
                throw new InvalidOperationException(
                    $"Dense-city attachment is not beneath its declared intact root: '{ownerStableKey}'.");
            }
            if (realizedAttachmentRoots.Contains(attachmentRoot))
                throw new InvalidOperationException("Dense-city attachment transform ownership is duplicated.");

            int districtId = owner.Building.Identity.DistrictId;
            int sequence = nextAttachmentSequenceByDistrict.TryGetValue(districtId, out int nextSequence)
                ? nextSequence
                : 0;
            if (sequence == int.MaxValue)
                throw new InvalidOperationException("Dense-city attachment sequence capacity is exhausted.");
            nextAttachmentSequenceByDistrict[districtId] = sequence + 1;

            DenseCityVisualAssetMetadata metadata =
                DenseCityVisualAssetMetadataExtractor.Extract(attachmentPrefab);
            var identity = new DenseCityRecordIdentity(
                owner.Building.Identity.GeneratorSchema,
                owner.Building.Identity.Seed,
                districtId,
                "building-attachment-intact",
                sequence,
                metadata.PrefabAssetGuid,
                metadata.PrefabLocalId);
            var attachment = new DenseCityPresentationBakeRecord(
                identity,
                category,
                metadata.PrefabAssetGuid,
                null,
                metadata.MaterialAssetGuids,
                worldMatrix,
                true,
                true,
                2,
                ownerStableKey);
            bool accepted = DenseCityBuildingAttachmentTransaction.TryCommitAndRealize(
                Records,
                attachment,
                realize);
            if (accepted)
            {
                realizedBuildingAttachments.Add(new DenseCityRealizedBuildingAttachment(
                    attachment,
                    attachmentRoot));
                realizedAttachmentRoots.Add(attachmentRoot);
            }
            return accepted;
        }

        internal void Seal()
        {
            RequireActive();
            Records.Seal();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            Records.Dispose();
            nextBuildingSequenceByDistrict.Clear();
            nextAttachmentSequenceByDistrict.Clear();
            nextInfrastructureSequenceByDistrict.Clear();
            realizedBuildingOwners.Clear();
            realizedBuildingStableKeys.Clear();
            realizedBuildingRoots.Clear();
            realizedBuildingAttachments.Clear();
            realizedAttachmentRoots.Clear();
            disposed = true;
        }

        private static void RequireWorldMatrix(
            DenseCityPresentationBakeRecord presentation,
            Transform realizedRoot)
        {
            Matrix4x4 actual = realizedRoot.localToWorldMatrix;
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(actual[index] - presentation.WorldMatrix[index]) <= 0.0001f)
                    continue;
                throw new InvalidOperationException(
                    $"Dense-city presentation transform drift: '{presentation.Identity.StableKey}'.");
            }
        }

        private int GetInfrastructureSequenceStart(int districtId, int requiredCount)
        {
            int sequenceStart = nextInfrastructureSequenceByDistrict.TryGetValue(
                districtId,
                out int nextSequence)
                ? nextSequence
                : 0;
            if (sequenceStart > int.MaxValue - requiredCount)
                throw new InvalidOperationException("Dense-city infrastructure sequence capacity is exhausted.");
            return sequenceStart;
        }

        private void RequireActive()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DenseCityGenerationTransactionContext));
        }
    }
}
