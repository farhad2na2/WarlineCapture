using System;
using System.Runtime.Serialization;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Unity.Entities;

internal static class S004AirOpeningSegmentControl
{
    sealed class Recruitment : SourceAriaRecruitmentBoundary
    {
        internal int Calls;
        internal Recruitment(EntityManager manager, Entity session) : base(manager, session) { }
        internal override SourceAriaRecruitmentSurface Read(SkirmishRoleKind role)
        { Calls++; return new SourceAriaRecruitmentSurface(false, false, "fixture"); }
        internal override SourceAriaRecruitmentReceipt Request(SkirmishRoleKind role, float now)
        { Calls++; return default; }
    }

    static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static void Main()
    {
        var manager = new EntityManager();
        var other = new EntityManager();
        var session = Entity.Null;
        var army = (SkirmishArmyProfileConfig)FormatterServices.GetUninitializedObject(typeof(SkirmishArmyProfileConfig));
        var foreign = new Recruitment(other, session);
        bool rejected = false;
        try { _ = new SourceS004AirOpeningSegment(manager, session, army, foreign); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected && foreign.Calls == 0, "Foreign world must be rejected before a read or command");
        var same = new Recruitment(manager, session);
        var segment = new SourceS004AirOpeningSegment(manager, session, army, same);
        rejected = false;
        try { segment.Step(); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected && same.Calls == 0, "Missing session must be rejected before a read or command");
        Console.WriteLine("[S004AirOpeningSegmentControl] result=Passed cases=2 fixtureOnly=1 commands=0 readiness=0");
    }
}
