#!/usr/bin/env python3
"""Focused wiring controls for actual S002 ground-production delivery events."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
QUEUE = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/queue-bridge.cs.template"
QUEUE_PREP = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/prepare_queue.py"
PROGRAM = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs"
HELICOPTER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/helicopter-bridge.cs.template"
PRODUCTION = ROOT / "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs"
PAID_ARIA = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/476_SourceAriaPaidRecruitment.cs"
ARIA_TRACE = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.s003.cs"


class DeliveryEventTelemetryControls(unittest.TestCase):
    def test_ground_spawn_records_success_and_failure_from_owner_result(self):
        source = QUEUE.read_text()
        self.assertIn("bool spawned=owner.World.TrySpawn(b,p,null,null,ref random,out var member)", source)
        self.assertIn('observed?"unit_spawned_and_placed":"unit_spawned_without_placement_readback"', source)
        self.assertIn('RecordDeliveryEvent("unit_spawn_failed",owner.World.Now', source)
        self.assertIn("TryReadSpawnPlacement(EntityManager em,Entity member,out int2 cell,out float3 position)", source)
        self.assertIn("!em.Exists(member)||!em.HasComponent<UnitGrid>(member)||!em.HasComponent<Unity.Transforms.LocalTransform>(member)", source)
        self.assertIn("cell=default;position=default", source)
        self.assertNotIn("Successful ground spawn lacks actual placement readback", source)
        spawn = source[source.index("public bool Spawn(RuntimeBuildingEntity b,SourcePendingProduction p,ref uint random)"):]
        self.assertLess(spawn.index("owner.DeliveredMembers++"), spawn.index("TryReadSpawnPlacement"))

    def test_unobserved_queue_accept_has_no_fabricated_placement(self):
        source = QUEUE_PREP.read_text()
        self.assertIn('RecordDeliveryEvent("queue_accepted",now,building.Id,productionIndex,spawnUnitPrefab.name,new int2(-1,-1),default,false,Entity.Null,false,pending.ReceiptId)', source)
        template = QUEUE.read_text()
        self.assertIn('["placement_observed"]=placementObserved', template)
        self.assertIn('["position_x"]=placementObserved?(float?)position.x:null', template)
        self.assertIn('["receipt_id"]=receiptId>0?(uint?)receiptId:null', template)
        self.assertIn('Entity.Null,false,p?.ReceiptId??0', template)
        self.assertIn('member,observed,p.ReceiptId)', template)

    def test_actual_delivery_events_are_serialized_from_the_owner(self):
        source = PROGRAM.read_text()
        self.assertIn('["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents)', source)
        self.assertIn('["actual_delivered_members"]=world.ActualDeliveryOwner.DeliveredMembers', source)

    def test_actual_aria_paid_receipt_id_flows_to_queue_and_spawn_events(self):
        aria = PAID_ARIA.read_text()
        self.assertIn("result.Pending?.ReceiptId", aria)
        trace = ARIA_TRACE.read_text()
        self.assertIn('["source_receipt_id"]=paidResult.ReceiptId', trace)
        queue = QUEUE_PREP.read_text()
        self.assertIn('Entity.Null,false,pending.ReceiptId)', queue)
        delivery = QUEUE.read_text()
        self.assertIn('member,observed,p.ReceiptId)', delivery)
        self.assertIn('Entity.Null,false,p?.ReceiptId??0', delivery)

    def test_released_pending_identity_is_fail_soft_at_helicopter_transitions(self):
        production = PRODUCTION.read_text()
        release = production[production.index("internal void ReleasePendingProduction("):]
        release = release[:release.index("\n        }")]
        self.assertIn("pending.Prefab = null;", release)

        helicopter = HELICOPTER.read_text()
        self.assertIn("void RecordTransition(Active a,float now,string kind)", helicopter)
        self.assertIn("pending?.ProductionIndex??-1", helicopter)
        self.assertIn("pending?.Prefab?.name??string.Empty", helicopter)
        self.assertIn("a.Building?.Id??-1", helicopter)
        arrival = helicopter[helicopter.index("public void Arrival("):]
        arrival = arrival[:arrival.index("public void Delivery(")]
        departure = helicopter[helicopter.index("public void Departure("):]
        departure = departure[:departure.index("public void Publish(")]
        self.assertIn('RecordTransition(a,now,"helicopter_arrived")', arrival)
        self.assertIn('RecordTransition(a,now,"helicopter_departed");owner.Return(a);', departure)


if __name__ == "__main__":
    unittest.main()
