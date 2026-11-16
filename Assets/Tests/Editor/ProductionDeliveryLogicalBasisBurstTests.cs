#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Game.Simulation;
using ProductionDeliveryLogicalBasis = Game.Simulation.SharedProductionDeliveryKernel.ProductionDeliveryLogicalBasis;

namespace Game.Tests.Editor
{
    /// <summary>Focused execution proof for the camera-independent delivery geometry kernel.</summary>
    public sealed class ProductionDeliveryLogicalBasisBurstTests
    {
        [BurstCompile(CompileSynchronously = true)]
        private struct LogicalDeliveryGeometryJob : IJob
        {
            public ProductionDeliveryLogicalBasis Basis;
            public NativeArray<float3> Results;
            public NativeArray<int> ManagedExecutionMarker;

            public void Execute()
            {
                int managed = 0;
                MarkManagedExecution(ref managed);
                ManagedExecutionMarker[0] = managed;
                Results[0] = SharedProductionDeliveryKernel.ProductionTransportLaneOffsetForLogicalBasis(Basis, 0, 2);
                Results[1] = SharedProductionDeliveryKernel.ProductionTransportLaneOffsetForLogicalBasis(Basis, 1, 2);
                Results[2] = SharedProductionDeliveryKernel.ProductionTransportIngressOffset(Basis, 10f);
            }

            [BurstDiscard]
            private static void MarkManagedExecution(ref int marker) => marker = 1;
        }

        [Test]
        public void LogicalLaneAndIngressKernelRunsInBurstAndMatchesFixedWorldFrame()
        {
            using var results = new NativeArray<float3>(3, Allocator.TempJob);
            using var managedMarker = new NativeArray<int>(1, Allocator.TempJob);
            var basis = ProductionDeliveryLogicalBasis.WorldAxes;
            var job = new LogicalDeliveryGeometryJob
            {
                Basis = basis,
                Results = results,
                ManagedExecutionMarker = managedMarker
            };

            // Compare the exact legacy fixed-vector (+Z) lane values before scheduling.
            var legacyLane0 = SharedProductionDeliveryKernel.ProductionTransportLaneOffset(true, Vector3.forward, 0, 2);
            var legacyLane1 = SharedProductionDeliveryKernel.ProductionTransportLaneOffset(true, Vector3.forward, 1, 2);
            Assert.AreEqual(new float3(legacyLane0.x, legacyLane0.y, legacyLane0.z),
                SharedProductionDeliveryKernel.ProductionTransportLaneOffsetForLogicalBasis(basis, 0, 2));
            Assert.AreEqual(new float3(legacyLane1.x, legacyLane1.y, legacyLane1.z),
                SharedProductionDeliveryKernel.ProductionTransportLaneOffsetForLogicalBasis(basis, 1, 2));

            job.Schedule().Complete();

            Assert.AreEqual(0, managedMarker[0], "The scheduled job must execute through Burst, not the managed fallback.");
            Assert.AreEqual(new float3(0f, 0f, -6f), results[0], "World +Z lane 0 retains centered 12-unit spacing.");
            Assert.AreEqual(new float3(0f, 0f, 6f), results[1], "World +Z lane 1 retains centered 12-unit spacing.");
            Assert.AreEqual(new float3(-10f, 0f, 0f), results[2], "Ingress remains opposite logical +X right.");
        }

        public static void RunFocusedValidation()
        {
            try
            {
                new ProductionDeliveryLogicalBasisBurstTests()
                    .LogicalLaneAndIngressKernelRunsInBurstAndMatchesFixedWorldFrame();
                Debug.Log("[ProductionDeliveryLogicalBasisBurst] result=Passed cases=3 legacyFixedVector=Matched burst=Executed missionReady=0");
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[ProductionDeliveryLogicalBasisBurst] result=Failed missionReady=0");
                ValidationExit.Failed();
            }
        }
    }
}
#endif
