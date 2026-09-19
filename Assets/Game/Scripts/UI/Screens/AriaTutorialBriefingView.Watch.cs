using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class AriaTutorialBriefingView
    {
        private Button watchButton, watchConfirm, watchCancel;
        private RectTransform watchActions;
        private TMP_Text watchLabel, confirmLabel, cancelLabel;
        private string watchLocale;
        internal Button WatchButton => watchButton;
        private bool watchConfirming, watchStartFailed;
        private string watchCaption;
        internal void PresentWatch(AriaPlayModel model, bool available)
        {
            if (doItButton == null) return;
            if (watchButton == null)
            {
                watchActions = (RectTransform)new GameObject("AriaWatchActions", typeof(RectTransform)).transform;
                watchActions.SetParent(transform, false);
                watchButton = CreateWatchButton("WatchAriaPlay", OnWatchRequested);
                watchConfirm = CreateWatchButton("ConfirmWatchAria", OnWatchConfirmed);
                watchCancel = CreateWatchButton("CancelWatchAria", () => watchConfirming = false);
                watchLabel = watchButton.GetComponentInChildren<TMP_Text>(true);
                confirmLabel = watchConfirm.GetComponentInChildren<TMP_Text>(true);
                cancelLabel = watchCancel.GetComponentInChildren<TMP_Text>(true);
            }
            string locale = UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            bool fa = locale.StartsWith("fa");
            watchActions.gameObject.SetActive(available || model.Active || model.Phase == AriaPlayPhase.Blocked);
            if (!available && !model.Active) watchConfirming = false;
            watchButton.gameObject.SetActive(available || model.Active || model.Phase == AriaPlayPhase.Blocked);
            watchButton.interactable = !watchConfirming;
            string caption = model.Active ? (fa ? "کنترل رو پس بگیر" : "Stop ARIA") :
                watchConfirming && watchStartFailed ? (fa ? "آریا نتونست شروع کنه. دوباره امتحان کن یا لغو رو بزن." : "ARIA couldn't start. Try again or cancel.") :
                watchConfirming ? (fa ? "آریا با نیروها و منابع همین بازی ادامه بده؟" : "Let ARIA play using your match resources?") :
                model.Phase == AriaPlayPhase.Blocked ? (fa ? "آریا نتونست ادامه بده — دوباره امتحان کن" : "ARIA couldn't continue — try again") :
                UiShellRuntimeGateway.ReadAriaPlayCapability() == AriaPlayCapability.BaseAssault
                    ? (fa ? "بازی آریا — نسخهٔ آزمایشی" : "Watch ARIA — preview")
                    : (fa ? "ببین آریا چطور بازی می‌کنه" : "Watch ARIA play");
            if (caption != watchCaption)
            {
                watchCaption = caption; UiLocalizedText.Set(watchLabel, caption);
                var colors = watchButton.colors; colors.normalColor = Color.white; watchButton.colors = colors;
                if (watchButton.targetGraphic is V3GradientGraphic gradient)
                    gradient.Configure(model.Active ? new Color(.65f,.12f,.06f) : new Color(0,.38f,.52f),
                        model.Active ? new Color(.2f,.025f,.01f) : new Color(0,.12f,.19f),
                        model.Active ? new Color(1,.35f,.15f) : new Color(0,.78f,.94f), 3);
            }
            watchConfirm.gameObject.SetActive(watchConfirming);
            watchCancel.gameObject.SetActive(watchConfirming);
            if (watchLocale != locale)
            {
            watchLocale = locale;
            UiLocalizedText.Set(confirmLabel, fa ? "شروع — هر وقت خواستی کنترل رو پس بگیر" : "Start — take control anytime");
            UiLocalizedText.Set(cancelLabel, fa ? "لغو" : "Cancel");
            }
        }
        private string SkirmishWatchInstruction(string fallback)
        {
            if (!UiShellRuntimeGateway.TryReadSkirmish(out _) || !UiShellRuntimeGateway.ReadAriaPlay().Active) return fallback;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            return UiShellRuntimeGateway.ReadAriaSkirmishIntent() switch
            {
                AriaSkirmishIntent.FindThreat => fa ? "روی نقطهٔ قرمز نقشه می‌زنم تا دشمن نزدیک نیروهامون رو ببینیم." : "I’ll tap the red map contact to see the enemy nearest our force.",
                AriaSkirmishIntent.TargetThreat => fa ? "اول این دشمن رو می‌زنیم تا نیروهامون بتونن جلو برن." : "We’ll deal with this enemy before advancing on the base.",
                AriaSkirmishIntent.Recruit => fa ? "از منوی ساخت نیروی کمکی می‌گیرم. اگه منابع کافی نباشه، با همین نیروها ادامه می‌دیم." : "I'll recruit reinforcements through Build. If we cannot afford them, we'll use our current force.",
                AriaSkirmishIntent.SelectSquad => fa ? "اول کارت گروه رو می‌زنم تا نیروها انتخاب بشن." : "I'll tap a squad card to select its soldiers.",
                AriaSkirmishIntent.FindBase => fa ? "دکمهٔ پایگاه دشمن دوربین رو می‌بره پیش هدف." : "The enemy-base button brings the objective into view.",
                AriaSkirmishIntent.Attack => fa ? "حالا حمله رو انتخاب می‌کنم؛ بعد باید خود هدف رو بزنیم." : "I'll choose Attack, then tap the target itself.",
                AriaSkirmishIntent.TargetBase => fa ? "پایگاه مشخص‌شده رو می‌زنم تا این گروه بهش حمله کنه." : "I'll tap the marked base to send this squad to attack it.",
                _ => fa ? "نیروها دارن دستور رو اجرا می‌کنن. چند لحظه روند نبرد و وضعیت پایگاه‌ها رو نگاه می‌کنم." : "The troops are carrying out their orders. I'll watch the battle and both bases before acting again."
            };
        }
        private Button CreateWatchButton(string objectName, UnityEngine.Events.UnityAction action)
        {
            var button = Instantiate(doItButton, watchActions);
            button.name = objectName;
            // Do It can be cloned while the cinematic lock owns its transient group.
            // New controls must not inherit that lock with no restoration owner.
            var group = button.GetComponent<CanvasGroup>();
            if (group != null) { group.interactable = true; group.blocksRaycasts = true; group.alpha = 1; }
            UiDisabledMaterialUtility.SetDisabled(button.gameObject, UiDisabledVisualReason.CinematicInteractionLock, false);
            UiDisabledMaterialUtility.SetSelectableDisabled(button, UiDisabledVisualReason.CinematicInteractionLock, false);
            button.interactable = true;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18; label.fontSizeMax = 22;
            button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(action);
            return button;
        }
        private void OnWatchRequested()
        {
            if (UiShellRuntimeGateway.ReadAriaPlay().Active) UiShellRuntimeGateway.StopAriaPlay();
            else { watchStartFailed = false; watchConfirming = true; }
        }
        private void OnWatchConfirmed()
        {
            bool accepted = UiShellRuntimeGateway.TryStartAriaPlay();
            watchStartFailed = !accepted;
            watchConfirming = !accepted;
        }
    }
}
