#if UNITY_EDITOR
using NUnit.Framework;
namespace Game.Tests.Editor
{
    public sealed class SkirmishProductionMatrixAuthorityTests
    {
        [Test] public void Actual720PacketAuthorityAdmitsExactFlagsOnly()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().Actual720PacketAuthorityAdmitsExactFlagsOnly();
        [Test] public void MissingDuplicateWrongSeedAndAuthorityRefuse()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().MissingDuplicateWrongSeedAndAuthorityRefuse();
        [Test] public void SyntheticStressHasAllSixtyAlternatingTwentySeedStrata()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().SyntheticStressHasAllSixtyAlternatingTwentySeedStrata();
        [Test] public void ChangedPlanCannotAuthorizeNextJob()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().ChangedPlanCannotAuthorizeNextJob();
        [Test] public void DisabledNativeSessionCannotMasqueradeAsZeroCleanup()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().DisabledNativeSessionCannotMasqueradeAsZeroCleanup();
        [Test] public void OperationalDefeatAndDrawRemainLossesAndRequireCleanNativeFacts()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().OperationalDefeatAndDrawRemainLossesAndRequireCleanNativeFacts();
        [Test] public void RuntimeErrorsCannotBecomeCleanOperationalCompletion()=>new Game.Editor.SkirmishProductionMatrixAuthorityValidation().RuntimeErrorsCannotBecomeCleanOperationalCompletion();
        public static void RunFocusedValidation()=>Game.Editor.SkirmishProductionMatrixAuthorityValidation.RunFocusedValidation();
    }
}
#endif
