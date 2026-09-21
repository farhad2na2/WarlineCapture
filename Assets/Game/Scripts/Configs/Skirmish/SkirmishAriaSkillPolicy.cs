using Game.Skirmish.Contracts;

namespace Game.Configs
{
    public static class SkirmishAriaSkillPolicy
    {
        public const int MaxRetries = 3;

        public static SkirmishAriaSkillDecision Step(in SkirmishAriaPublicView view)
        {
            var decision = new SkirmishAriaSkillDecision
            {
                Phase = SkirmishAriaSkillPhase.Observe,
                Field = "observation"
            };
            if (view.Finished)
            {
                decision.Accepted = true;
                decision.Skill = SkirmishAriaSkillKind.RecognizeResult;
                decision.Phase = SkirmishAriaSkillPhase.Terminal;
                decision.Field = "result";
                return decision;
            }

            if (!view.Playing)
            {
                decision.Skill = SkirmishAriaSkillKind.Handback;
                decision.Phase = SkirmishAriaSkillPhase.Handback;
                decision.Handback = true;
                decision.Reason = SkirmishReasonCode.StaleCommand;
                decision.Field = "not_playing";
                return decision;
            }

            if (view.FailedAttempts >= MaxRetries && view.LastSkill != SkirmishAriaSkillKind.None)
            {
                decision.Skill = Alternative(view);
                decision.Phase = decision.Skill == SkirmishAriaSkillKind.Handback
                    ? SkirmishAriaSkillPhase.Handback
                    : SkirmishAriaSkillPhase.Alternative;
                decision.Handback = decision.Skill == SkirmishAriaSkillKind.Handback;
                decision.RetryCount = view.FailedAttempts;
                decision.Accepted = !decision.Handback;
                decision.Field = "retry_exhausted";
                return decision;
            }

            decision = Choose(view);
            decision.RetryCount = view.LastSkill == decision.Skill && !view.LastSkillAccepted
                ? view.FailedAttempts + 1
                : 0;
            return decision;
        }

        private static SkirmishAriaSkillDecision Choose(in SkirmishAriaPublicView view)
        {
            if (view.VisibleHostileAir > 0 && view.CanAffordAntiAir && view.RecruitControlAvailable)
                return Act(SkirmishAriaSkillKind.Recruit, "recruit.aa");
            if (view.OwnInfantry < 16 && view.CanAffordRifle && view.RecruitControlAvailable)
                return Act(SkirmishAriaSkillKind.Recruit, "recruit.rifle");
            if (view.VisibleHostileCombat > 0 && view.CanAffordRocketeer && view.RecruitControlAvailable)
                return Act(SkirmishAriaSkillKind.Recruit, "recruit.counter");
            if (view.VisibleHostileAir > 0 && !view.PadReady && view.AirPadControlAvailable)
                return Act(SkirmishAriaSkillKind.Inspect, "pad.not_ready");
            if (!view.GroupControlAvailable && view.HoldControlAvailable)
                return Act(SkirmishAriaSkillKind.SelectGroup, "select.group");
            if (view.EnemyDesignatedAlive && view.AttackControlAvailable && view.GroupControlAvailable)
                return Act(SkirmishAriaSkillKind.Attack, "attack.base");
            if (view.HoldControlAvailable && view.GroupControlAvailable)
                return Act(SkirmishAriaSkillKind.Hold, "hold.reserve");
            if (view.RecruitControlAvailable)
                return Act(SkirmishAriaSkillKind.Inspect, "inspect.economy");
            return new SkirmishAriaSkillDecision
            {
                Skill = SkirmishAriaSkillKind.Scout,
                Phase = SkirmishAriaSkillPhase.Choose,
                Field = "scout",
                Accepted = true
            };
        }

        private static SkirmishAriaSkillKind Alternative(in SkirmishAriaPublicView view)
        {
            if (view.LastSkill == SkirmishAriaSkillKind.Attack && view.HoldControlAvailable)
                return SkirmishAriaSkillKind.Hold;
            if (view.LastSkill == SkirmishAriaSkillKind.Recruit && view.HoldControlAvailable)
                return SkirmishAriaSkillKind.Hold;
            if (view.LastSkill == SkirmishAriaSkillKind.Hold && view.RecruitControlAvailable)
                return SkirmishAriaSkillKind.Inspect;
            return SkirmishAriaSkillKind.Handback;
        }

        private static SkirmishAriaSkillDecision Act(SkirmishAriaSkillKind skill, string field) =>
            new SkirmishAriaSkillDecision
            {
                Accepted = true,
                Skill = skill,
                Phase = SkirmishAriaSkillPhase.Act,
                Field = field
            };
    }
}
