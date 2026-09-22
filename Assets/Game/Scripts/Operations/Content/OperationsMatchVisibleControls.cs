using System;
using Game.Operations.Loop;

namespace Game.Operations.Content
{
    /// <summary>
    /// Names of the shipping controls that drive operation.o001.
    /// District and Raid are the Ops dashboard and district-detail buttons.
    /// Move, Attack, Scan, Hold, Board and Select are the match command buttons.
    /// Continue is the result action. Wait is the one-second match clock.
    /// </summary>
    public static class OperationsMatchVisibleControls
    {
        public const string District = "District";
        public const string Raid = "Raid";
        public const string Move = "Move";
        public const string Attack = "Attack";
        public const string Scan = "Scan";
        public const string Hold = "Hold";
        public const string Board = "Board";
        public const string Select = "Select";
        public const string Continue = "Continue";
        public const string Back = "Back";
        public const string Wait = "Wait";

        public static bool Press(
            OperationsO001PlayerShell shell,
            string shippingControl,
            string actorId,
            string targetId,
            string routeId)
        {
            if (shell == null)
                throw new ArgumentNullException(nameof(shell));
            actorId ??= string.Empty;
            targetId ??= string.Empty;
            routeId ??= string.Empty;
            switch (shippingControl)
            {
                case District:
                    return shell.Press(OperationsO001PlayerShell.LibraryId);
                case Raid:
                    return shell.Press(OperationsO001PlayerShell.DeployId);
                case Continue:
                    return shell.Press(OperationsO001PlayerShell.ContinueId);
                case Back:
                    return shell.Press(OperationsO001PlayerShell.BackId);
                case Wait:
                    return shell.Press(OperationsO001PlayerShell.WaitId);
                case Select:
                    return PressSelect(shell, actorId);
                case Move:
                    return PressSkill(shell, "Move", actorId, targetId, routeId);
                case Attack:
                    return PressSkill(shell, "Attack", actorId, targetId, routeId);
                case Scan:
                    return PressSkill(shell, "Scan", actorId, targetId, routeId) ||
                        PressSkill(shell, "Observe", actorId, targetId, routeId);
                case Hold:
                    return PressSkill(shell, "Interact", actorId, targetId, routeId) ||
                        PressSkill(shell, "Repair", actorId, targetId, routeId) ||
                        PressSkill(shell, "Hold", actorId, targetId, routeId);
                case Board:
                    return PressSkill(shell, "Extract", actorId, targetId, routeId);
                default:
                    return false;
            }
        }

        public static string ShippingName(OperationsAriaSkillKind skill)
        {
            switch (skill)
            {
                case OperationsAriaSkillKind.Move:
                    return Move;
                case OperationsAriaSkillKind.Attack:
                    return Attack;
                case OperationsAriaSkillKind.Scan:
                case OperationsAriaSkillKind.Observe:
                    return Scan;
                case OperationsAriaSkillKind.Interact:
                case OperationsAriaSkillKind.Repair:
                case OperationsAriaSkillKind.Hold:
                    return Hold;
                case OperationsAriaSkillKind.Extract:
                    return Board;
                default:
                    return string.Empty;
            }
        }

        static bool PressSelect(OperationsO001PlayerShell shell, string actorId)
        {
            if (actorId.Length > 0)
                return shell.Press(OperationsO001PlayerShell.SelectId(actorId));

            OperationsPlayerControl[] controls = shell.Read().Controls ?? Array.Empty<OperationsPlayerControl>();
            string current = shell.SelectedUnitId ?? string.Empty;
            string first = string.Empty;
            string next = string.Empty;
            bool passed = current.Length == 0;
            for (int index = 0; index < controls.Length; index++)
            {
                if (!controls[index].Enabled || !controls[index].Id.StartsWith("select|", StringComparison.Ordinal))
                    continue;
                string actor = controls[index].Id.Substring("select|".Length);
                if (first.Length == 0)
                    first = actor;
                if (passed && next.Length == 0)
                    next = actor;
                if (actor == current)
                    passed = true;
            }

            string chosen = next.Length > 0 ? next : first;
            return chosen.Length > 0 && shell.Press(OperationsO001PlayerShell.SelectId(chosen));
        }

        static bool PressSkill(
            OperationsO001PlayerShell shell,
            string skill,
            string actorId,
            string targetId,
            string routeId)
        {
            if (actorId.Length > 0)
            {
                var intent = new OperationsAriaIntent(ParseSkill(skill), actorId, targetId, routeId, string.Empty);
                return shell.Press(OperationsO001PlayerShell.OrderId(intent));
            }

            string prefix = "order|" + skill + "|";
            OperationsPlayerControl[] controls = shell.Read().Controls ?? Array.Empty<OperationsPlayerControl>();
            for (int index = 0; index < controls.Length; index++)
            {
                if (controls[index].Enabled && controls[index].Id.StartsWith(prefix, StringComparison.Ordinal))
                    return shell.Press(controls[index].Id);
            }

            return false;
        }

        static OperationsAriaSkillKind ParseSkill(string skill)
        {
            if (Enum.TryParse(skill, out OperationsAriaSkillKind parsed))
                return parsed;
            return OperationsAriaSkillKind.Focus;
        }
    }
}
