using Game.Operations.Loop;
using Game.Operations.Tactical;

namespace Game.Operations.Content
{
    /// <summary>
    /// O001 teach coach: Scan → Evidence → Extract.
    /// Soft-highlight may gate input briefly; never blocks player input longer than
    /// <see cref="MaxInputBlockSeconds"/>. Presentation shell (P2) binds this frame;
    /// this type owns timing/content only.
    /// </summary>
    public enum OperationsCoachStepKind : byte
    {
        None = 0,
        Scan = 1,
        Evidence = 2,
        Extract = 3,
        Done = 4
    }

    public readonly struct OperationsCoachFrame
    {
        public OperationsCoachFrame(
            OperationsCoachStepKind step,
            int stepIndex,
            int stepCount,
            string titleKey,
            string bodyKey,
            bool inputBlocked,
            float inputBlockRemainingSeconds,
            string focusNodeId)
        {
            Step = step;
            StepIndex = stepIndex;
            StepCount = stepCount;
            TitleKey = titleKey ?? string.Empty;
            BodyKey = bodyKey ?? string.Empty;
            InputBlocked = inputBlocked;
            InputBlockRemainingSeconds = inputBlockRemainingSeconds;
            FocusNodeId = focusNodeId ?? string.Empty;
        }

        public OperationsCoachStepKind Step { get; }
        public int StepIndex { get; }
        public int StepCount { get; }
        public string TitleKey { get; }
        public string BodyKey { get; }
        public bool InputBlocked { get; }
        public float InputBlockRemainingSeconds { get; }
        public string FocusNodeId { get; }
    }

    public sealed class OperationsOnboardingCoach
    {
        public const string MissionId = "operation.o001";
        public const float MaxInputBlockSeconds = 3f;
        public const int StepCount = 3;

        float _blockElapsed;
        bool _blocking;
        OperationsCoachStepKind _lastHighlighted = OperationsCoachStepKind.None;

        public void Reset()
        {
            _blockElapsed = 0f;
            _blocking = false;
            _lastHighlighted = OperationsCoachStepKind.None;
        }

        /// <summary>
        /// Advances soft-block timer. Call each frame/tick with real elapsed seconds.
        /// Guarantees unblock at or before <see cref="MaxInputBlockSeconds"/>.
        /// </summary>
        public void AdvanceSeconds(float deltaSeconds)
        {
            if (!_blocking)
                return;
            if (deltaSeconds < 0f)
                deltaSeconds = 0f;
            _blockElapsed += deltaSeconds;
            if (_blockElapsed >= MaxInputBlockSeconds)
                ClearBlock();
        }

        /// <summary>Player tap/gesture clears any soft highlight gate immediately.</summary>
        public void NotifyPlayerInput() => ClearBlock();

        public bool IsInputBlocked => _blocking;

        public float InputBlockRemainingSeconds
        {
            get
            {
                if (!_blocking)
                    return 0f;
                float remaining = MaxInputBlockSeconds - _blockElapsed;
                return remaining > 0f ? remaining : 0f;
            }
        }

        public bool TryRead(OperationsLoopSession loop, out OperationsCoachFrame frame)
        {
            frame = default;
            if (loop == null || !loop.HasMission || loop.MissionId != MissionId)
                return false;
            if (loop.MissionTerminal || loop.Phase != OperationsLoopPhase.Active)
            {
                ClearBlock();
                frame = new OperationsCoachFrame(
                    OperationsCoachStepKind.Done,
                    StepCount,
                    StepCount,
                    "operations.coach.o001.done.title",
                    "operations.coach.o001.done.body",
                    false,
                    0f,
                    string.Empty);
                return true;
            }

            OperationsCoachStepKind step = ResolveStep(loop, out string focusNodeId);
            if (step == OperationsCoachStepKind.None)
                return false;

            if (step != _lastHighlighted)
            {
                _lastHighlighted = step;
                BeginBlock();
            }

            int index = StepIndexOf(step);
            frame = new OperationsCoachFrame(
                step,
                index,
                StepCount,
                TitleKey(step),
                BodyKey(step),
                _blocking,
                InputBlockRemainingSeconds,
                focusNodeId);
            return true;
        }

        static OperationsCoachStepKind ResolveStep(OperationsLoopSession loop, out string focusNodeId)
        {
            focusNodeId = string.Empty;
            if (loop.TryNode("scan_signals", out OperationsTacticalNodeState scan) &&
                scan.Phase == OperationsTacticalNodePhase.Active)
            {
                focusNodeId = "scan_signals";
                return OperationsCoachStepKind.Scan;
            }

            if (loop.TryNode("interact_relay", out OperationsTacticalNodeState interact) &&
                interact.Phase == OperationsTacticalNodePhase.Active)
            {
                focusNodeId = "interact_relay";
                return OperationsCoachStepKind.Evidence;
            }

            if (loop.TryNode("extract_force", out OperationsTacticalNodeState extract) &&
                extract.Phase == OperationsTacticalNodePhase.Active)
            {
                focusNodeId = "extract_force";
                return OperationsCoachStepKind.Extract;
            }

            if (NodeComplete(loop, "scan_signals") &&
                NodeComplete(loop, "interact_relay") &&
                NodeComplete(loop, "extract_force"))
                return OperationsCoachStepKind.Done;

            // Between activations: prefer next incomplete teach step.
            if (!NodeComplete(loop, "scan_signals"))
            {
                focusNodeId = "scan_signals";
                return OperationsCoachStepKind.Scan;
            }

            if (!NodeComplete(loop, "interact_relay"))
            {
                focusNodeId = "interact_relay";
                return OperationsCoachStepKind.Evidence;
            }

            if (!NodeComplete(loop, "extract_force"))
            {
                focusNodeId = "extract_force";
                return OperationsCoachStepKind.Extract;
            }

            return OperationsCoachStepKind.Done;
        }

        static bool NodeComplete(OperationsLoopSession loop, string nodeId) =>
            loop.TryNode(nodeId, out OperationsTacticalNodeState state) &&
            state.Phase == OperationsTacticalNodePhase.Complete;

        static int StepIndexOf(OperationsCoachStepKind step)
        {
            switch (step)
            {
                case OperationsCoachStepKind.Scan: return 1;
                case OperationsCoachStepKind.Evidence: return 2;
                case OperationsCoachStepKind.Extract: return 3;
                default: return StepCount;
            }
        }

        static string TitleKey(OperationsCoachStepKind step)
        {
            switch (step)
            {
                case OperationsCoachStepKind.Scan: return "operations.coach.o001.scan.title";
                case OperationsCoachStepKind.Evidence: return "operations.coach.o001.evidence.title";
                case OperationsCoachStepKind.Extract: return "operations.coach.o001.extract.title";
                default: return "operations.coach.o001.done.title";
            }
        }

        static string BodyKey(OperationsCoachStepKind step)
        {
            switch (step)
            {
                case OperationsCoachStepKind.Scan: return "operations.coach.o001.scan.body";
                case OperationsCoachStepKind.Evidence: return "operations.coach.o001.evidence.body";
                case OperationsCoachStepKind.Extract: return "operations.coach.o001.extract.body";
                default: return "operations.coach.o001.done.body";
            }
        }

        void BeginBlock()
        {
            _blocking = true;
            _blockElapsed = 0f;
        }

        void ClearBlock()
        {
            _blocking = false;
            _blockElapsed = 0f;
        }
    }
}
