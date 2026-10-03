using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private void ApplyHudButtonIcons()
        {
            HudIconButtonView.Apply(showMeButton, HudButtonIcon.Camera, HudButtonRole.Information, "SHOW ME", "نشان بده");
            HudIconButtonView.Apply(ContinueButton, HudButtonIcon.Continue, HudButtonRole.Play, "CONTINUE", "ادامه");
            StyleUtilityButtons(utilityActions);
            StyleUtilityButtons(extractionActions);
        }

        private static void StyleUtilityButtons(RectTransform row)
        {
            if (row == null) return;
            foreach (var button in row.GetComponentsInChildren<Button>(true))
                switch (button.name)
                {
                    case "FieldGuide": case "Guide":
                        HudIconButtonView.Apply(button, HudButtonIcon.Guide, HudButtonRole.Information, "FIELD GUIDE", "راهنمای میدان"); break;
                    case "ReadWarning":
                        HudIconButtonView.Apply(button, HudButtonIcon.Alert, HudButtonRole.Alert, "READ ALERT", "هشدار"); break;
                    case "SkipLesson":
                        HudIconButtonView.Apply(button, HudButtonIcon.Skip, HudButtonRole.Neutral, "SKIP", "رد کردن"); break;
                    case "ReturnWarningCamera":
                        HudIconButtonView.Apply(button, HudButtonIcon.Back, HudButtonRole.Navigation, "BACK", "بازگشت"); break;
                    case "Team":
                        HudIconButtonView.Apply(button, HudButtonIcon.Team, HudButtonRole.Navigation, "TEAM", "گروه"); break;
                    case "Landing":
                        HudIconButtonView.Apply(button, HudButtonIcon.Landing, HudButtonRole.Navigation, "LANDING", "محل فرود"); break;
                    case "Departure":
                        HudIconButtonView.Apply(button, HudButtonIcon.Departure, HudButtonRole.Navigation, "DEPARTURE", "خروج"); break;
                }
        }
    }
}
