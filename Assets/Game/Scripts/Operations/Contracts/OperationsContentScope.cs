using System;

namespace Game.Operations.Contracts
{
    public static class OperationsContentScope
    {
        public const string Intro = "operations.sahrin.intro.v1";
        public const string Full = "operations.sahrin.full.v1";
        public static string Normalize(string scope) => string.IsNullOrEmpty(scope) ? Full : scope;
        public static bool IsKnown(string scope) => Normalize(scope) is Intro or Full;
        public static bool IsIntro(string scope) => scope == Intro;
        public static bool AllowsMission(string scope, string missionId) =>
            IsKnown(scope) && OperationsIdentityRules.IsValidMissionId(missionId) &&
            (!IsIntro(scope) || missionId is "operation.o001" or "operation.o002" or "operation.o003");
        public static bool AllowsDistrict(string scope, int number) => IsKnown(scope) &&
            number >= 1 && number <= (IsIntro(scope) ? 1 : OperationsIdentityRules.DistrictCount);
        public static void Require(string scope)
        { if (!IsKnown(scope)) throw new ArgumentException("Unknown Operations content scope.", nameof(scope)); }
    }
}
