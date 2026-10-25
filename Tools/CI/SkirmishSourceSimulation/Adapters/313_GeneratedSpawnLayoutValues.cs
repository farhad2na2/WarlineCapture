using Game.Configs;using Game.Skirmish.Contracts;
namespace Game.Runtime{internal static class GeneratedSpawnLayoutValues{
public static bool TryResolveUnitSpawnWorld(
            SkirmishResolvedSetup setup,
            byte factionId,
            SkirmishRoleKind role,
            int memberIndex,
            out Vector3 world)
        {
            return TryResolveMeasuredWorld(setup, factionId, false, string.Empty, role, memberIndex, out world);
        }
public static bool TryResolveMeasuredWorld(
            SkirmishResolvedSetup setup,
            byte factionId,
            bool isStructure,
            string structureId,
            SkirmishRoleKind role,
            int memberIndex,
            out Vector3 world)
        {
            world = default;
            if (setup == null || !setup.MeasuredLayoutBound)
                return false;

            if (isStructure)
            {
                if (TryFindStructure(setup, factionId, structureId, out SkirmishResolvedStructureEntry structure) &&
                    (structure.SpawnWorldX != 0f || structure.SpawnWorldZ != 0f || setup.PlayerStagingWorldX != 0f))
                {
                    world = new Vector3(structure.SpawnWorldX, 0f, structure.SpawnWorldZ);
                    return true;
                }

                if (structureId == SkirmishStructureIds.GroundStaging)
                {
                    world = factionId == 2
                        ? new Vector3(setup.EnemyStagingWorldX, 0f, setup.EnemyStagingWorldZ)
                        : new Vector3(setup.PlayerStagingWorldX, 0f, setup.PlayerStagingWorldZ);
                    return world.x != 0f || world.z != 0f;
                }

                world = factionId == 2
                    ? new Vector3(setup.EnemyBaseWorldX, 0f, setup.EnemyBaseWorldZ)
                    : new Vector3(setup.PlayerBaseWorldX, 0f, setup.PlayerBaseWorldZ);
                return world.x != 0f || world.z != 0f;
            }

            if (TryFindForce(setup, factionId, role, out SkirmishResolvedForceEntry force) &&
                (force.SpawnWorldX != 0f || force.SpawnWorldZ != 0f))
            {
                world = OffsetOnPad(force.SpawnWorldX, force.SpawnWorldZ, factionId, memberIndex);
                return true;
            }

            float padX = factionId == 2 ? setup.EnemySpawnPadX : setup.PlayerSpawnPadX;
            float padZ = factionId == 2 ? setup.EnemySpawnPadZ : setup.PlayerSpawnPadZ;
            if (padX == 0f && padZ == 0f)
                return false;
            world = OffsetOnPad(padX, padZ, factionId, memberIndex);
            return true;
        }
public static Vector3 OffsetOnPad(float centerX, float centerZ, byte factionId, int memberIndex)
        {
            int column = memberIndex % 4;
            int row = memberIndex / 4;
            float side = factionId == 2 ? -1f : 1f;
            return new Vector3(centerX + column * 2.2f * side, 0f, centerZ - row * 2.2f);
        }
public static Vector3 ResolveUnitFallbackWorld(byte factionId, int memberIndex)
        {
            Vector3 pad = SkirmishVisualSpawnService.StagingWorld(factionId, true);
            int column = memberIndex % 4;
            int row = memberIndex / 4;
            float side = factionId == 2 ? -1f : 1f;
            return pad + new Vector3(column * 2.2f * side, 0f, -row * 2.2f);
        }
private static bool TryFindStructure(
            SkirmishResolvedSetup setup,
            byte factionId,
            string structureId,
            out SkirmishResolvedStructureEntry structure)
        {
            structure = default;
            if (setup.Structures == null)
                return false;
            for (int i = 0; i < setup.Structures.Length; i++)
            {
                if (setup.Structures[i].FactionId == factionId &&
                    setup.Structures[i].StructureId == structureId)
                {
                    structure = setup.Structures[i];
                    return true;
                }
            }

            return false;
        }
private static bool TryFindForce(
            SkirmishResolvedSetup setup,
            byte factionId,
            SkirmishRoleKind role,
            out SkirmishResolvedForceEntry force)
        {
            force = default;
            if (setup.Forces == null)
                return false;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                if (setup.Forces[i].FactionId == factionId && setup.Forces[i].RoleKind == role)
                {
                    force = setup.Forces[i];
                    return true;
                }
            }

            return false;
        }
}}
