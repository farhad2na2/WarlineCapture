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

    internal sealed class DenseCityGenerationTransactionContext : IDisposable
    {
        private readonly Dictionary<int, int> nextBuildingSequenceByDistrict = new();
        private readonly List<DenseCityRealizedBuildingOwner> realizedBuildingOwners = new();
        private readonly HashSet<string> realizedBuildingStableKeys = new(StringComparer.Ordinal);
        private readonly HashSet<Transform> realizedBuildingRoots = new();
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
            realizedBuildingOwners.Clear();
            realizedBuildingStableKeys.Clear();
            realizedBuildingRoots.Clear();
            disposed = true;
        }

        private void RequireActive()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DenseCityGenerationTransactionContext));
        }
    }
}
