using System;namespace Game.Runtime {internal sealed class SourceHostPlacementSession {internal struct Context {internal SourceHostPlacementLifecycle LifecycleSystem;internal Func<SourceHostPlacementLifecycle.ConfirmContext> CreateConfirmContext;internal Action RecordBuildingBuilt,NotifyStaticMinimapChanged;internal Action<bool> ExitBuildMode;}private bool _preserveBuildingSelectionOnNextExitBuildMode;private void ExitBuildMode(Context context,bool clearBuildingSelection){if(context.ExitBuildMode==null)throw new NotSupportedException("Actual native exit-build-mode owner required");context.ExitBuildMode(clearBuildingSelection);}
        public bool ConfirmBuildingPlacement(
            Context context,
            int transactionId,
            out SourceHostPlacementLifecycle.ConfirmFailureReason failureReason)
        {
            failureReason = SourceHostPlacementLifecycle.ConfirmFailureReason.MissingActivePlacement;
            if (context.LifecycleSystem == null)
                return false;

            bool confirmed = transactionId > 0
                ? context.LifecycleSystem.Confirm(transactionId, context.CreateConfirmContext(), out failureReason)
                : context.LifecycleSystem.Confirm(context.CreateConfirmContext(), out failureReason);
            if (!confirmed)
                return false;

            context.RecordBuildingBuilt?.Invoke();
            context.NotifyStaticMinimapChanged?.Invoke();
            _preserveBuildingSelectionOnNextExitBuildMode = true;
            ExitBuildMode(context, clearBuildingSelection: false);
            return true;
        }
}}