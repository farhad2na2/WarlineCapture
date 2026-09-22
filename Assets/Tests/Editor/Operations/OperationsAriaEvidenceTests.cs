#if UNITY_EDITOR
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsAriaEvidenceTests
    {
        [Test]
        public void AriaEvidenceHarnessPasses()
        {
            OperationsAriaEvidenceChecks.RunAll();
        }
    }
}
#endif
