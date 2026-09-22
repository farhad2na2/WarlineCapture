#if UNITY_EDITOR
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsAriaPlayModeCaptureTests
    {
        [Test]
        public void PlayModeCaptureWiringPasses()
        {
            OperationsAriaPlayModeCaptureChecks.RunAll();
        }
    }
}
#endif
