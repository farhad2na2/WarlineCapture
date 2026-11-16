using System;
using Game.Runtime.TransportPose;
using Game.Simulation;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal interface ISourceAirSelfPose
    {
        float3 RootPosition { get; }
        float3 Forward { get; }
        void SetRoot(float3 position, quaternion rotation);
        void Spin(float delta);
    }

    // Opt-in composition of the original non-plane arrival and AirSelf landing
    // phases. A caller must own the real transport, fallback and publication;
    // this projection neither launches nor admits a production request.
    internal sealed class SourceAirSelfFlightProjection
    {
        internal readonly RuntimeBuildingEntity Building;
        internal readonly SourceUnitPrefab Prefab;
        internal readonly ISourceAirSelfPose Pose;
        internal readonly ISourceAirSelfCompletionView Completion;
        internal delegate void Fallback(float now, ref uint random);
        readonly Fallback fallback;
        readonly Action<float> departure;
        readonly Action publish;
        internal readonly float3 Entry, Hover, Touchdown;
        internal readonly quaternion EntryRotation, HoverRotation;
        internal readonly float ArrivalSeconds;
        internal float PhaseStartedAt, HoverEnteredAt, NextDropReadyAt;
        internal int Phase;
        internal bool Present = true;

        internal SourceAirSelfFlightProjection(RuntimeBuildingEntity building, SourceUnitPrefab prefab, ISourceAirSelfPose pose,
            ISourceAirSelfCompletionView completion, float3 entry, float3 hover, float3 touchdown,
            quaternion entryRotation, quaternion hoverRotation, float arrivalSeconds, float startedAt,
            Fallback unsupportedFallback, Action<float> actualDeparture, Action publishActualReadModel)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            Pose = pose ?? throw new ArgumentNullException(nameof(pose));
            Completion = completion ?? throw new ArgumentNullException(nameof(completion));
            fallback = unsupportedFallback ?? throw new ArgumentNullException(nameof(unsupportedFallback));
            departure = actualDeparture ?? throw new ArgumentNullException(nameof(actualDeparture));
            publish = publishActualReadModel ?? throw new ArgumentNullException(nameof(publishActualReadModel));
            PoseMath.Finite(entry); PoseMath.Finite(hover); PoseMath.Finite(touchdown);
            PoseMath.Unit(entryRotation); PoseMath.Unit(hoverRotation); PoseMath.Finite(arrivalSeconds); PoseMath.Finite(startedAt);
            Entry = entry; Hover = hover; Touchdown = touchdown; EntryRotation = entryRotation; HoverRotation = hoverRotation;
            ArrivalSeconds = math.max(.5f, arrivalSeconds); PhaseStartedAt = startedAt; HoverEnteredAt = -1f; NextDropReadyAt = startedAt;
        }

        internal void Tick(float now, float delta, ref uint random)
        {
            PoseMath.Finite(now); PoseMath.Finite(delta); if (delta < 0f || delta > 1f) throw new ArgumentOutOfRangeException(nameof(delta));
            var view = new View(this);
            SharedProductionTransportDispatchKernel.Step<SourceAirSelfFlightProjection, View>(ref view, this, now, delta, ref random);
        }

        readonly struct View : ISharedProductionTransportView<SourceAirSelfFlightProjection>
        {
            readonly SourceAirSelfFlightProjection flight;
            internal View(SourceAirSelfFlightProjection flight) { this.flight = flight; }
            public SharedProductionTransportRead Read(SourceAirSelfFlightProjection f) => new SharedProductionTransportRead(f.Present, true, 2, f.Phase);
            public void Spin(SourceAirSelfFlightProjection f, float delta) => f.Pose.Spin(delta);
            public void Arrival(SourceAirSelfFlightProjection f, float now)
            {
                float t = math.clamp((now - f.PhaseStartedAt) / math.max(.5f, f.ArrivalSeconds), 0f, 1f);
                f.Pose.SetRoot(PoseMath.Lerp(f.Entry, f.Hover, t), PoseMath.Slerp(f.EntryRotation, f.HoverRotation, t));
                if (t < 1f) return;
                f.Phase = 1; f.PhaseStartedAt = now; f.HoverEnteredAt = now; f.NextDropReadyAt = now;
            }
            public void Delivery(SourceAirSelfFlightProjection f, float now, ref uint random)
            {
                float landing = math.clamp((now - f.PhaseStartedAt) / 1.5f, 0f, 1f);
                f.Pose.SetRoot(PoseMath.Lerp(f.Hover, f.Touchdown, landing), f.HoverRotation);
                if (landing < 1f) return;
                if (SourceAirSelfCompletionProjection.Complete(f.Completion, f.Building, f.Prefab, f.Touchdown, f.Pose.Forward, now, ref random))
                { f.Present = false; return; }
                // The native owner continues to its ordinary drop/departure branch.
                // A source caller must provide that owner; no result is invented.
                f.fallback(now, ref random);
            }
            public void Departure(SourceAirSelfFlightProjection f, float now) => f.departure(now);
            public void Publish() => flight.publish();
        }
    }
}
