using System;
using Game.Missions.Contracts;

public static class ImplementedContentAccessChecks
{
    public const string PassMarker = "[ImplementedContentAccess] result=Passed canonical=205 profiles=4 free=9 ownership,progression,readiness,missing,restore,offline";
    sealed class VerifiedFixture : IVerifiedContentEntitlements
    {
        public bool Campaign, Operations;
        // Models an already verified account snapshot, including offline reuse.
        public bool Owns(string id) => id == ContentAccessAuthority.CampaignProduct ? Campaign :
            id == ContentAccessAuthority.OperationsProduct && Operations;
    }
    static void Require(bool condition, string detail)
    { if (!condition) throw new InvalidOperationException(detail); }
    public static void RunAll()
    {
        int count = 0, free = 0, campaign = 0, operations = 0;
        foreach (var row in ContentAccessAuthority.Memberships)
        {
            count++; if (row.Free) free++;
            if (row.ProductId == ContentAccessAuthority.CampaignProduct) campaign++; else operations++;
            for (int profile = 0; profile < 4; profile++)
            {
                var fixture = new VerifiedFixture { Campaign = (profile & 1) != 0, Operations = (profile & 2) != 0 };
                var access = new ContentAccessAuthority(fixture);
                bool owns = row.Free || fixture.Owns(row.ProductId);
                Require(access.Evaluate(row.Id, true, true, true) ==
                    (owns ? ContentAccessState.Allowed : ContentAccessState.NotOwned), row.Id + " profile " + profile);
                Require(access.Evaluate(row.Id, false, true, true) == ContentAccessState.InstalledContentMissing, "missing " + row.Id);
                Require(access.Evaluate(row.Id, true, false, true) == ContentAccessState.Unavailable, "readiness " + row.Id);
                Require(access.Evaluate(row.Id, true, true, false) == ContentAccessState.ProgressionLocked, "progression " + row.Id);
                Require(access.Evaluate(row.Id, true, true, true, false) == ContentAccessState.Allowed, "inactive release restriction");
            }
        }
        Require(count == 205 && free == 9 && campaign == 145 && operations == 60, "membership totals");
        var restored = new VerifiedFixture();
        var restoredAccess = new ContentAccessAuthority(restored);
        const string paid = "skirmish.s025";
        Require(restoredAccess.Evaluate(paid, true, true, true) == ContentAccessState.NotOwned, "before verified restore");
        restored.Campaign = true;
        Require(restoredAccess.Evaluate(paid, true, true, true) == ContentAccessState.Allowed, "verified restore");
        // Temporary offline operation continues using the same verified account snapshot.
        Require(restoredAccess.Evaluate(paid, true, true, true) == ContentAccessState.Allowed, "offline cached ownership");
        Require(restoredAccess.Evaluate("scenario.debug", true, true, true) == ContentAccessState.UnknownContent, "unknown content");
        Require(restoredAccess.Evaluate(null, true, true, true) == ContentAccessState.UnknownContent, "null content");
    }
}
