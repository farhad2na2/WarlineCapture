using System;
using Game.Configs;
namespace Game.UI.Runtime
{
    public static class SharedResultStateClassifier
    {
        public static ResultObjectiveState Classify(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return ResultObjectiveState.Information;
            if(GameLocalization.TryGetSourceByLocalized(value,out _,out var source))value=source;
            string state=value.Trim().ToUpperInvariant();
            // Exact negative terms precede positive ones: INCOMPLETE is never COMPLETE.
            foreach(string word in new[]{"FAILED","MISSED","DESTROYED","LOST","DAMAGED","LOSSES"})
                if(state==word)return ResultObjectiveState.Failed;
            foreach(string word in new[]{"INCOMPLETE","NOT COMPLETE","NOT EARNED","AT RISK"})
                if(state==word)return ResultObjectiveState.Incomplete;
            foreach(string word in new[]{"COMPLETE","COMPLETED","HELD","STABLE","SAFE","UNDAMAGED","SECURED","VERIFIED","EARNED","INTACT","SURVIVED","STOPPED"})
                if(state==word)return ResultObjectiveState.Complete;
            string[] ratio=state.Split('/');
            if(ratio.Length==2&&int.TryParse(ratio[0].Trim(),out int actual)&&int.TryParse(ratio[1].Trim(),out int required)&&required>0)
                return actual>=required?ResultObjectiveState.Complete:ResultObjectiveState.Incomplete;
            return ResultObjectiveState.Information;
        }
    }
}
