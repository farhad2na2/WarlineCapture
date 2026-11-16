using System;
namespace Game.Simulation
{
    // One logical diagnostic clock, driven once by each real complete PlayerLoop.
    public sealed class SkirmishValidationStepClock
    {
        private readonly double unscaledBase, realtimeBase;
        private readonly float step;
        private double elapsed;
        private int frame;
        public float Delta { get; private set; }
        public float UnscaledNow => (float)(unscaledBase + elapsed);
        public double RealtimeNow => realtimeBase + elapsed;
        public int Samples { get; private set; }
        public string Failure { get; private set; }
        public SkirmishValidationStepClock(int frame, double scaled, float unscaled, double realtime, float fixedStep)
        {
            if (frame < 0 || !Finite(scaled) || !Finite(unscaled) || !Finite(realtime) || !Finite(fixedStep) || fixedStep <= 0 || fixedStep > .1f)
                throw new ArgumentException("Finite real clock anchors and normal-sized step required");
            this.frame = frame; unscaledBase = unscaled; realtimeBase = realtime; step = fixedStep;
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public void Sample(int currentFrame, double scaled, float scaledDelta, float unscaledDelta, bool paused)
        {
            if (Failure != null || currentFrame == frame) return;
            if (currentFrame != frame + 1 || !Finite(scaled) || !Finite(scaledDelta) || !Finite(unscaledDelta) || scaledDelta < 0 || unscaledDelta < 0)
            { Failure = "nonconsecutiveOrInvalidPlayerLoopClock"; return; }
            if (!paused && BitConverter.SingleToInt32Bits(scaledDelta) != BitConverter.SingleToInt32Bits(step))
            { Failure = "nonNormalSizedSimulationStep"; return; }
            Delta = paused ? unscaledDelta : scaledDelta;
            elapsed += Delta; frame = currentFrame; Samples++;
        }
    }
}
