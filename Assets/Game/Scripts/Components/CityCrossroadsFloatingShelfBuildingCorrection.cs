using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Components
{
    /// <summary>
    /// Lowers leftover City Crossroads geometry that still sits on archived
    /// interior-hill Y. Gameplay building roots store world Y in
    /// <see cref="LocalTransform"/> and can be slammed to grade. The player-visible
    /// rectangular sand plate is a resident RenderOnly mesh
    /// (<c>SM_Env_Ground_Square_01</c> plus <c>SM_Prop_Statue_*</c>) parented
    /// under the rotated map root, so its <see cref="LocalTransform"/> XZ is
    /// local and must be tested in world space via <see cref="LocalToWorld"/>.
    /// Those meshes are absent from the virtualized render database.
    /// </summary>
    public static class CityCrossroadsFloatingShelfBuildingCorrection
    {
        public const float MinimumRaisedHeight = 3.5f;
        public const float MaximumRaisedHeight = 9.5f;
        public const float MaximumResidentRaisedHeight = 11.5f;
        public const float TargetHeight = 0.01f;

        /// <summary>
        /// Authored Y of the leftover <c>SM_Env_Ground_Square_01</c> stamps.
        /// Resident props keep their relative height above this plate.
        /// </summary>
        public const float PlateReferenceHeight = 5.85f;

        public static float ResidentHeightDelta => TargetHeight - PlateReferenceHeight;

        /// <summary>Civic hall / statue-side look-at used by camera D.</summary>
        public static readonly float3 CivicHallPlate = new(1031.03f, 7.01f, 539.84f);

        /// <summary>Sand plinth immediately south of the civic hall (camera D plate).</summary>
        public static readonly float3 CivicPlinthPlate = new(1043.26f, 6.20f, 488.48f);

        /// <summary>Northern sand plinth in the camera A shelf (statue-side north).</summary>
        public static readonly float3 NorthernPlinthPlate = new(964.98f, 6.20f, 687.84f);

        /// <summary>Large civic sand tile under the golden statue (camera D).</summary>
        public static readonly float3 CivicGroundSquarePlate = new(1033.16f, 5.85f, 509.37f);

        /// <summary>Golden statue standing on the civic sand tile.</summary>
        public static readonly float3 CivicStatue = new(1026.72f, 10.59f, 523.35f);

        /// <summary>Cylindrical pedestal under the civic statue.</summary>
        public static readonly float3 CivicStatueBase = new(1026.63f, 5.89f, 523.35f);

        /// <summary>Large northern sand tile at the player-base join (camera A).</summary>
        public static readonly float3 NorthernGroundSquarePlate = new(985.87f, 5.85f, 697.94f);

        /// <summary>Golden statue at the northern base-join plate.</summary>
        public static readonly float3 NorthernStatue = new(999.85f, 10.59f, 704.38f);

        public const string CivicHallBuildingName = "Building_0020_SM_Bld_Hall_01";
        public const string CivicPlinthBuildingName = "Building_0078_SM_Bld_Plinth_01 (6)";
        public const string NorthernPlinthBuildingName = "Building_0060_SM_Bld_Plinth_01 (6)";
        public const string CivicGroundSquareName = "SM_Env_Ground_Square_01 (8)";
        public const string CivicStatueName = "SM_Prop_Statue_01 (2)";
        public const string CivicStatueBaseName = "SM_Prop_Statue_Base_02";

        // Conservative unions of the three archived-hill stamps inside the
        // City Crossroads playable window. xmin, zmin, xmax, zmax.
        private static readonly float4[] Shelves =
        {
            new float4(952f, 646f, 1033f, 710f),
            new float4(1008f, 476f, 1084f, 562f),
            new float4(1050f, 566f, 1123f, 653f)
        };

        public static bool ContainsShelf(float x, float z)
        {
            for (int i = 0; i < Shelves.Length; i++)
            {
                float4 shelf = Shelves[i];
                if (x >= shelf.x && z >= shelf.y && x <= shelf.z && z <= shelf.w)
                    return true;
            }

            return false;
        }

        public static bool TryCorrect(ref float3 position)
        {
            if (position.y < MinimumRaisedHeight || position.y > MaximumRaisedHeight)
                return false;
            if (!ContainsShelf(position.x, position.z))
                return false;

            position.y = TargetHeight;
            return true;
        }

        public static bool TryCorrect(ref LocalTransform transform)
        {
            float3 position = transform.Position;
            if (!TryCorrect(ref position))
                return false;

            transform.Position = position;
            return true;
        }

        public static bool TryCorrectResidentVisual(
            float3 worldPosition,
            float localY,
            out float deltaY)
        {
            deltaY = 0f;
            if (localY < MinimumRaisedHeight)
                return false;
            if (worldPosition.y < MinimumRaisedHeight ||
                worldPosition.y > MaximumResidentRaisedHeight)
            {
                return false;
            }
            if (!ContainsShelf(worldPosition.x, worldPosition.z))
                return false;

            deltaY = ResidentHeightDelta;
            return true;
        }

        public static bool TryCorrectResidentVisual(
            ref LocalTransform transform,
            in LocalToWorld localToWorld)
        {
            if (!TryCorrectResidentVisual(
                    localToWorld.Position,
                    transform.Position.y,
                    out float deltaY))
            {
                return false;
            }

            float3 position = transform.Position;
            position.y += deltaY;
            transform.Position = position;
            return true;
        }

        public static bool TryCorrectResidentWorldMatrix(ref LocalToWorld localToWorld)
        {
            float3 world = localToWorld.Position;
            if (world.y < MinimumRaisedHeight || world.y > MaximumResidentRaisedHeight)
                return false;
            if (!ContainsShelf(world.x, world.z))
                return false;

            float4x4 value = localToWorld.Value;
            value.c3.y += ResidentHeightDelta;
            localToWorld.Value = value;
            return true;
        }

        public static void ApplyResidentWorldDelta(ref LocalToWorld localToWorld, float deltaY)
        {
            float4x4 value = localToWorld.Value;
            value.c3.y += deltaY;
            localToWorld.Value = value;
        }
    }
}
