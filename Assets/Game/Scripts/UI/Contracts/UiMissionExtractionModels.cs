using UnityEngine;

namespace Game.UI.Contracts
{
    public enum UiMissionExtractionAction : byte { ShowLesson, ContinuePlan, FocusTeam, FocusLanding, FocusDeparture }
    public readonly struct UiMissionExtractionModel
    {
        public readonly int Aboard, Delivered, Required, CarrierLeg, SecureSeconds, RemainingSeconds, Lesson, PatrolSeconds, RequiredSecureSeconds;
        public readonly bool Contested, Cleared, AircraftAtLanding;
        public readonly Vector3 LandingCenter, DepartureCenter;
        public readonly float LandingRadius, DepartureRadius;
        public readonly Quaternion MarkerRotation;
        public UiMissionExtractionModel(int aboard,int delivered,int required,int carrierLeg,int secure,int remaining,int lesson,bool contested,bool cleared,Vector3 landingCenter=default,Vector3 departureCenter=default,float landingRadius=18,float departureRadius=14,int patrolSeconds=-1,Quaternion markerRotation=default,int requiredSecureSeconds=20,bool aircraftAtLanding=true)
        {RequiredSecureSeconds=requiredSecureSeconds;AircraftAtLanding=aircraftAtLanding;MarkerRotation=markerRotation;PatrolSeconds=patrolSeconds;Aboard=aboard;Delivered=delivered;Required=required;CarrierLeg=carrierLeg;SecureSeconds=secure;RemainingSeconds=remaining;Lesson=lesson;Contested=contested;Cleared=cleared;LandingCenter=landingCenter;DepartureCenter=departureCenter;LandingRadius=landingRadius;DepartureRadius=departureRadius;}
    }
    public interface IUiMissionExtractionGateway
    {
        bool TryReadMissionExtraction(out UiMissionExtractionModel model);
        bool TryRequestExtractionAction(UiMissionExtractionAction action);
        bool IsExtractionGuideContext();
    }
}
