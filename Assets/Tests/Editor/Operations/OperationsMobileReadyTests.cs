using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsMobileReadyTests
    {
        [Test]
        public void MobileReadyChecks_Pass()
        {
            OperationsMobileReadyChecks.RunAll();
        }
    }
}
