using System;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    // Access is consulted only at deployment/replay/resume boundaries, never by
    // battle simulation or reward settlement. Commerce and release restrictions
    // remain disabled until the complete products and verified billing are accepted.
    public static class ContentAccessRuntime
    {
        public const bool ReleaseOwnershipRestrictionsEnabled = false;
        private sealed class NoVerifiedEntitlements : IVerifiedContentEntitlements
        { public bool Owns(string productId) => false; }
        private static ContentAccessAuthority authority = new(new NoVerifiedEntitlements());
        private static bool enforceOwnership = ReleaseOwnershipRestrictionsEnabled;

        public static ContentAccessState Evaluate(string canonicalId, bool installed = true,
            bool ready = true, bool progression = true) =>
            authority.Evaluate(canonicalId, installed, ready, progression, enforceOwnership);

        public static void ConfigureVerifiedProvider(IVerifiedContentEntitlements provider) =>
            authority = new ContentAccessAuthority(provider);

#if UNITY_EDITOR
        // Explicit, scoped, non-shipping fixture. It cannot persist ownership or
        // alter progression. Restore the previous provider when the fixture ends.
        public static IDisposable UseValidationProvider(IVerifiedContentEntitlements provider)
        {
            var scope = new ValidationScope(authority, enforceOwnership);
            authority = new ContentAccessAuthority(provider); enforceOwnership = true;
            return scope;
        }
        private sealed class ValidationScope : IDisposable
        {
            private readonly ContentAccessAuthority previous;
            private readonly bool previousEnforcement;
            public ValidationScope(ContentAccessAuthority previous, bool enforced)
            { this.previous = previous; previousEnforcement = enforced; }
            public void Dispose() { authority = previous; enforceOwnership = previousEnforcement; }
        }
#endif
    }
}
