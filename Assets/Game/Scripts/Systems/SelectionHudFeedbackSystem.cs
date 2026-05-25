using Unity.Collections;
using Unity.Entities;

public sealed class SelectionHudFeedbackSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);

    public readonly struct Context
    {
        public readonly SelectionUiQuerySystem SelectionUiQuerySystem;
        public readonly TryGetEntityManagerDelegate TryGetDefaultEntityManager;

        public Context(
            SelectionUiQuerySystem selectionUiQuerySystem,
            TryGetEntityManagerDelegate tryGetDefaultEntityManager)
        {
            SelectionUiQuerySystem = selectionUiQuerySystem;
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
        }
    }

    private BattleHudGameplayBridge _battleHudBridge;
    private World _queryWorld;
    private EntityQuery _feedbackQuery;

    public void ResetBridgeCache()
    {
        _battleHudBridge = null;
    }

    public Entity EnsureFeedbackQueue(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld != world || world == null || !world.IsCreated)
        {
            _queryWorld = world;
            _feedbackQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectionHudFeedbackQueueComponent>(),
                ComponentType.ReadWrite<SelectionHudFeedbackElement>());
        }

        if (!_feedbackQuery.IsEmptyIgnoreFilter)
            return _feedbackQuery.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(SelectionHudFeedbackQueueComponent));
        em.SetName(entity, "SelectionHudFeedbackQueue");
        em.AddBuffer<SelectionHudFeedbackElement>(entity);
        return entity;
    }

    public void QueueSelection(EntityManager em, Entity entity, SelectionUiQuerySystem selectionUiQuerySystem)
    {
        if (entity == Entity.Null || !em.Exists(entity))
        {
            QueueClearSelection(em);
            return;
        }

        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.Selection,
            Label = ToFixed64(selectionUiQuerySystem.ResolveFocusedUnitName(em, entity)),
            Status = ToFixed64(selectionUiQuerySystem.ResolveHudSelectionStatus(em, entity))
        });
    }

    public void QueueSquadSelection(EntityManager em, int selectedCount)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        if (selectedCount <= 0)
        {
            feedback.Add(new SelectionHudFeedbackElement { Kind = SelectionHudFeedbackKind.ClearSelection });
            return;
        }

        string unitLabel = selectedCount == 1 ? "UNIT" : "UNITS";
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.SquadSelection,
            Label = ToFixed64($"{selectedCount} {unitLabel}"),
            Status = ToFixed64("SQUAD SELECTED"),
            Count = selectedCount
        });
    }

    public void QueueClearSelection(EntityManager em)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement { Kind = SelectionHudFeedbackKind.ClearSelection });
    }

    public void QueueCommandMode(EntityManager em, TacticalCommandMode mode)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.CommandMode,
            CommandMode = (int)mode
        });
    }

    public void QueueClearCommandMode(EntityManager em)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement { Kind = SelectionHudFeedbackKind.ClearCommandMode });
    }

    public void QueueCommandResult(EntityManager em, TacticalCommandResult result)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.CommandResult,
            CommandAccepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)result.ReasonCode
        });
    }

    public void QueueWorldMarkersVisible(EntityManager em, bool visible)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.WorldMarkersVisible,
            Visible = visible ? (byte)1 : (byte)0
        });
    }

    public void ProcessPendingFeedback(EntityManager em)
    {
        Entity entity = EnsureFeedbackQueue(em);
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(entity);
        if (feedback.Length == 0)
            return;

        BattleHudGameplayBridge bridge = ResolveBattleHudBridge();
        if (bridge == null)
        {
            feedback.Clear();
            return;
        }

        for (int i = 0; i < feedback.Length; i++)
            ApplyFeedback(bridge, feedback[i]);
        feedback.Clear();
    }

    public void ApplySelection(EntityManager em, Entity entity, SelectionUiQuerySystem selectionUiQuerySystem)
    {
        QueueSelection(em, entity, selectionUiQuerySystem);
        ProcessPendingFeedback(em);
    }

    public void ApplySelection(Context context, EntityManager em, Entity entity)
    {
        ApplySelection(em, entity, context.SelectionUiQuerySystem);
    }

    public void ApplySquadSelection(EntityManager em, int selectedCount)
    {
        QueueSquadSelection(em, selectedCount);
        ProcessPendingFeedback(em);
    }

    public void ApplySquadSelection(Context context, int selectedCount)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ApplySquadSelection(em, selectedCount);
    }

    public void ClearSelection(EntityManager em)
    {
        QueueClearSelection(em);
        ProcessPendingFeedback(em);
    }

    public void ClearSelection(Context context)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ClearSelection(em);
    }

    public void ApplyCommandMode(EntityManager em, TacticalCommandMode mode)
    {
        QueueCommandMode(em, mode);
        ProcessPendingFeedback(em);
    }

    public void ApplyCommandMode(Context context, TacticalCommandMode mode)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ApplyCommandMode(em, mode);
    }

    public void ClearCommandMode(EntityManager em)
    {
        QueueClearCommandMode(em);
        ProcessPendingFeedback(em);
    }

    public void ClearCommandMode(Context context)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ClearCommandMode(em);
    }

    public void ApplyCommandResult(EntityManager em, TacticalCommandResult result)
    {
        QueueCommandResult(em, result);
        ProcessPendingFeedback(em);
    }

    public void ApplyCommandResult(Context context, TacticalCommandResult result)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ApplyCommandResult(em, result);
    }

    public void SetWorldMarkersVisible(EntityManager em, bool visible)
    {
        QueueWorldMarkersVisible(em, visible);
        ProcessPendingFeedback(em);
    }

    public void SetWorldMarkersVisible(Context context, bool visible)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        SetWorldMarkersVisible(em, visible);
    }

    private static bool TryGetDefaultEntityManager(Context context, out EntityManager em)
    {
        em = default;
        return context.TryGetDefaultEntityManager != null &&
               context.TryGetDefaultEntityManager(out em);
    }

    private static void ApplyFeedback(BattleHudGameplayBridge bridge, SelectionHudFeedbackElement feedback)
    {
        switch (feedback.Kind)
        {
            case SelectionHudFeedbackKind.Selection:
            case SelectionHudFeedbackKind.SquadSelection:
                bridge.ApplySelection(feedback.Label.ToString(), feedback.Status.ToString());
                break;
            case SelectionHudFeedbackKind.ClearSelection:
                bridge.ClearSelection();
                break;
            case SelectionHudFeedbackKind.CommandMode:
                bridge.ApplyCommandMode((TacticalCommandMode)feedback.CommandMode);
                break;
            case SelectionHudFeedbackKind.ClearCommandMode:
                bridge.ClearCommandMode();
                break;
            case SelectionHudFeedbackKind.CommandResult:
                bridge.ApplyCommandResult(feedback.CommandAccepted != 0
                    ? TacticalCommandResult.Success()
                    : TacticalCommandResult.Rejected((TacticalCommandReasonCode)feedback.ReasonCode));
                break;
            case SelectionHudFeedbackKind.WorldMarkersVisible:
                bridge.SetWorldMarkersVisible(feedback.Visible != 0);
                break;
        }
    }

    private static FixedString64Bytes ToFixed64(string value)
    {
        FixedString64Bytes result = default;
        if (string.IsNullOrEmpty(value))
            return result;
        result.Append(value.Length <= 61 ? value : value.Substring(0, 61));
        return result;
    }

    private BattleHudGameplayBridge ResolveBattleHudBridge()
    {
        if (_battleHudBridge != null)
            return _battleHudBridge;

        _battleHudBridge = BattleHudGameplayBridge.ResolveActive();
        return _battleHudBridge;
    }
}
