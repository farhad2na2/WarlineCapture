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
                    ? (fa ? "بازی آریا" : "ARIA Play")
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
                AriaSkirmishIntent.BuildDefense => fa ? "اول نزدیک پایگاه برج نگهبانی می‌سازم تا نیروهامون فرصت جمع شدن داشته باشن." : "I'll build watchtowers near our base so our troops have time to gather.",
                AriaSkirmishIntent.BuildAirPad => fa ? "دوربین رو به پایگاه خودمون می‌برم و جای امنی برای فرودگاه بالگرد انتخاب می‌کنم." : "I'll focus our base and choose a safe site for the helipad.",
                AriaSkirmishIntent.WaitForAirPadSite => fa ? "جای امن و قابل ساختی برای فرودگاه بالگرد پیدا نشد. ساخت رو لغو کردم؛ می‌تونید جای مناسبی انتخاب کنید یا دوباره آریا رو شروع کنید." : "I couldn't find a safe buildable helipad site, so I canceled placement. Choose a site or restart ARIA to retry.",
                AriaSkirmishIntent.ReturnAircraft => fa ? "بالگرد رو با دکمهٔ بازگشت به پایگاه برمی‌گردونم." : "I'll use Return to bring the helicopter home.",
                AriaSkirmishIntent.ServiceAircraft => fa ? "صبر می‌کنم بالگرد روی فرودگاه بنشینه و سوخت کافی برای پرواز بعدی داشته باشه." : "I'll wait for the helicopter to land and for fuel before the next flight.",
                AriaSkirmishIntent.DefendBase => fa ? "گروه رو روی حفظ می‌ذارم تا از پایگاه دفاع کنه. هم‌زمان نیروی کمکی می‌گیریم." : "I’ll put this squad on Hold to defend our base while reinforcements arrive.",
                AriaSkirmishIntent.GroupForce => fa ? "نیروهای نزدیک رو با کشیدن کادر انتخاب می‌کنم تا با هم حمله کنن." : "I’ll drag a selection box around our nearby troops so they fight together.",
                AriaSkirmishIntent.FindThreat => fa ? "با نقشه، دشمن نزدیک نیروهامون رو میارم توی دید." : "I’ll use the map to bring the nearest enemy into view.",
                AriaSkirmishIntent.Advance => fa ? "روی زمین می‌زنم تا نیروها جلو برن و توی مسیر با دشمن‌ها بجنگن." : "I’ll tap the ground to advance and engage enemies along the route.",
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
