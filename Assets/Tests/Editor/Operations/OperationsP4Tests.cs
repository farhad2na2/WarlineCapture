#if UNITY_EDITOR
using NUnit.Framework;

namespace Game.Tests.Editor.Operations
{
    public sealed class OperationsP4Tests
    {
        [Test]
        public void Package4HostChecksPass()
        {
            OperationsP4Checks.RunAll();
        }
    }
}
#endif
