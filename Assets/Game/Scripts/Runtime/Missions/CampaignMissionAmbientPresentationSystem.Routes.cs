using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionAmbientPresentationSystem
    {
        internal const byte PanicCivilianPresentationKind = 1;
        internal const byte CalmCivilianPresentationKind = 2;
        internal const byte BasePersonnelPresentationKind = 3;

        private static readonly FixedString64Bytes M01PanicPresentationId =
            "ambient.ch01.m01.civilians";
        private static readonly FixedString64Bytes M02CalmCivilianPresentationId =
            "ambient.ch01.m02.civilians";
        private static readonly FixedString64Bytes M02BasePersonnelPresentationId =
            "ambient.ch01.m02.base_personnel";

        internal static bool TryResolveRouteContract(
            ref OperationMapBlob map,
            ref CampaignMissionAmbientPresentationBlob ambient,
            out byte presentationKind,
            out AmbientRouteAnchors anchors)
        {
            presentationKind = 0;
            anchors = default;
            if (ambient.PresentationId.IsEmpty || ambient.RouteId.IsEmpty ||
                !CampaignMissionSpawnSystem.TryFindAnchor(ref map, ambient.AnchorId, out _))
                return false;

            if (ambient.PresentationId.Equals(M01PanicPresentationId))
            {
                presentationKind = PanicCivilianPresentationKind;
                return TryResolveAnchors(
                    ref map,
                    "anchor.ch01.m01.player_spawn",
                    "anchor.ch01.m01.patrol_spawn",
                    "anchor.ch01.m01.civilian_evacuation",
                    out anchors);
            }

            if (ambient.PresentationId.Equals(M02CalmCivilianPresentationId))
            {
                presentationKind = CalmCivilianPresentationKind;
                return TryResolveAnchors(
                    ref map,
                    "anchor.ch01.m02.civilian_edge",
                    "anchor.ch01.m02.civilian_evacuation",
                    "anchor.ch01.m02.build_lot",
                    out anchors);
            }

            if (ambient.PresentationId.Equals(M02BasePersonnelPresentationId))
            {
                presentationKind = BasePersonnelPresentationKind;
                return TryResolveAnchors(
                    ref map,
                    "anchor.ch01.m02.resource_focus",
                    "anchor.ch01.m02.forward_post",
                    "anchor.ch01.m02.build_lot",
                    out anchors);
            }

            return false;
        }

        internal static FixedString64Bytes PresentationPrefabKey(byte presentationKind, int index)
        {
            if (presentationKind == BasePersonnelPresentationKind)
            {
                return index switch
                {
                    0 => new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_02"),
                    1 => new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04"),
                    2 => new FixedString64Bytes("Unit_Chr_Soldier_Female_01_Alt_01"),
                    _ => new FixedString64Bytes("Unit_Chr_Soldier_Female_02_Alt_01")
                };
            }

            return index switch
            {
                0 => new FixedString64Bytes("Unit_Chr_Civilian_Male_01"),
                1 => new FixedString64Bytes("Unit_Chr_Civilian_Female_01"),
                2 => new FixedString64Bytes("Unit_Chr_Civilian_Male_02"),
                _ => new FixedString64Bytes("Unit_Chr_Civilian_Female_02")
            };
        }

        internal static AmbientRoute CreateAmbientRoute(
            byte presentationKind,
            in AmbientRouteAnchors anchors,
            int ordinal,
            int seed)
        {
            if (presentationKind == PanicCivilianPresentationKind)
                return CreatePanicRoute(in anchors.First, in anchors.Second, ordinal, seed);

            return CreateLoopRoute(
                in anchors,
                ordinal,
                seed,
                presentationKind == BasePersonnelPresentationKind);
        }

        private static bool TryResolveAnchors(
            ref OperationMapBlob map,
            in FixedString64Bytes firstId,
            in FixedString64Bytes secondId,
            in FixedString64Bytes thirdId,
            out AmbientRouteAnchors anchors)
        {
            anchors = default;
            if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map, firstId, out anchors.First) ||
                !CampaignMissionSpawnSystem.TryFindAnchor(ref map, secondId, out anchors.Second) ||
                !CampaignMissionSpawnSystem.TryFindAnchor(ref map, thirdId, out anchors.Third))
                return false;
            return true;
        }

        private static void SetLocomotionAnimation(
            EntityManager entityManager,
            Entity entity,
            UnitAnimationKind preferred)
        {
            if (!entityManager.HasBuffer<UnitAnimationOrderEntry>(entity))
                return;

            DynamicBuffer<UnitAnimationOrderEntry> animationOrder =
                entityManager.GetBuffer<UnitAnimationOrderEntry>(entity);
            byte animationIndex = byte.MaxValue;
            byte fallbackIndex = byte.MaxValue;
            UnitAnimationKind fallback = preferred == UnitAnimationKind.Run
                ? UnitAnimationKind.Walk
                : UnitAnimationKind.Run;
            for (int index = 0; index < animationOrder.Length; index++)
            {
                UnitAnimationKind kind = (UnitAnimationKind)animationOrder[index].Kind;
                if (kind == preferred)
                {
                    animationIndex = (byte)(index + 1);
                    break;
                }
                if (fallbackIndex == byte.MaxValue && kind == fallback)
                    fallbackIndex = (byte)(index + 1);
            }

            if (animationIndex == byte.MaxValue)
                animationIndex = fallbackIndex;
            if (animationIndex != byte.MaxValue)
            {
                SetOrAdd(entityManager, entity, new UnitResolvedAnimationIndex
                {
                    Value = animationIndex,
                    Changed = 1,
                    Updated = 1
                });
            }
        }

        private static AmbientRoute CreateLoopRoute(
            in AmbientRouteAnchors anchors,
            int ordinal,
            int seed,
            bool basePersonnel)
        {
            uint hash = math.hash(new int3(seed ^ (basePersonnel ? 0x63B1 : 0x37D9), ordinal + 1, 0x45));
            int centerIndex = ordinal % 3;
            float3 center = centerIndex switch
            {
                0 => anchors.First.Position,
                1 => anchors.Second.Position,
                _ => anchors.Third.Position
            };
            float3 forward = math.normalizesafe(
                anchors.Third.Position - anchors.First.Position,
                new float3(1f, 0f, 0f));
            forward.y = 0f;
            forward = math.normalizesafe(forward, new float3(1f, 0f, 0f));
            float3 lateral = new(forward.z, 0f, -forward.x);
            float angle = ordinal * 2.399963f + UnsignedUnit(hash, 8) * 0.65f;
            float radius = (basePersonnel ? 4.5f : 3.5f) + UnsignedUnit(hash, 16) * 2f;
            float3 start = LoopPoint(center, forward, lateral, angle, radius);
            float3 first = LoopPoint(center, forward, lateral, angle + 2.094395f, radius);
            float3 second = LoopPoint(center, forward, lateral, angle + 4.18879f, radius);

            return new AmbientRoute
            {
                Start = start,
                AlleyMerge = first,
                SquadPass = second,
                Exit = start,
                Speed = (basePersonnel ? 1.8f : 1.5f) + UnsignedUnit(hash, 24) * 0.7f,
                DelaySeconds = 0f,
                RouteIndex = ordinal,
                Loop = 1
            };
        }

        private static float3 LoopPoint(
            float3 center,
            float3 forward,
            float3 lateral,
            float angle,
            float radius) =>
            center + forward * (math.cos(angle) * radius) + lateral * (math.sin(angle) * radius);

        private static AmbientRoute CreatePanicRoute(
            in OperationMapAnchorBlob player,
            in OperationMapAnchorBlob hostile,
            int ordinal,
            int seed)
        {
            uint hashA = math.hash(new int3(seed ^ 0x51A7, ordinal + 1, 0x2B));
            uint hashB = math.hash(new int3(seed ^ 0x2C31, ordinal + 1, 0x73));
            float3 towardSquad = math.normalizesafe(
                player.Position - hostile.Position,
                new float3(0f, 0f, -1f));
            towardSquad.y = 0f;
            towardSquad = math.normalizesafe(towardSquad, new float3(0f, 0f, -1f));
            float3 lateral = new(towardSquad.z, 0f, -towardSquad.x);
            int routeIndex = ordinal & 7;
            float side = (routeIndex & 1) == 0 ? 1f : -1f;
            float alongJitter = SignedUnit(hashA, 0);
            float lateralJitter = SignedUnit(hashA, 8);
            float waypointJitter = SignedUnit(hashA, 16);
            float exitJitter = SignedUnit(hashB, 8);
            float speedJitter = UnsignedUnit(hashB, 24);

            float3 start;
            float3 alleyMerge;
            if (routeIndex <= 1)
            {
                start = hostile.Position + towardSquad * (7f + alongJitter * 5f) +
                        lateral * (side * (17f + lateralJitter * 3f));
                alleyMerge = hostile.Position + towardSquad * (18f + waypointJitter * 3f) +
                             lateral * (side * (7f + lateralJitter * 2f));
            }
            else if (routeIndex <= 3)
            {
                start = hostile.Position + towardSquad * (5f + alongJitter * 8f) +
                        lateral * (side * (9f + lateralJitter * 3f));
                alleyMerge = hostile.Position + towardSquad * (19f + waypointJitter * 4f) +
                             lateral * (side * (4f + lateralJitter * 2f));
            }
            else if (routeIndex <= 5)
            {
                start = hostile.Position + towardSquad * (3f + alongJitter * 6f) +
                        lateral * (side * (4.5f + lateralJitter * 2f));
                alleyMerge = hostile.Position + towardSquad * (20f + waypointJitter * 4f) +
                             lateral * (side * (6f + lateralJitter * 3f));
            }
            else
            {
                start = hostile.Position + towardSquad * (4f + alongJitter * 8f) +
                        lateral * (side * (15f + lateralJitter * 4f));
                alleyMerge = hostile.Position + towardSquad * (11f + waypointJitter * 5f) +
                             lateral * (side * (27f + lateralJitter * 5f));
            }

            bool towardFriendlyLine = routeIndex <= 5;
            float3 squadPass = towardFriendlyLine
                ? player.Position - towardSquad * (10f + waypointJitter * 5f) +
                  lateral * (side * (8f + lateralJitter * 6f))
                : hostile.Position + towardSquad * (19f + waypointJitter * 5f) +
                  lateral * (side * (38f + lateralJitter * 6f));
            float3 exit = towardFriendlyLine
                ? player.Position + towardSquad * (27f + exitJitter * 7f) +
                  lateral * (side * (19f + lateralJitter * 8f))
                : player.Position - towardSquad * (2f + exitJitter * 7f) +
                  lateral * (side * (55f + lateralJitter * 8f));

            return new AmbientRoute
            {
                Start = start,
                AlleyMerge = alleyMerge,
                SquadPass = squadPass,
                Exit = exit,
                Speed = 6.4f + speedJitter * 1.6f,
                DelaySeconds = 0f,
                RouteIndex = routeIndex,
                Loop = 0
            };
        }

        private static float SignedUnit(uint hash, int shift) =>
            UnsignedUnit(hash, shift) * 2f - 1f;

        private static float UnsignedUnit(uint hash, int shift) =>
            ((hash >> shift) & 255u) / 255f;

        internal struct AmbientRouteAnchors
        {
            public OperationMapAnchorBlob First;
            public OperationMapAnchorBlob Second;
            public OperationMapAnchorBlob Third;
        }

        internal struct AmbientRoute
        {
            public float3 Start;
            public float3 AlleyMerge;
            public float3 SquadPass;
            public float3 Exit;
            public float Speed;
            public float DelaySeconds;
            public int RouteIndex;
            public byte Loop;
        }
    }
}
