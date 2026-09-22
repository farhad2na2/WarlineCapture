#if UNITY_EDITOR
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsO001PlayerReadyTests
    {
        [Test]
        public void PlayerShellHostChecksPass()
        {
            OperationsO001PlayerReadyChecks.RunAll();
        }
    }
}
#endif
