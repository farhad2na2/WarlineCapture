#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Diagnostics;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Opt-in proof and workload measurement for the actual AriaSkirmishPlanSystem.OnUpdate.
    /// The one-row fixture is synthetic and does not establish mission readiness or player behavior.
    /// </summary>
    public static class AriaSkirmishPlanBurstOnUpdateValidation
    {
        private const int WarmupUpdates = 16;
        private const int MeasuredUpdates = 120;

        [Serializable]
        private sealed class Receipt
        {
            public string method = "AriaSkirmishPlanSystem.OnUpdate via SystemHandle.Update(WorldUnmanaged)";
            public string scope = "EditorOnlySyntheticOneRowSourceS002FinishedAndActiveObservations";
            public string limitation = "No native mission, gameplay readiness, or player behavior claim; no performance regression claim without a pre-change baseline.";
            public string unityVersion;
            public string burstCompiler = "Unity.Burst.BurstCompiler";
            public bool burstEnabled;
            public bool burstDiscardManagedFallbackWitnessZero;
            public bool outputChanged;
            public int queryRows = 1;
            public int warmupUpdates;
            public int warmupLimit;
            public int measuredUpdates;
            public int measuredFrame;
            public long elapsedTicks;
            public long tickFrequency;
            public long allocatedBytes;
            public ActiveReceipt activeNonFinished;
        }

        [Serializable]
        private sealed class ActiveReceipt
        {
            public string scope = "EditorOnlySyntheticOneRowSourceS002ActiveObservation";
            public bool burstDiscardManagedFallbackWitnessZero;
            public bool outputChanged;
            public string outputKind;
            public int warmupUpdates;
            public int warmupLimit;
            public int measuredUpdates;
            public int measuredFrame;
            public long elapsedTicks;
            public long tickFrequency;
            public long allocatedBytes;
        }

        public static void RunFocusedValidation()
        {
            try
            {
                Receipt receipt = RunWorkload();
                UnityEngine.Debug.Log("[AriaSkirmishPlanBurstOnUpdate] result=Passed receipt=" + JsonUtility.ToJson(receipt));
                ValidationExit.Passed();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                UnityEngine.Debug.LogError("[AriaSkirmishPlanBurstOnUpdate] result=Failed burstExecutionProved=0");
                ValidationExit.Failed();
            }
        }

        private static Receipt RunWorkload()
        {
            Require(BurstCompiler.IsEnabled, "Burst compiler is disabled; no execution credit.");
            using var world = new World("AriaSkirmishPlanBurstOnUpdateValidation");
            EntityManager em = world.EntityManager;

            Entity boundary = em.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(AriaPlaySessionComponent),
                typeof(AriaPlayObservationComponent),
                typeof(UiShellStateComponent));
            em.SetComponentData(boundary, new UiShellStateComponent
            {
                ActiveRoute = UIRoute.Match,
                TransitionSequenceId = 7
            });

            Entity row = em.CreateEntity(
                typeof(AriaSkirmishObservationComponent),
                typeof(AriaSkirmishPlanComponent),
                typeof(AriaSkirmishObservationOwner));
            em.SetComponentData(row, new AriaSkirmishObservationComponent
            {
                Value = new AriaSkirmishObservation
                {
                    Active = true,
                    Finished = true,
                    SourceScenarioIndex = 4,
                    ExpandedSession = true,
                    ExpandedBaseAssault = true,
                    Frame = 1,
                    Time = 1f
                }
            });
            em.SetComponentData(row, new AriaSkirmishObservationOwner
            {
                Boundary = boundary,
                TransitionSequence = 7
            });

            SystemHandle system = world.GetOrCreateSystem<AriaSkirmishPlanSystem>();
            bool outputChanged = false;
            int burstProofWarmupAttempt = 0;
            int frame = 0;
            for (int i = 0; i < WarmupUpdates; i++)
            {
                frame++;
                UpdateSourceObservation(em, row, frame);
                AriaSkirmishPlanSystem.ResetManagedOnUpdateFallbackWitness();
                system.Update(world.Unmanaged);
                int managedFallbacks = AriaSkirmishPlanSystem.ManagedOnUpdateFallbackWitness;
                AriaPlayObservationComponent output = em.GetComponentData<AriaPlayObservationComponent>(boundary);
                if (managedFallbacks == 0 && output.Frame == frame && output.Kind == AriaPlayObservationKind.Finished)
                {
                    outputChanged = true;
                    burstProofWarmupAttempt = i + 1;
                    break;
                }
            }
            Require(outputChanged, "No matching OnUpdate query row completed with a zero BurstDiscard managed-fallback witness within the bounded warmup.");

            AriaSkirmishPlanSystem.ResetManagedOnUpdateFallbackWitness();
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < MeasuredUpdates; i++)
            {
                frame++;
                UpdateSourceObservation(em, row, frame);
                system.Update(world.Unmanaged);
            }
            long elapsed = Stopwatch.GetTimestamp() - start;
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Require(AriaSkirmishPlanSystem.ManagedOnUpdateFallbackWitness == 0,
                "OnUpdate reached managed execution during measured workload.");
            AriaPlayObservationComponent finalOutput = em.GetComponentData<AriaPlayObservationComponent>(boundary);
            Require(finalOutput.Frame == frame && finalOutput.Kind == AriaPlayObservationKind.Finished,
                "Measured updates did not execute the matching OnUpdate query row.");

            var receipt = new Receipt
            {
                unityVersion = Application.unityVersion,
                burstEnabled = BurstCompiler.IsEnabled,
                burstDiscardManagedFallbackWitnessZero = true,
                outputChanged = true,
                warmupUpdates = burstProofWarmupAttempt,
                warmupLimit = WarmupUpdates,
                measuredUpdates = MeasuredUpdates,
                measuredFrame = frame,
                elapsedTicks = elapsed,
                tickFrequency = Stopwatch.Frequency,
                allocatedBytes = allocatedBytes
            };
            receipt.activeNonFinished = RunActiveNonFinishedWorkload();
            return receipt;
        }

        private static ActiveReceipt RunActiveNonFinishedWorkload()
        {
            using var world = new World("AriaSkirmishPlanBurstOnUpdateActiveValidation");
            EntityManager em = world.EntityManager;

            Entity boundary = em.CreateEntity(
                typeof(UiShellRootComponent),
                typeof(AriaPlaySessionComponent),
                typeof(AriaPlayObservationComponent),
                typeof(UiShellStateComponent));
            em.SetComponentData(boundary, new UiShellStateComponent
            {
                ActiveRoute = UIRoute.Match,
                TransitionSequenceId = 17
            });
            // Finished is a sentinel here. The active workload must replace it
            // with the actual current-frame policy output.
            em.SetComponentData(boundary, new AriaPlayObservationComponent
            {
                Kind = AriaPlayObservationKind.Finished,
                Frame = -1,
                Time = -1f
            });

            Entity row = em.CreateEntity(
                typeof(AriaSkirmishObservationComponent),
                typeof(AriaSkirmishPlanComponent),
                typeof(AriaSkirmishObservationOwner));
            em.SetComponentData(row, new AriaSkirmishObservationComponent
            {
                Value = new AriaSkirmishObservation
                {
                    Active = true,
                    Finished = false,
                    SourceScenarioIndex = 4,
                    ExpandedSession = true,
                    ExpandedBaseAssault = true,
                    Frame = 1,
                    Time = 1f / 60f
                }
            });
            em.SetComponentData(row, new AriaSkirmishObservationOwner
            {
                Boundary = boundary,
                TransitionSequence = 17
            });

            SystemHandle system = world.GetOrCreateSystem<AriaSkirmishPlanSystem>();
            bool outputChanged = false;
            int burstProofWarmupAttempt = 0;
            int frame = 0;
            for (int i = 0; i < WarmupUpdates; i++)
            {
                frame++;
                UpdateSourceObservation(em, row, frame);
                AriaSkirmishPlanSystem.ResetManagedOnUpdateFallbackWitness();
                system.Update(world.Unmanaged);
                int managedFallbacks = AriaSkirmishPlanSystem.ManagedOnUpdateFallbackWitness;
                AriaPlayObservationComponent output = em.GetComponentData<AriaPlayObservationComponent>(boundary);
                if (managedFallbacks == 0 && output.Frame == frame &&
                    output.Time == frame / 60f && output.Kind != AriaPlayObservationKind.Finished)
                {
                    outputChanged = true;
                    burstProofWarmupAttempt = i + 1;
                    break;
                }
            }
            Require(outputChanged,
                "Active S002 OnUpdate did not mutate the boundary output with a current frame/time under the zero-fallback Burst witness.");

            AriaSkirmishPlanSystem.ResetManagedOnUpdateFallbackWitness();
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < MeasuredUpdates; i++)
            {
                frame++;
                UpdateSourceObservation(em, row, frame);
                system.Update(world.Unmanaged);
            }
            long elapsed = Stopwatch.GetTimestamp() - start;
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Require(AriaSkirmishPlanSystem.ManagedOnUpdateFallbackWitness == 0,
                "Active S002 OnUpdate reached managed execution during its measured workload.");
            AriaPlayObservationComponent finalOutput = em.GetComponentData<AriaPlayObservationComponent>(boundary);
            Require(finalOutput.Frame == frame && finalOutput.Time == frame / 60f &&
                    finalOutput.Kind != AriaPlayObservationKind.Finished,
                "Active S002 measured updates did not produce a current non-finished boundary output.");

            return new ActiveReceipt
            {
                burstDiscardManagedFallbackWitnessZero = true,
                outputChanged = true,
                outputKind = finalOutput.Kind.ToString(),
                warmupUpdates = burstProofWarmupAttempt,
                warmupLimit = WarmupUpdates,
                measuredUpdates = MeasuredUpdates,
                measuredFrame = frame,
                elapsedTicks = elapsed,
                tickFrequency = Stopwatch.Frequency,
                allocatedBytes = allocatedBytes
            };
        }

        private static void UpdateSourceObservation(EntityManager em, Entity row, int frame)
        {
            AriaSkirmishObservationComponent observation = em.GetComponentData<AriaSkirmishObservationComponent>(row);
            observation.Value.Frame = frame;
            observation.Value.Time = frame / 60f;
            em.SetComponentData(row, observation);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
#endif
