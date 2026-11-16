using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal static partial class GeneratedReferenceTemplateImporter
    {
        internal static Result Import(EntityManager em, JObject source, string binaryDirectory,
            IEnumerable<string> requiredSourceKeys, string projectedRootKey, UnitMoveAuthoringProjection expectedMovement)
            => ImportCore(em, source, binaryDirectory, requiredSourceKeys, projectedRootKey, expectedMovement, null, null);

        internal static Result Import(EntityManager em, JObject source, string binaryDirectory,
            IEnumerable<string> requiredSourceKeys, string projectedRootKey, UnitMoveAuthoringProjection expectedMovement,
            UnitCombat expectedCombat, UnitHealth expectedHealth)
            => ImportCore(em, source, binaryDirectory, requiredSourceKeys, projectedRootKey, expectedMovement,
                expectedCombat, expectedHealth, null);

        internal static Result Import(EntityManager em, JObject source, string binaryDirectory,
            IEnumerable<string> requiredSourceKeys, string projectedRootKey, UnitMoveAuthoringProjection expectedMovement,
            UnitCombat expectedCombat, UnitHealth expectedHealth, UnitAttack expectedAttack)
            => ImportCore(em, source, binaryDirectory, requiredSourceKeys, projectedRootKey, expectedMovement,
                expectedCombat, expectedHealth, expectedAttack);

        private static Result ImportCore(EntityManager em, JObject source, string binaryDirectory,
            IEnumerable<string> requiredSourceKeys, string projectedRootKey, UnitMoveAuthoringProjection expectedMovement,
            UnitCombat? expectedCombat, UnitHealth? expectedHealth, UnitAttack? expectedAttack = null)
        {
            Need(em != null, "Actual template import manager required");
            List<Row> rows = DecodeAndValidate(source, binaryDirectory, requiredSourceKeys, out JObject roots, out int omitted);
            Need(expectedCombat.HasValue == expectedHealth.HasValue,
                "UnitCombat and UnitHealth source projection must be supplied together");
            Need(!expectedCombat.HasValue || !string.IsNullOrEmpty(projectedRootKey),
                "UnitCombat/UnitHealth projection requires an exact projected root key");
            Need(!expectedAttack.HasValue || !string.IsNullOrEmpty(projectedRootKey),
                "UnitAttack projection requires an exact projected root key");
            Need(!expectedAttack.HasValue || projectedRootKey == "Unit_Chr_Soldier_Male_02_Alt_04",
                "UnitAttack authoring projection is scoped to the exact Alt04 rifle root");
            if (projectedRootKey != null)
            {
                Need(expectedMovement.HasMovementSlice, "Complete shared UnitMove movement projection required");
                Need(roots[projectedRootKey] != null, "Projected UnitMove root is absent from the captured source domain");
                Entity sourceRoot = ExactInitialValueCodec.EntityId((string)roots[projectedRootKey]);
                Row rootRow = rows.FirstOrDefault(row => row.Source == sourceRoot);
                Cell moveCell = rootRow?.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitMove) && !cell.Buffer);
                Need(moveCell != null, "Projected UnitMove component is missing from captured source root");
                var footprintCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitFootprint) && !cell.Buffer);
                var behaviorCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitMovementBehavior) && !cell.Buffer);
                var vehicleCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitVehicleMovement) && !cell.Buffer);
                var kinematicsCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitVehicleKinematics) && !cell.Buffer);
                Need(footprintCell != null && behaviorCell != null && vehicleCell != null && kinematicsCell != null,
                    "Complete baked rifle movement components required");
                UnitMove captured = (UnitMove)moveCell.Value;
                UnitFootprint capturedFootprint = (UnitFootprint)footprintCell.Value;
                UnitMovementBehavior capturedBehavior = (UnitMovementBehavior)behaviorCell.Value;
                UnitVehicleMovement capturedVehicle = (UnitVehicleMovement)vehicleCell.Value;
                UnitVehicleKinematics capturedKinematics = (UnitVehicleKinematics)kinematicsCell.Value;
                Need(SameBits(captured.Speed, expectedMovement.Speed) &&
                    SameBits(captured.WalkSpeed, expectedMovement.WalkSpeed) &&
                    SameBits(captured.RoadSpeedMultiplier, expectedMovement.RoadSpeedMultiplier) &&
                    SameBits(captured.ArriveDistance, expectedMovement.ArriveDistance) &&
                    capturedFootprint.Size.Equals(expectedMovement.Footprint.Size) &&
                    capturedBehavior.AllowIdleWander == expectedMovement.MovementBehavior.AllowIdleWander &&
                    capturedBehavior.UsesVehicleMotion == expectedMovement.MovementBehavior.UsesVehicleMotion &&
                    SameBits(capturedVehicle.TurnSpeedDegrees, expectedMovement.VehicleMovement.TurnSpeedDegrees) &&
                    SameBits(capturedVehicle.Acceleration, expectedMovement.VehicleMovement.Acceleration) &&
                    SameBits(capturedVehicle.Braking, expectedMovement.VehicleMovement.Braking) &&
                    SameBits(capturedVehicle.RearPivotOffset, expectedMovement.VehicleMovement.RearPivotOffset) &&
                    SameBits(capturedKinematics.CurrentSpeed, expectedMovement.VehicleKinematics.CurrentSpeed) &&
                    SameBits(capturedKinematics.StallSeconds, expectedMovement.VehicleKinematics.StallSeconds),
                    "Captured baked rifle movement slice differs from pinned authoring projection for " + projectedRootKey);
                moveCell.Value = new UnitMove { Speed = expectedMovement.Speed, WalkSpeed = expectedMovement.WalkSpeed,
                    RoadSpeedMultiplier = expectedMovement.RoadSpeedMultiplier, ArriveDistance = expectedMovement.ArriveDistance };
                footprintCell.Value = expectedMovement.Footprint;
                behaviorCell.Value = expectedMovement.MovementBehavior;
                vehicleCell.Value = expectedMovement.VehicleMovement;
                kinematicsCell.Value = expectedMovement.VehicleKinematics;

                if (expectedCombat.HasValue)
                {
                    var combatCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitCombat) && !cell.Buffer);
                    var healthCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitHealth) && !cell.Buffer);
                    Need(combatCell != null && healthCell != null,
                        "Projected rifle root must contain UnitCombat and UnitHealth before import allocation");
                    UnitCombat capturedCombat = (UnitCombat)combatCell.Value;
                    UnitHealth capturedHealth = (UnitHealth)healthCell.Value;
                    UnitCombat expectedCombatValue = expectedCombat.Value;
                    UnitHealth expectedHealthValue = expectedHealth.Value;
                    Need(capturedCombat.AggroRangeCells == expectedCombatValue.AggroRangeCells &&
                        SameBits(capturedCombat.ChaseBreakDistance, expectedCombatValue.ChaseBreakDistance) &&
                        capturedCombat.CanAttack == expectedCombatValue.CanAttack &&
                        capturedCombat.AutoEngage == expectedCombatValue.AutoEngage &&
                        capturedHealth.Current == expectedHealthValue.Current &&
                        capturedHealth.Max == expectedHealthValue.Max,
                        "Captured rifle UnitCombat/UnitHealth fields differ from current pinned authoring projection for " + projectedRootKey);
                    combatCell.Value = expectedCombatValue;
                    healthCell.Value = expectedHealthValue;
                }

                if (expectedAttack.HasValue)
                {
                    var attackCell = rootRow.Cells.FirstOrDefault(cell => cell.Type == typeof(UnitAttack) && !cell.Buffer);
                    Need(attackCell != null, "Projected rifle root must contain UnitAttack before import allocation");
                    UnitAttack capturedAttack = (UnitAttack)attackCell.Value;
                    UnitAttack expectedAttackValue = expectedAttack.Value;
                    Need(SameBits(capturedAttack.Range, expectedAttackValue.Range) &&
                        SameBits(capturedAttack.CooldownSeconds, expectedAttackValue.CooldownSeconds) &&
                        capturedAttack.Damage == expectedAttackValue.Damage &&
                        SameBits(capturedAttack.TraceColor.x, expectedAttackValue.TraceColor.x) &&
                        SameBits(capturedAttack.TraceColor.y, expectedAttackValue.TraceColor.y) &&
                        SameBits(capturedAttack.TraceColor.z, expectedAttackValue.TraceColor.z) &&
                        SameBits(capturedAttack.TraceColor.w, expectedAttackValue.TraceColor.w) &&
                        SameBits(capturedAttack.TraceWidth, expectedAttackValue.TraceWidth) &&
                        SameBits(capturedAttack.TraceScrollSpeed, expectedAttackValue.TraceScrollSpeed) &&
                        SameBits(capturedAttack.TraceDashDensity, expectedAttackValue.TraceDashDensity) &&
                        SameBits(capturedAttack.TraceVisibleSeconds, expectedAttackValue.TraceVisibleSeconds) &&
                        capturedAttack.TracerEveryNthShot == expectedAttackValue.TracerEveryNthShot,
                        "Captured rifle UnitAttack fields differ from current pinned authoring projection for " + projectedRootKey);
                    attackCell.Value = expectedAttackValue;
                }
            }

            var result = new Result { Manager = em, OmittedPresentationComponents = omitted };
            try
            {
                foreach (Row row in rows) result.SourceToLogical.Add(row.Source, em.CreateEntity());
                foreach (Row row in rows)
                {
                    Entity owner = result.SourceToLogical[row.Source];
                    foreach (Cell cell in row.Cells)
                    {
                        if (cell.Buffer)
                        {
                            object buffer = typeof(EntityManager).GetMethod("AddBuffer").MakeGenericMethod(cell.Type)
                                .Invoke(em, new object[] { owner });
                            var add = buffer.GetType().GetMethod("Add");
                            foreach (object item in cell.Items) add.Invoke(buffer, new[] { Remap(item, cell.Type, result.SourceToLogical) });
                        }
                        else em.AddComponent(owner, cell.Type, Remap(cell.Value, cell.Type, result.SourceToLogical));
                        if (cell.Enabled.HasValue)
                            typeof(EntityManager).GetMethod("SetComponentEnabled").MakeGenericMethod(cell.Type)
                                .Invoke(em, new object[] { owner, cell.Enabled.Value });
                        result.ImportedComponents++;
                    }
                    if (!row.Enabled && !em.HasComponent<Disabled>(owner)) em.AddComponentData(owner, new Disabled());
                    result.ImportedEntities++;
                }
                foreach (JProperty root in roots.Properties())
                {
                    Entity entity = result.SourceToLogical[ExactInitialValueCodec.EntityId((string)root.Value)];
                    result.Prefabs.Add(root.Name, entity);
                    em.SetName(entity, root.Name);
                }
                return result;
            }
            catch
            {
                foreach (Entity entity in result.SourceToLogical.Values)
                    if (em.Exists(entity)) em.DestroyEntity(entity);
                throw;
            }
        }

        private static bool SameBits(float left, float right) =>
            BitConverter.ToInt32(BitConverter.GetBytes(left), 0) == BitConverter.ToInt32(BitConverter.GetBytes(right), 0);
    }
}
