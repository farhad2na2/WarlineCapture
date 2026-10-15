#if UNITY_EDITOR
namespace Game.Tests.Editor
{
    public static class SkirmishMissionLogicalBindingValidation
    {
        public static void RunFocusedValidation()
        {
            var t=new SkirmishMissionLogicalBindingTests();
            t.MissingActualLogicalAssetRejectsNonBaWhileBaRemainsUnchanged();
            t.FramingRequiresActualOpeningAndRecruitRolesWithNormalViewEvidence();
            t.FocusMustBeExactOwnedOpeningMemberAndUsesEstablishedSmoothFloor();
            t.AirborneRoleNeedsDistinctElevationAndFollowRestorationStages();
            t.CameraRoleDigestIsOrderIndependentButBindsElevationFocusAndPixels();
            t.DuplicateUnknownAndNonfiniteFramingDeclarationsReject();
            t.SourceAndAreaLogicalDomainsRemainDistinctAndNativeReadIsFailClosed();
            t.S005EngineeringProfileIdentityAndExistingSourceDerivedValuesRemainIntact();
            t.GenericBaseAssaultProfileAppliesScopedZoomAndRejectsCachedValueDrift();
            t.ElevatedRoleFocusUsesExistingGroundPlaneCameraAndAbsoluteHeight();
            t.ActualQueuedGenericProviderRejectsViewAndLayoutDrift();
            t.RenderBoundsObstructionCandidatesDoNotClaimRasterVisibility();
            t.MissingNativeMeshRetainsFailureWithoutClaimingClearPixels();
            UnityEngine.Debug.Log("[SkirmishMissionLogicalBindingValidation] result=Passed tests=13 sourceFit=0 readablePixelsAccepted=0 nativeMission=0");
        }
    }
}
#endif
