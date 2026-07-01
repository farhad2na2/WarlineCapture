using System;
using UnityEngine;

public interface IBattleHudRuntimeFeedbackView
{
    TacticalCommandMode CurrentCommandMode { get; set; }

    TacticalCommandMode StickyCommandMode { get; set; }

    TacticalCommandResult LastCommandResult { get; set; }

    bool HasLastCommandResult { get; set; }

    BattleHudRuntimeFeedbackState RuntimeFeedbackState { get; }

    Sprite ResolveCommandIconSprite(TacticalCommandMode mode);

    void ApplyCurrentOrderBanner(MatchHudCurrentOrderBannerModel model);

    void ApplyTransientCurrentOrderBanner(MatchHudCurrentOrderBannerModel model, float now, float durationSeconds);

    void ApplyCommandFeedbackActions(MatchHudCommandFeedbackActionsModel model);

    void ApplyCommandModeTabs(TacticalCommandMode mode);

    void ApplyPersistentCommandFeedback(MatchHudCommandFeedbackModel model, MatchHudCommandFeedbackActionsModel actionsModel);

    void ApplyTransientCommandFeedback(MatchHudCommandFeedbackModel model, float now);

    void BindFeedbackActionCallbacks(Action boardAllRequested, Action cancelRequested);

    void ClearCommandModeTabs();

    void ClearFeedbackActionCallbacks();

    void HideCurrentOrderBanner();

    void ClearPersistentCommandFeedback();

    void HideCommandMode();

    void HideFeedbackMessage();

    void HideInvalidCommand();

    void HideSelectedEntity();

    void SetWorldMarkersVisible(bool visible);

    void ShowCommandMode(string mode);

    void ShowFeedbackMessage(string message);

    void ShowFeedbackMessage(string message, CommandFeedbackSeverity severity);

    void ShowInvalidCommand(string reason);

    void ShowSelectedEntity(string displayName, string status);

    void TickFeedbackLifetime(float now);
}
