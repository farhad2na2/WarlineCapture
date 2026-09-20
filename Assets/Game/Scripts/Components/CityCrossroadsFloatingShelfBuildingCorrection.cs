using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Components
{
    /// <summary>
    /// Lowers leftover City Crossroads gameplay buildings that still sit on
    /// archived interior-hill Y. Dense-city visual grading removed the hills,
    /// and those resident meshes (notably <c>SM_Bld_Plinth_01</c>) are absent
    /// from the virtualized render database, so they keep SubScene renderers
    /// and cast a rectangular shadow gap above authored grade.
    /// </summary>
    public static class CityCrossroadsFloatingShelfBuildingCorrection
    {
        public const float MinimumRaisedHeight = 3.5f;
        public const float MaximumRaisedHeight = 9.5f;
        public const float TargetHeight = 0.01f;

        /// <summary>Civic hall / statue-side look-at used by camera D.</summary>
        public static readonly float3 CivicHallPlate = new(1031.03f, 7.01f, 539.84f);

        /// <summary>Sand plinth immediately south of the civic hall (camera D plate).</summary>
        public static readonly float3 CivicPlinthPlate = new(1043.26f, 6.20f, 488.48f);

        /// <summary>Northern sand plinth in the camera A shelf (statue-side north).</summary>
        public static readonly float3 NorthernPlinthPlate = new(964.98f, 6.20f, 687.84f);

        public const string CivicHallBuildingName = "Building_0020_SM_Bld_Hall_01";
        public const string CivicPlinthBuildingName = "Building_0078_SM_Bld_Plinth_01 (6)";
        public const string NorthernPlinthBuildingName = "Building_0060_SM_Bld_Plinth_01 (6)";

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
    }
}
