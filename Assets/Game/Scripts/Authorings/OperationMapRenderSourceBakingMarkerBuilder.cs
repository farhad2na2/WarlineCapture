using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Configs;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    internal static class OperationMapRenderSourceBakingMarkerBuilder
    {
        internal static void AddMarkers(
            Baker<OperationMapVirtualizedPresentationAuthoring> baker,
            GameObject sourcePresentationRoot,
            OperationMapRenderDatabaseBakeConfig config,
            Entity databaseEntity)
        {
            if (baker == null)
                throw new ArgumentNullException(nameof(baker));
            if (sourcePresentationRoot == null)
                throw new ArgumentNullException(nameof(sourcePresentationRoot));
            baker.DependsOn(sourcePresentationRoot);
            if (config == null)
            {
                throw new InvalidOperationException(
                    "A render database is required before source-row stripping.");
            }
            if (!config.TryValidateSchema(out string configError))
            {
                throw new InvalidOperationException(
                    $"A valid render database is required before source-row stripping: {configError}");
            }

            DynamicBuffer<OperationMapRenderEligibleSourceRowBakingComponent> expectedRows =
                baker.AddBuffer<OperationMapRenderEligibleSourceRowBakingComponent>(
                    databaseEntity);
            var expectedKeys = new HashSet<SourceRowKey>();
            for (int placementIndex = 0;
                 placementIndex < config.Placements.Count;
                 placementIndex++)
            {
                OperationMapRenderPlacementConfigRecord placement =
                    config.Placements[placementIndex];
                OperationMapRenderPrototypeConfigRecord prototype =
                    config.Prototypes[placement.PrototypeIndex];
                bool eligible =
                    (prototype.EligibilityFlags &
                     OperationMapRenderEligibilityFlags.Eligible) != 0;
                bool residentException =
                    (prototype.EligibilityFlags &
                     OperationMapRenderEligibilityFlags.AlwaysResidentException) != 0;
                if (!eligible || residentException)
                    continue;

                for (int partOffset = 0; partOffset < prototype.PartCount; partOffset++)
                {
                    OperationMapRenderPrototypePartConfigRecord part =
                        config.Parts[prototype.FirstPart + partOffset];
                    if (part.SubMeshIndex != 0)
                    {
                        throw new InvalidOperationException(
                            "VRP-034 supports only the accepted single-submesh render-only " +
                            $"pilot, but logical part {prototype.FirstPart + partOffset} " +
                            $"uses submesh {part.SubMeshIndex}.");
                    }

                    var row = new OperationMapRenderEligibleSourceRowBakingComponent
                    {
                        OwnerIdentity = new OperationMapRenderIdentity128
                        {
                            Low = placement.StableIdentityLow,
                            High = placement.StableIdentityHigh
                        },
                        RendererPathIdentity = new OperationMapRenderIdentity128
                        {
                            Low = part.RendererPathIdentityLow,
                            High = part.RendererPathIdentityHigh
                        },
                        Mesh = config.Meshes[part.MeshIndex].Mesh,
                        Material = config.Materials[part.MaterialIndex].Material,
                        SubMeshIndex = (ushort)part.SubMeshIndex
                    };
                    if (!expectedKeys.Add(SourceRowKey.From(row)))
                    {
                        throw new InvalidOperationException(
                            "Eligible logical rows contain a duplicate owner/path identity.");
                    }
                    expectedRows.Add(row);
                }
            }

            if (expectedRows.Length == 0)
            {
                throw new InvalidOperationException(
                    "Source-row stripping requires at least one eligible logical row.");
            }

        }

        internal static void AddOwnerMarkers<TAuthoring>(
            Baker<TAuthoring> baker,
            TAuthoring authoring,
            string ownerIdentitySource,
            OperationMapEntityPresentationRole role)
            where TAuthoring : Component
        {
            if (baker == null)
                throw new ArgumentNullException(nameof(baker));
            if (authoring == null)
                throw new ArgumentNullException(nameof(authoring));

            Transform owner = authoring.transform;
            Entity ownerEntity =
                baker.GetEntity(authoring.gameObject, TransformUsageFlags.Renderable);
            DynamicBuffer<OperationMapRenderSourceRowBakingComponent> sourceRows =
                baker.AddBuffer<OperationMapRenderSourceRowBakingComponent>(ownerEntity);
            Renderer[] renderers = authoring.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (HasNestedStableOwner(renderer.transform, owner))
                    continue;

                baker.DependsOn(renderer);
                Entity renderEntity =
                    baker.GetEntity(renderer.gameObject, TransformUsageFlags.Renderable);
                sourceRows.Add(new OperationMapRenderSourceRowBakingComponent
                {
                    RenderEntity = renderEntity,
                    OwnerIdentity = Project(ownerIdentitySource),
                    RendererPathIdentity =
                        Project("renderer-path|" + GetIndexedRelativePath(owner, renderer.transform)),
                    IsRenderOnlyOwner = (byte)(
                        role == OperationMapEntityPresentationRole.RenderOnly ? 1 : 0)
                });
            }
        }

        private static bool HasNestedStableOwner(
            Transform renderer,
            Transform expectedOwner)
        {
            for (Transform current = renderer; current != null && current != expectedOwner;
                 current = current.parent)
            {
                if (current.GetComponent<DenseCityPresentationIdentityAuthoring>() != null ||
                    current.GetComponent<OperationMapEntityPresentationIdentityAuthoring>() != null)
                    return true;
            }
            return false;
        }

        private static string GetIndexedRelativePath(Transform owner, Transform target)
        {
            if (owner == target)
                return "<owner>";

            var parts = new List<string>();
            Transform current = target;
            while (current != null && current != owner)
            {
                parts.Add($"{current.name}[{current.GetSiblingIndex()}]");
                current = current.parent;
            }
            if (current != owner)
            {
                throw new InvalidOperationException(
                    $"Renderer '{target.name}' is not beneath its stable owner.");
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static OperationMapRenderIdentity128 Project(string source)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(source);
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
                digest = sha256.ComputeHash(bytes);

            return new OperationMapRenderIdentity128
            {
                Low = ReadUInt64LittleEndian(digest, 0),
                High = ReadUInt64LittleEndian(digest, sizeof(ulong))
            };
        }

        private static ulong ReadUInt64LittleEndian(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (int index = 0; index < sizeof(ulong); index++)
                value |= (ulong)bytes[offset + index] << (index * 8);
            return value;
        }

        private readonly struct SourceRowKey : IEquatable<SourceRowKey>
        {
            private readonly ulong _ownerLow;
            private readonly ulong _ownerHigh;
            private readonly ulong _pathLow;
            private readonly ulong _pathHigh;

            private SourceRowKey(
                OperationMapRenderIdentity128 owner,
                OperationMapRenderIdentity128 path)
            {
                _ownerLow = owner.Low;
                _ownerHigh = owner.High;
                _pathLow = path.Low;
                _pathHigh = path.High;
            }

            internal static SourceRowKey From(
                OperationMapRenderEligibleSourceRowBakingComponent row) =>
                new(row.OwnerIdentity, row.RendererPathIdentity);

            public bool Equals(SourceRowKey other) =>
                _ownerLow == other._ownerLow &&
                _ownerHigh == other._ownerHigh &&
                _pathLow == other._pathLow &&
                _pathHigh == other._pathHigh;

            public override bool Equals(object obj) =>
                obj is SourceRowKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)_ownerLow ^ (int)(_ownerLow >> 32);
                    hash = (hash * 397) ^ (int)_ownerHigh ^ (int)(_ownerHigh >> 32);
                    hash = (hash * 397) ^ (int)_pathLow ^ (int)(_pathLow >> 32);
                    return (hash * 397) ^ (int)_pathHigh ^ (int)(_pathHigh >> 32);
                }
            }
        }
    }
}
