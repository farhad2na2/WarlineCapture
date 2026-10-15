using System;
using System.Collections.Generic;

namespace Game.Skirmish.Contracts
{
    public enum SkirmishPublicGoalPerspective : byte
    { None, BaseAssault, Capture, Evacuate, DefendExits, Escort, Interdict }
    public enum SkirmishPublicGoalNodeKind : byte
    { None, CaptureZone, ExitCorridor, EvacuationExit, DeliveryDestination, OwnDesignatedSubject, ObservedOpposingSubject, OwnDesignatedBase, OpposingDesignatedBase }
    public enum SkirmishPublicGoalProvenance : byte
    { None, PublicObjective, OwnRoster, LegallyObservedContact }
    public enum SkirmishPublicCapturePhase : byte { None, Neutralizing, Capturing, Held }
    public enum SkirmishPublicHealthProvenance : byte { None, OwnRoster, CurrentlyObserved, LastObserved }
    public enum SkirmishPublicSubjectKind : byte { None, DesignatedRifle, ObjectiveConvoyTruck }
    public enum SkirmishPublicSubjectDisposition : byte { None, Live, Lost, Evacuated, Delivered }

    // Logical public marker identity, never a gameplay Entity or hidden actor handle.
    // Contact time uses the same mission elapsed clock as its enclosing snapshot.
    public struct SkirmishPublicGoalNode
    {
        public string PublicId;
        public SkirmishPublicGoalNodeKind Kind;
        public SkirmishPublicGoalProvenance Provenance;
        public bool PositionKnown, CurrentlyVisible;
        public float WorldX, WorldZ;
        public long LastObservedTicks;
        public bool OwnerKnown, Contested, CaptureProgressKnown;
        public int OwnerFaction, CapturingFaction;
        public float CaptureFraction;
        public SkirmishPublicCapturePhase CapturePhase;
        public bool WorkProgressKnown;
        public long WorkTicks, RequiredWorkTicks;
        public SkirmishPublicSubjectKind RevealedSubjectKind;
        public SkirmishPublicSubjectDisposition SubjectDisposition;
        public bool HealthKnown, Mounted;
        public float Health, MaximumHealth;
        public SkirmishPublicHealthProvenance HealthProvenance;
        public long HealthObservedTicks;
    }

    // Only values actually disclosed by the objective's public scoreboard.
    // Unknown values are represented by the flag and zero fields, never estimates.
    public struct SkirmishPublicGoalScore
    {
        public bool TicketsKnown, ProgressKnown;
        public int OwnTickets, OpposingTickets;
        public int Completed, Required, OriginalSubjects, SubjectsLost;
        public bool ExitStateKnown, ExitOpen;
    }

    // The HUD and ARIA must receive the same snapshot. Enemy perception builds an
    // independent snapshot from its own legal knowledge. This type does not certify
    // observation provenance; native producers and presentation joins must prove it.
    public sealed class SkirmishPublicGoalSnapshot
    {
        public const int SemanticVersion = 1;
        public readonly string DefinitionId;
        public readonly int DefinitionVersion, ObserverFaction;
        public readonly SkirmishObjectiveKind ObjectiveKind;
        public readonly SkirmishPublicGoalPerspective Perspective;
        public readonly long ElapsedTicks, RemainingTicks;
        public readonly SkirmishOutcomeKind Outcome;
        public readonly SkirmishPublicGoalScore Score;
        private readonly SkirmishPublicGoalNode[] nodes;
        public int NodeCount => nodes.Length;
        public SkirmishPublicGoalNode Node(int index) => nodes[index];

        private SkirmishPublicGoalSnapshot(string definitionId, int definitionVersion,
            int faction, SkirmishObjectiveKind kind, SkirmishPublicGoalPerspective perspective,
            long elapsed, long remaining, SkirmishOutcomeKind outcome,
            SkirmishPublicGoalScore score, SkirmishPublicGoalNode[] values)
        {
            DefinitionId=definitionId;DefinitionVersion=definitionVersion;ObserverFaction=faction;
            ObjectiveKind=kind;Perspective=perspective;ElapsedTicks=elapsed;RemainingTicks=remaining;
            Outcome=outcome;Score=score;nodes=(SkirmishPublicGoalNode[])values.Clone();
        }

        public static bool TryCreate(string definitionId, int definitionVersion,
            int faction, SkirmishObjectiveKind kind, SkirmishPublicGoalPerspective perspective,
            long elapsed, long remaining, SkirmishOutcomeKind outcome,
            SkirmishPublicGoalScore score, SkirmishPublicGoalNode[] values,
            out SkirmishPublicGoalSnapshot snapshot, out string error)
        {
            snapshot=null;error="Invalid public objective identity, clock or perspective.";
            if(string.IsNullOrWhiteSpace(definitionId)||definitionId.Length>61||definitionVersion<=0||
                (faction!=1&&faction!=2)||elapsed<0||remaining<0||!Enum.IsDefined(typeof(SkirmishOutcomeKind),outcome)||
                !PerspectiveMatches(kind,perspective)||values==null||values.Length==0||values.Length>32)return false;
            error="Invalid or undisclosed public scoreboard fields.";
            if(score.OwnTickets<0||score.OpposingTickets<0||score.Completed<0||score.Required<0||score.OriginalSubjects<0||score.SubjectsLost<0||
                (!score.TicketsKnown&&(score.OwnTickets!=0||score.OpposingTickets!=0))||
                (!score.ProgressKnown&&(score.Completed!=0||score.Required!=0||score.OriginalSubjects!=0||score.SubjectsLost!=0))||
                (score.ProgressKnown&&(score.Required==0||score.Required>score.OriginalSubjects||score.Completed>score.OriginalSubjects||
                    score.SubjectsLost>score.OriginalSubjects-score.Completed))||
                (kind!=SkirmishObjectiveKind.FrontlineControl&&score.TicketsKnown)||
                (!score.ExitStateKnown&&score.ExitOpen)||
                (kind!=SkirmishObjectiveKind.Breakthrough&&(score.ExitStateKnown||score.ExitOpen)))return false;
            var ids=new HashSet<string>(StringComparer.Ordinal);
            int zones=0,corridors=0,exits=0,destinations=0,ownBases=0,opposingBases=0,subjects=0;
            foreach(var node in values)
            {
                error="Invalid, duplicate, undisclosed or future public goal node.";
                if(string.IsNullOrWhiteSpace(node.PublicId)||node.PublicId.Length>61||!ids.Add(node.PublicId)||
                    !Enum.IsDefined(typeof(SkirmishPublicGoalNodeKind),node.Kind)||node.Kind==SkirmishPublicGoalNodeKind.None||
                    !Enum.IsDefined(typeof(SkirmishPublicGoalProvenance),node.Provenance)||node.Provenance==SkirmishPublicGoalProvenance.None||
                    !Finite(node.WorldX)||!Finite(node.WorldZ)||node.LastObservedTicks<0||node.LastObservedTicks>elapsed||
                    (!node.PositionKnown&&(node.WorldX!=0||node.WorldZ!=0||node.CurrentlyVisible))||
                    node.OwnerFaction<0||node.OwnerFaction>2||(!node.OwnerKnown&&node.OwnerFaction!=0)||
                    !Finite(node.CaptureFraction)||node.CaptureFraction<0||node.CaptureFraction>1||
                    !Enum.IsDefined(typeof(SkirmishPublicCapturePhase),node.CapturePhase)||
                    (!node.CaptureProgressKnown&&(node.CaptureFraction!=0||node.CapturingFaction!=0))||
                    (node.CaptureProgressKnown&&(node.CapturingFaction<1||node.CapturingFaction>2||
                        (node.CapturePhase!=SkirmishPublicCapturePhase.Neutralizing&&node.CapturePhase!=SkirmishPublicCapturePhase.Capturing)))||
                    node.WorkTicks<0||node.RequiredWorkTicks<0||node.WorkTicks>node.RequiredWorkTicks||
                    (!node.WorkProgressKnown&&(node.WorkTicks!=0||node.RequiredWorkTicks!=0))||
                    (node.WorkProgressKnown&&node.RequiredWorkTicks==0)||
                    !Enum.IsDefined(typeof(SkirmishPublicSubjectKind),node.RevealedSubjectKind)||
                    !Enum.IsDefined(typeof(SkirmishPublicSubjectDisposition),node.SubjectDisposition)||
                    !Finite(node.Health)||!Finite(node.MaximumHealth)||node.Health<0||node.MaximumHealth<0||node.Health>node.MaximumHealth||
                    !Enum.IsDefined(typeof(SkirmishPublicHealthProvenance),node.HealthProvenance)||node.HealthObservedTicks<0||node.HealthObservedTicks>elapsed||
                    (node.HealthKnown?node.MaximumHealth<=0||node.HealthProvenance==SkirmishPublicHealthProvenance.None:
                        node.Health!=0||node.MaximumHealth!=0||node.HealthProvenance!=SkirmishPublicHealthProvenance.None||node.HealthObservedTicks!=0))return false;
                bool own=node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject||node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedBase;
                bool hostile=node.Kind==SkirmishPublicGoalNodeKind.ObservedOpposingSubject;
                if(own&&(node.Provenance!=SkirmishPublicGoalProvenance.OwnRoster||!node.OwnerKnown||node.OwnerFaction!=faction))return false;
                if(hostile&&(node.Provenance!=SkirmishPublicGoalProvenance.LegallyObservedContact||!node.OwnerKnown||node.OwnerFaction==0||node.OwnerFaction==faction))return false;
                if(!own&&!hostile&&node.Provenance!=SkirmishPublicGoalProvenance.PublicObjective)return false;
                if(node.Mounted&&!own)return false;
                if(node.HealthKnown&&
                    (own?node.HealthProvenance!=SkirmishPublicHealthProvenance.OwnRoster:
                        node.HealthProvenance==SkirmishPublicHealthProvenance.OwnRoster||
                        (node.HealthProvenance==SkirmishPublicHealthProvenance.CurrentlyObserved&&!node.CurrentlyVisible)))return false;
                if(!NodeKindMatches(kind,perspective,node.Kind))return false;
                if(node.CaptureProgressKnown&&node.Kind!=SkirmishPublicGoalNodeKind.CaptureZone)return false;
                if(node.RevealedSubjectKind!=SkirmishPublicSubjectKind.None&&
                    node.Kind!=SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.Kind!=SkirmishPublicGoalNodeKind.ObservedOpposingSubject)return false;
                bool subject=node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject||node.Kind==SkirmishPublicGoalNodeKind.ObservedOpposingSubject;
                if(!subject&&node.SubjectDisposition!=SkirmishPublicSubjectDisposition.None)return false;
                if(node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&
                    (node.SubjectDisposition==SkirmishPublicSubjectDisposition.None||
                        node.RevealedSubjectKind!=(kind==SkirmishObjectiveKind.Breakthrough?SkirmishPublicSubjectKind.DesignatedRifle:SkirmishPublicSubjectKind.ObjectiveConvoyTruck)))return false;
                if(hostile&&(node.SubjectDisposition==SkirmishPublicSubjectDisposition.Evacuated||node.SubjectDisposition==SkirmishPublicSubjectDisposition.Delivered))return false;
                if(kind==SkirmishObjectiveKind.Breakthrough&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Delivered||
                    kind==SkirmishObjectiveKind.ConvoyEscort&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Evacuated)return false;
                if(node.CapturePhase==SkirmishPublicCapturePhase.Held&&(!node.OwnerKnown||node.OwnerFaction==0))return false;
                if(node.CapturePhase!=SkirmishPublicCapturePhase.None&&node.Kind!=SkirmishPublicGoalNodeKind.CaptureZone)return false;
                if(node.Kind==SkirmishPublicGoalNodeKind.CaptureZone)zones++;
                if(node.Kind==SkirmishPublicGoalNodeKind.ExitCorridor)corridors++;
                if(node.Kind==SkirmishPublicGoalNodeKind.EvacuationExit)exits++;
                if(node.Kind==SkirmishPublicGoalNodeKind.DeliveryDestination)destinations++;
                if(node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedBase)ownBases++;
                if(node.Kind==SkirmishPublicGoalNodeKind.OpposingDesignatedBase)opposingBases++;
                if(subject)subjects++;
            }
            error="Incomplete or contradictory public objective shape.";
            if(kind==SkirmishObjectiveKind.BaseAssault&&(ownBases!=1||opposingBases!=1)||
                kind==SkirmishObjectiveKind.FrontlineControl&&(zones!=3||ownBases>1||opposingBases>1)||
                kind==SkirmishObjectiveKind.Breakthrough&&(corridors!=2||exits!=1||subjects>12)||
                kind==SkirmishObjectiveKind.ConvoyEscort&&(destinations<1||destinations>3||subjects>3))return false;
            if(score.ProgressKnown&&
                (kind==SkirmishObjectiveKind.Breakthrough?score.OriginalSubjects!=12||score.Required!=8:
                    kind!=SkirmishObjectiveKind.ConvoyEscort||score.OriginalSubjects!=3||score.Required!=2))return false;
            snapshot=new SkirmishPublicGoalSnapshot(definitionId,definitionVersion,faction,kind,perspective,elapsed,remaining,outcome,score,values);
            error=null;return true;
        }

        public static bool PerspectiveMatches(SkirmishObjectiveKind kind,SkirmishPublicGoalPerspective perspective) =>
            kind==SkirmishObjectiveKind.BaseAssault?perspective==SkirmishPublicGoalPerspective.BaseAssault:
            kind==SkirmishObjectiveKind.FrontlineControl?perspective==SkirmishPublicGoalPerspective.Capture:
            kind==SkirmishObjectiveKind.Breakthrough?perspective==SkirmishPublicGoalPerspective.Evacuate||perspective==SkirmishPublicGoalPerspective.DefendExits:
            kind==SkirmishObjectiveKind.ConvoyEscort&&(perspective==SkirmishPublicGoalPerspective.Escort||perspective==SkirmishPublicGoalPerspective.Interdict);

        private static bool Finite(float value) => !float.IsNaN(value)&&!float.IsInfinity(value);

        private static bool NodeKindMatches(SkirmishObjectiveKind kind,SkirmishPublicGoalPerspective perspective,SkirmishPublicGoalNodeKind node) =>
            kind==SkirmishObjectiveKind.BaseAssault?node==SkirmishPublicGoalNodeKind.OwnDesignatedBase||node==SkirmishPublicGoalNodeKind.OpposingDesignatedBase:
            kind==SkirmishObjectiveKind.FrontlineControl?node==SkirmishPublicGoalNodeKind.CaptureZone||node==SkirmishPublicGoalNodeKind.OwnDesignatedBase||node==SkirmishPublicGoalNodeKind.OpposingDesignatedBase:
            kind==SkirmishObjectiveKind.Breakthrough?node==SkirmishPublicGoalNodeKind.ExitCorridor||node==SkirmishPublicGoalNodeKind.EvacuationExit||
                (perspective==SkirmishPublicGoalPerspective.Evacuate?node==SkirmishPublicGoalNodeKind.OwnDesignatedSubject:node==SkirmishPublicGoalNodeKind.ObservedOpposingSubject):
            kind==SkirmishObjectiveKind.ConvoyEscort&&(node==SkirmishPublicGoalNodeKind.DeliveryDestination||
                (perspective==SkirmishPublicGoalPerspective.Escort?node==SkirmishPublicGoalNodeKind.OwnDesignatedSubject:node==SkirmishPublicGoalNodeKind.ObservedOpposingSubject));
    }
}
