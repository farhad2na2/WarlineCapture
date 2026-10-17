#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Game.Editor
{
    // Admission only: a complete finite sampling plan is not gameplay evidence.
    public sealed class SkirmishProductionMatrixAuthority
    {
        const string Catalog="Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv";
        const string Rules="Design/Roadmap/Skirmish_Expansion/AI_AND_ARIA.md";
        readonly string root,path,hash;
        readonly Dictionary<string,JObject> slots=new Dictionary<string,JObject>(StringComparer.Ordinal);
        readonly List<string> slotOrder=new List<string>();
        readonly Dictionary<string,string[]> catalog=new Dictionary<string,string[]>(StringComparer.Ordinal);
        readonly Dictionary<string,string> sourcePins=new Dictionary<string,string>(StringComparer.Ordinal);
        public string Kind {get;}
        public int SlotCount=>slots.Count;
        public SkirmishProductionMatrixAuthority(string projectRoot,string planPath,string expectedHash)
        {
            root=Path.GetFullPath(projectRoot);path=Path.GetFullPath(planPath);hash=expectedHash;
            Need(Regex.IsMatch(hash??"","^[a-f0-9]{64}$")&&SkirmishProductionCanaryRequest.FileHash(path)==hash,"Sampling plan SHA differs");
            var plan=SkirmishProductionCanaryRequest.ReadObject(path);
            Exact(plan,"schema_version kind source_pins slots");
            Need((int?)plan["schema_version"]==1,"Unknown matrix sampling plan");Kind=(string)plan["kind"];
            Need(Kind=="Baseline720"||Kind=="Stress1200","Unknown finite matrix kind");
            foreach(var line in File.ReadAllLines(Resolve(Catalog)).Skip(1).Where(v=>!string.IsNullOrWhiteSpace(v)))
            {var fields=line.Split(',');Need(fields.Length>=5&&!catalog.ContainsKey(fields[0]),"Catalog tuple malformed");catalog.Add(fields[0],fields);}
            Need(catalog.Count==120&&catalog.Keys.OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(Enumerable.Range(1,120).Select(v=>"S"+v.ToString("D3"))),"Exact S001–S120 canonical catalog rows required");
            Need(catalog.Values.All(v=>new[]{"DB","CC","MP","IB","AP"}.Contains(v[1])&&new[]{"BA","FC","BT","CE"}.Contains(v[2])&&new[]{"G","A","C"}.Contains(v[3])&&new[]{"F","E"}.Contains(v[4]))&&
                catalog.Values.Select(v=>Family(v)+"|"+v[4]).Distinct(StringComparer.Ordinal).Count()==120,"Exact five maps/four objectives/three armies/two starts required");
            foreach(JObject pin in (JArray)plan["source_pins"])
            {Exact(pin,"path sha256");string key=(string)pin["path"],value=(string)pin["sha256"];Need(!sourcePins.ContainsKey(key)&&Regex.IsMatch(value??"","^[a-f0-9]{64}$"),"Duplicate/invalid plan authority");sourcePins.Add(key,value);}
            var expectedPins=new HashSet<string>(catalog.Values.Select(v=>Packet(v)),StringComparer.Ordinal){Catalog,Rules};
            Need(expectedPins.SetEquals(sourcePins.Keys),"Exact catalog/rules/twenty packet authority pins required");Recheck();
            foreach(JObject slot in (JArray)plan["slots"])
            {
                Exact(slot,"catalog_id seed locale size difficulty control_mode");
                Need(catalog.ContainsKey((string)slot["catalog_id"])&&slot["seed"]?.Type==JTokenType.Integer&&(long)slot["seed"]>0&&(long)slot["seed"]<=int.MaxValue,"Invalid canonical slot");
                Need((string)slot["size"]=="Standard"&&(string)slot["difficulty"]=="Regular"&&(string)slot["control_mode"]=="aria"&&new[]{"EN","FA"}.Contains((string)slot["locale"]),"Only normal baseline/stress ARIA Regular Standard admitted");
                Need(!slots.ContainsKey(Key(slot)),"Duplicate sampling slot");slots.Add(Key(slot),(JObject)slot.DeepClone());slotOrder.Add(Key(slot));
            }
            if(Kind=="Baseline720")
            {
                Need(slots.Count==720,"Exact720 baseline slots required");
                foreach(var item in catalog)
                {
                    string text=File.ReadAllText(Resolve(Packet(item.Value)));
                    var section=Regex.Match(text,"^## "+item.Key+@"\s*$([\s\S]*?)(?=^## |\z)",RegexOptions.Multiline);
                    var seeds=Regex.Match(section.Groups[1].Value,@"Regular seeds ((?:`\d+`[ ,and]*)+) in both EN and FA at Standard");
                    var values=Regex.Matches(seeds.Groups[1].Value,@"`(\d+)`").Cast<Match>().Select(v=>int.Parse(v.Groups[1].Value)).ToArray();
                    Need(section.Success&&seeds.Success&&values.Length==3&&values.Distinct().Count()==3,"Packet baseline seeds absent");
                    foreach(int seed in values)foreach(string locale in new[]{"EN","FA"})
                        Need(slots.ContainsKey(item.Key+"|"+seed+"|"+locale),"Baseline must contain every exact packet seed/locale");
                }
            }
            else
            {
                Need(slots.Count==1200,"Exact1200 stress slots required; do not infer stress from baseline runs");
                var groups=((JArray)plan["slots"]).OfType<JObject>().GroupBy(v=>Family(catalog[(string)v["catalog_id"]])).ToArray();
                Need(groups.Length==60,"Exact sixty map/objective/army strata required");
                foreach(var group in groups)
                {
                    var rows=group.ToArray();Need(rows.Length==20&&rows.Select(v=>(int)v["seed"]).Distinct().Count()==20,"Twenty distinct stress seeds per stratum required");
                    for(int i=0;i<20;i++)Need(catalog[(string)rows[i]["catalog_id"]][4]==(i%2==0?"F":"E"),"Stress plan must alternate actual Field/Established catalog starts");
                }
            }
        }
        public void ValidateJob(JObject job)
        {
            Recheck();string id=(string)job["catalog_id"];Need(catalog.ContainsKey(id),"Job catalog outside canonical plan");Need(slots.TryGetValue(Key(job),out var slot),"Job is outside the exact reviewed finite plan");
            foreach(string field in new[]{"catalog_id","seed","locale","size","difficulty","control_mode"})Need(JToken.DeepEquals(slot[field],job[field]),"Job sampling flag differs: "+field);
            string packet=Packet(catalog[id]);Need((string)job["packet_path"]==packet&&(string)job["packet_sha256"]==sourcePins[packet],"Canonical family packet differs");
        }
        public void ValidateBatch(IEnumerable<JObject> jobs)
        {
            var selected=jobs.ToArray();Need(selected.Length>0&&selected.Length<=1200,"Finite nonempty batch required");
            foreach(var job in selected)ValidateJob(job);
            var keys=selected.Select(Key).ToArray();Need(keys.Distinct(StringComparer.Ordinal).Count()==keys.Length,"Duplicate sampling slot in batch");
            if(Kind=="Stress1200")
            {
                var order=slotOrder.Select((key,index)=>new{key,index}).ToDictionary(v=>v.key,v=>v.index,StringComparer.Ordinal);
                for(int i=1;i<keys.Length;i++)Need(order[keys[i]]==order[keys[i-1]]+1,"Stress dispatch must be a contiguous ordered plan slice, preserving alternating starts");
            }
        }
        public static bool IsOperationallyComplete(in Game.Configs.SkirmishS002AriaTerminalFacts facts,bool appended,bool nativeTerminalSnapshot)
        {
            return appended&&nativeTerminalSnapshot&&facts.MatchFinished&&!facts.Aborted&&!facts.ForcedVictory&&
                new[]{"Victory","Defeat","Draw"}.Contains(facts.MatchOutcome)&&facts.NormalSpeed&&
                facts.InputViolations==0&&facts.HumanInterventions==0&&facts.DefinitionVersion>0&&
                Regex.IsMatch(facts.CodeHash??"","^[a-f0-9]{64}$")&&Regex.IsMatch(facts.ConfigHash??"","^[a-f0-9]{64}$");
        }
        public void Recheck()
        {Need(SkirmishProductionCanaryRequest.FileHash(path)==hash,"Sampling plan changed");foreach(var pin in sourcePins)Need(SkirmishProductionCanaryRequest.FileHash(Resolve(pin.Key))==pin.Value,"Sampling authority changed: "+pin.Key);}
        string Resolve(string relative){Need(!Path.IsPathRooted(relative)&&!relative.Split('/').Contains(".."),"Unsafe authority path");return Path.Combine(root,relative);}
        static string Packet(string[] tuple)=>"Design/Roadmap/Skirmish_Expansion/Scenarios/"+tuple[1]+"_"+tuple[2]+".md";
        static string Family(string[] tuple)=>tuple[1]+"|"+tuple[2]+"|"+tuple[3];
        static string Key(JObject row)=>(string)row["catalog_id"]+"|"+(int)row["seed"]+"|"+(string)row["locale"];
        static void Need(bool value,string error){if(!value)throw new InvalidDataException(error);}
        static void Exact(JObject row,string fields){Need(row!=null&&row.Properties().Select(v=>v.Name).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(fields.Split(' ').OrderBy(v=>v,StringComparer.Ordinal)),"Unknown/missing sampling fields");}
    }
}
#endif
