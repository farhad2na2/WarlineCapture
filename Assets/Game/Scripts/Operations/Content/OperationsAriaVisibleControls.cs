using System;
using Game.Operations.Loop;

namespace Game.Operations.Content
{
    public enum OperationsVisibleStepKind : byte
    {
        Advanced = 0,
        Victory = 1,
        Stuck = 2
    }

    /// <summary>
    /// Regular EN Aria plays by pressing the same shipping controls as a person.
    /// It does not call the capture script or the loop order methods itself.
    /// </summary>
    public static class OperationsAriaVisibleControls
    {
        public static OperationsVisibleStepKind Step(OperationsO001PlayerShell shell, out string detail)
        {
            detail = string.Empty;
            if (shell == null)
                throw new ArgumentNullException(nameof(shell));

            OperationsPlayerShellFrame frame = shell.Read();
            if (frame.O001Victory && frame.ReturnAcknowledged && frame.Phase == OperationsLoopPhase.Dashboard)
            {
                detail = "returned";
                return OperationsVisibleStepKind.Victory;
            }

            if (NeedsContinue(frame))
            {
                if (!OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Continue, string.Empty, string.Empty, string.Empty))
                {
                    detail = shell.Describe();
                    return OperationsVisibleStepKind.Stuck;
                }

                OperationsPlayerShellFrame after = shell.Read();
                if (after.O001Victory && after.ReturnAcknowledged && after.Phase == OperationsLoopPhase.Dashboard)
                {
                    detail = "settled";
                    return OperationsVisibleStepKind.Victory;
                }

                detail = shell.Describe();
                return OperationsVisibleStepKind.Stuck;
            }

            if (frame.Phase == OperationsLoopPhase.Dashboard && frame.Route == OperationsShellNames.Operations)
            {
                if (!OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.District, string.Empty, string.Empty, string.Empty))
                {
                    detail = shell.Describe();
                    return OperationsVisibleStepKind.Stuck;
                }

                detail = "library";
                return OperationsVisibleStepKind.Advanced;
            }

            if (frame.Route == OperationsShellNames.MissionBriefing)
            {
                if (!OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Raid, string.Empty, string.Empty, string.Empty))
                {
                    detail = shell.Describe();
                    return OperationsVisibleStepKind.Stuck;
                }

                detail = "deploy";
                return OperationsVisibleStepKind.Advanced;
            }

            OperationsAriaIntent[] plan = OperationsAriaObjectivePlanner.Plan(shell.Session);
            if (TryExtract(shell, plan, out detail))
                return OperationsVisibleStepKind.Advanced;

            if (!TryFirstAction(plan, out OperationsAriaIntent intent))
            {
                if (OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Wait, string.Empty, string.Empty, string.Empty) || shell.Read().Terminal)
                {
                    detail = "wait";
                    return OperationsVisibleStepKind.Advanced;
                }

                detail = shell.Describe();
                return OperationsVisibleStepKind.Stuck;
            }

            if (intent.ActorId.Length > 0 && shell.ChannelTicks(intent.ActorId) > 0)
            {
                if (!OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Wait, string.Empty, string.Empty, string.Empty))
                {
                    detail = shell.Describe();
                    return OperationsVisibleStepKind.Stuck;
                }

                detail = "channel";
                return OperationsVisibleStepKind.Advanced;
            }

            if (!PressIntent(shell, intent))
            {
                if (OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Wait, string.Empty, string.Empty, string.Empty) || shell.Read().Terminal)
                {
                    detail = "wait_after_reject";
                    return OperationsVisibleStepKind.Advanced;
                }

                detail = shell.Describe();
                return OperationsVisibleStepKind.Stuck;
            }

            if (!shell.Read().Terminal)
                OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Wait, string.Empty, string.Empty, string.Empty);
            detail = intent.Skill.ToString();
            return OperationsVisibleStepKind.Advanced;
        }

        static bool TryExtract(OperationsO001PlayerShell shell, OperationsAriaIntent[] plan, out string detail)
        {
            detail = string.Empty;
            bool any = false;
            for (int index = 0; index < plan.Length; index++)
            {
                if (plan[index].Skill != OperationsAriaSkillKind.Extract)
                    continue;
                any = true;
                PressIntent(shell, plan[index]);
            }

            if (!any)
                return false;
            if (!shell.Read().Terminal)
                OperationsMatchVisibleControls.Press(shell, OperationsMatchVisibleControls.Wait, string.Empty, string.Empty, string.Empty);
            detail = "extract";
            return true;
        }

        static bool PressIntent(OperationsO001PlayerShell shell, OperationsAriaIntent intent)
        {
            string shipping = OperationsMatchVisibleControls.ShippingName(intent.Skill);
            if (shipping.Length == 0)
                return false;
            string orderId = OperationsO001PlayerShell.OrderId(intent);
            if (!Contains(shell, orderId) && intent.ActorId.Length > 0)
            {
                if (!OperationsMatchVisibleControls.Press(
                        shell,
                        OperationsMatchVisibleControls.Select,
                        intent.ActorId,
                        string.Empty,
                        string.Empty))
                    return false;
            }

            return Contains(shell, orderId) &&
                OperationsMatchVisibleControls.Press(shell, shipping, intent.ActorId, intent.TargetId, intent.RouteId);
        }

        static bool Contains(OperationsO001PlayerShell shell, string controlId)
        {
            OperationsPlayerControl[] controls = shell.Read().Controls;
            for (int index = 0; index < controls.Length; index++)
            {
                if (controls[index].Enabled && controls[index].Id == controlId)
                    return true;
            }

            return false;
        }

        static bool TryFirstAction(OperationsAriaIntent[] plan, out OperationsAriaIntent intent)
        {
            for (int index = 0; index < plan.Length; index++)
            {
                if (plan[index].Skill == OperationsAriaSkillKind.Focus)
                    continue;
                intent = plan[index];
                return true;
            }

            intent = default;
            return false;
        }

        static bool NeedsContinue(OperationsPlayerShellFrame frame)
        {
            if (frame.Terminal && frame.Phase == OperationsLoopPhase.Active)
                return true;
            if (frame.Phase == OperationsLoopPhase.PendingResult)
                return true;
            return frame.Phase == OperationsLoopPhase.Settled && !frame.ReturnAcknowledged;
        }
    }
}
