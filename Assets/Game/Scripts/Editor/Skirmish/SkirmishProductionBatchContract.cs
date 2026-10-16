using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.Editor
{
    // Operational continuation only. This contract cannot publish queue success.
    public sealed class SkirmishProductionBatchContract
    {
        public enum Phase { Idle, Running, NativeFinished, Cleaned, Failed }
        public Phase Current { get; private set; }
        public readonly string Candidate, Build, ManifestFileHash;
        public readonly string[] BaselineWorlds;
        public readonly string BaselineScenes;
        public readonly int BaselinePreviewCount;
        private readonly HashSet<string> previousWorlds=new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> previousSessions=new HashSet<string>(StringComparer.Ordinal);
        private string activeWorld,activeSession;
        private string frozenOutcome;
        private bool begin,end,terminal,returned;
        public string LastFailure { get; private set; }
        public SkirmishProductionBatchContract(string candidate,string build,string manifestFileHash,
            IEnumerable<string> baselineWorlds,string scenes,int previews)
        {
            Candidate=candidate;Build=build;ManifestFileHash=manifestFileHash;
            BaselineWorlds=baselineWorlds.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            BaselineScenes=scenes;BaselinePreviewCount=previews;
        }
        public void Start(string candidate,string build)
        {
            Require(Current==Phase.Idle||Current==Phase.Cleaned,"Batch cannot start before clean completion.");
            Require(candidate==Candidate&&build==Build,"Frozen candidate/build differs between jobs.");
            activeWorld=activeSession=frozenOutcome=null;begin=end=terminal=returned=false;Current=Phase.Running;
        }
        public void Fingerprint(string endpoint,string actualSource,string actualBuild,string actualManifestFileHash)
        {
            Require(Current==Phase.Running,"Fingerprint outside an owned native job.");
            Require(actualSource==Candidate&&actualBuild==Build&&actualManifestFileHash==ManifestFileHash,
                "Native source, DLL or shared manifest changed.");
            if(endpoint=="Begin"){Require(!begin,"Duplicate native Begin.");begin=true;}
            else {Require(endpoint=="End"&&begin&&!end,"Missing Begin or duplicate native End.");end=true;}
        }
        public void NativeSnapshot(string world,string session,string phase,string outcome,bool frozen)
        {
            Require(Current==Phase.Running&&begin&&!end,"Native facts outside fingerprinted job.");
            Require(!string.IsNullOrWhiteSpace(world)&&!string.IsNullOrWhiteSpace(session),"Native world/session absent.");
            if(activeWorld==null)
            {
                Require(!BaselineWorlds.Contains(world)&&!previousWorlds.Contains(world),"Native world was reused between jobs.");
                Require(!previousSessions.Contains(session),"Native session identity was reused between jobs.");
                activeWorld=world;activeSession=session;
            }
            Require(world==activeWorld&&session==activeSession,"Native session or world spliced.");
            if(terminal)Require(phase=="Finished"&&frozen&&outcome==frozenOutcome,"Native phase/outcome changed after frozen terminal.");
            if(phase=="Finished")
            {
                Require(frozen&&new[]{"Victory","Defeat","Draw"}.Contains(outcome),"Terminal facts are not an actual frozen outcome.");
                terminal=true;frozenOutcome=outcome;
            }
        }
        public void Return(string session,string route,bool transitioning,bool nativeSessionPresent,string presentedControl,string action)
        {
            Require(Current==Phase.Running&&terminal&&!end&&session==activeSession,"Return is not bound to terminal session.");
            Require(route=="MainMenu"&&!transitioning&&!nativeSessionPresent&&!string.IsNullOrWhiteSpace(presentedControl)&&action=="MAIN MENU",
                "Normal public Main Menu return is incomplete.");returned=true;
        }
        public void Finish(int actualOwnerExitCode)
        {
            Require(Current==Phase.Running&&actualOwnerExitCode==0&&begin&&end&&terminal&&returned,
                "Owned native finish/return/fingerprints incomplete or failed.");Current=Phase.NativeFinished;
        }
        public void Cleanup(IEnumerable<string> actualWorlds,string actualScenes,int actualPreviews)
        {
            Require(Current==Phase.NativeFinished,"Cleanup cannot authorize continuation before native finish.");
            Require(actualWorlds.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(BaselineWorlds)&&
                actualScenes==BaselineScenes&&actualPreviews==BaselinePreviewCount,
                "Native worlds, scenes or preview scenes leaked/changed; batch cannot continue.");
            previousWorlds.Add(activeWorld);previousSessions.Add(activeSession);Current=Phase.Cleaned;
        }
        public void Fail(string reason){Current=Phase.Failed;LastFailure=reason;}
        private void Require(bool value,string reason)
        {
            if(value)return;Fail(reason);throw new InvalidDataException(reason);
        }
    }
}
