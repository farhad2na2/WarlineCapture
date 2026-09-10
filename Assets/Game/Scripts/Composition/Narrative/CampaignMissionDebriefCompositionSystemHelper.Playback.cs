using Game.Narrative.Contracts;
using Game.Configs;
using Game.UI.Runtime;

namespace Game.Composition
{
    internal sealed partial class CampaignMissionDebriefCompositionSystemHelper
    {
        internal sealed class PlaybackPresentationSystemHelper
        {
            private FirstLaunchNarrativeSequencePresentationSystemHelper presentation;
            private NarrativeSequenceView view;
            private string pendingDestination;
            private string renderedState, renderedLocale;
            private bool wasPaused, renderedPaused, renderedSubtitles;

            public void Bind(FirstLaunchNarrativeSequencePresentationSystemHelper owner, NarrativeSequenceView target)
            {
                Unbind(); presentation=owner; view=target;
                presentation.SkipRequested+=RequestSkip;
                view.SkipConfirmationView?.Bind(ConfirmSkip,CancelSkip);
                view.SkipConfirmationView?.SetVisible(false);
                view.ReviewerControlsView?.SetDevelopmentVisibility(false);
                view.PlaybackControlsView?.BindTransport(TogglePause,ToggleSubtitles);
                renderedState=null; Tick();
            }
            public void Tick()
            {
                if(presentation==null || view==null) return;
                if(renderedState==presentation.CurrentStateId && renderedPaused==presentation.IsPaused &&
                    renderedSubtitles==presentation.SubtitlesEnabled && renderedLocale==GameLocalization.CurrentLocaleCode) return;
                renderedState=presentation.CurrentStateId; renderedPaused=presentation.IsPaused; renderedSubtitles=presentation.SubtitlesEnabled;
                renderedLocale=GameLocalization.CurrentLocaleCode;
                int pages=System.Math.Max(1,presentation.StateCount-1);
                view.PlaybackControlsView?.ApplyTransport(System.Math.Min(pages,presentation.CurrentStateIndex+1),pages,renderedPaused,renderedSubtitles);
            }
            public void Unbind()
            {
                if(presentation!=null) presentation.SkipRequested-=RequestSkip;
                view?.SkipConfirmationView?.Unbind(); view?.SkipConfirmationView?.SetVisible(false);
                view?.PlaybackControlsView?.UnbindTransport();
                presentation=null; view=null; pendingDestination=null; renderedState=null;
            }
            private void RequestSkip(NarrativeRouteRequest request)
            {
                if(presentation==null || !presentation.IsRunning || !string.IsNullOrEmpty(pendingDestination) || string.IsNullOrEmpty(request.DestinationId)) return;
                pendingDestination=request.DestinationId; wasPaused=presentation.IsPaused;
                presentation.Pause(); view.SetSkipState(false,false,presentation.SkipLabel);
                if(view.SkipConfirmationView==null) { ConfirmSkip(); return; }
                view.SkipConfirmationView.SetVisible(true);
            }
            private void ConfirmSkip()
            {
                if(presentation==null || string.IsNullOrEmpty(pendingDestination)) return;
                string destination=pendingDestination; pendingDestination=null;
                view.SkipConfirmationView?.SetVisible(false);
                presentation.StartAt(destination,presentation.CreateCompletion(destination,true));
            }
            private void CancelSkip()
            {
                if(presentation==null || string.IsNullOrEmpty(pendingDestination)) return;
                pendingDestination=null; view.SkipConfirmationView?.SetVisible(false);
                if(!wasPaused) presentation.Resume();
                view.SetSkipState(true,true,presentation.SkipLabel);
            }
            private void TogglePause()
            {
                if(presentation==null || !string.IsNullOrEmpty(pendingDestination)) return;
                if(presentation.IsPaused) presentation.Resume(); else presentation.Pause(); Tick();
            }
            private void ToggleSubtitles()
            {
                if(presentation==null) return;
                presentation.SetSubtitlesEnabled(!presentation.SubtitlesEnabled); Tick();
            }
        }
    }
}
