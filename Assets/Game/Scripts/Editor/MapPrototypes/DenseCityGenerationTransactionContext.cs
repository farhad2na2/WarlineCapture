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
            GeneratedCityBuildingRole role)
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
        }

        internal DenseCityBuildingBakeRecord Building { get; }
        internal Transform IntactPresentationRoot { get; }
        internal GameObject SourcePrefab { get; }
        internal GeneratedCityBuildingRole Role { get; }
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

        internal void RegisterRealizedBuildingOwner(
            DenseCityBuildingBakeRecord building,
            Transform intactPresentationRoot,
            GameObject sourcePrefab,
            GeneratedCityBuildingRole role)
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
                role));
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
            realizedBuildingOwners.Clear();
            realizedBuildingStableKeys.Clear();
            realizedBuildingRoots.Clear();
            realizedBuildingAttachments.Clear();
            realizedAttachmentRoots.Clear();
            disposed = true;
        }

        private void RequireActive()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DenseCityGenerationTransactionContext));
        }
    }
}
