#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Managed request/evidence owner only. No ambient selection, source loading,
        // persistent authoring, measurement, or readiness permission is introduced.
        internal sealed class RequestedLogicalSourceLayouts
        {
            const string Field="logicalSourceLayoutInput",JoinField="logicalSourceLayoutJoin";
            const int MaximumBytes=4*1024*1024;
            static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
            static readonly string[] InitialFields={"initialOwnedLayoutJson","initialOwnedLayoutRawSha256","initialOwnedLayoutSemanticSha256"};
            readonly string raw,rawSha,group;
            readonly JObject pins;
            readonly JObject[] entries;
            public JObject SourcePins=>(JObject)pins.DeepClone();

            RequestedLogicalSourceLayouts(byte[] bytes,string expectedSha)
            {
                Require(bytes!=null&&bytes.Length>0&&bytes.Length<=MaximumBytes&&Hex(expectedSha)&&Digest(bytes)==expectedSha,"Bounded SHA-pinned UTF8 logical input required");
                raw=Utf8.GetString(bytes);rawSha=expectedSha;
                var input=ObjectJson(raw);
                Require(input["schemaVersion"]?.Type==JTokenType.Integer&&(int)input["schemaVersion"]==1,"Logical input schemaVersion1 required");
                pins=(JObject)(input["sourcePins"] as JObject)?.DeepClone();Require(pins!=null,"Exact sourcePins object required");ValidatePins(pins);
                var rows=input["missions"] as JArray;Require(rows!=null&&rows.Count>0&&rows.Count<=48&&rows.All(x=>x is JObject),"Bounded explicit mission objects required");
                entries=rows.OfType<JObject>().Select(x=>(JObject)x.DeepClone()).ToArray();
                var ids=new HashSet<string>(StringComparer.Ordinal);string sameGroup=null;
                foreach(var entry in entries)
                {
                    var authority=entry["authority"] as JObject;RequireLogicalGroundBaMission(authority);
                    string id=(string)authority["catalog_id"],current=(string)authority["tuple"]["map_id"];
                    Require(ids.Add(id)&&new[]{"CC","AP","MP","IB"}.Contains(current),"Unique logical requested group/catalog required");
                    if(sameGroup==null)sameGroup=current;else Require(sameGroup==current,"One logical source group required");
                    Require(entry["layoutJson"]?.Type==JTokenType.String&&Hex((string)entry["layoutSemanticSha256"]),"Original full typed layout JSON/semantic SHA required");
                    ValidateTemplate(entry,ObjectJson((string)entry["layoutJson"]));
                }
                group=sameGroup;
            }

            internal static RequestedLogicalSourceLayouts FromRaw(byte[] rawUtf8,string sha256)=>new RequestedLogicalSourceLayouts(rawUtf8,sha256);
            internal static RequestedLogicalSourceLayouts ReadOptional(string path,string sha256)
            {
                if(string.IsNullOrEmpty(path)&&string.IsNullOrEmpty(sha256))return null;
                Require(!string.IsNullOrEmpty(path)&&Hex(sha256),"Optional logical input path and SHA must be supplied together");
                var before=new FileInfo(path);before.Refresh();
                Require(before.Exists&&(before.Attributes&(FileAttributes.Directory|FileAttributes.ReparsePoint|FileAttributes.Device))==0&&before.Length>0&&before.Length<=MaximumBytes,
                    "Positive bounded regular logical input file required before open");
                long length=before.Length;var attributes=before.Attributes;var created=before.CreationTimeUtc;var written=before.LastWriteTimeUtc;
                using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
                Require(file.CanSeek&&file.Length==length,"Seekable unchanged logical input descriptor required");
                using var bytes=new MemoryStream();var buffer=new byte[65536];int count;
                while((count=file.Read(buffer,0,Math.Min(buffer.Length,MaximumBytes+1-(int)bytes.Length)))>0)
                {bytes.Write(buffer,0,count);Require(bytes.Length<=MaximumBytes,"Logical input exceeds bound");}
                var after=new FileInfo(path);after.Refresh();
                Require(bytes.Length==length&&file.Length==length&&after.Exists&&after.Attributes==attributes&&after.Length==length&&after.CreationTimeUtc==created&&after.LastWriteTimeUtc==written,
                    "Logical input changed during bounded read");
                return FromRaw(bytes.ToArray(),sha256);
            }

            internal JObject[] ResolveRequestedRows(JArray authority,string requestedGroup,string requestedIds)
            {
                Require(authority!=null&&!string.IsNullOrWhiteSpace(requestedIds)&&requestedGroup==group,"Exact logical requested group required");
                var ids=requestedIds.Split(',').Select(x=>x.Trim()).ToArray();
                Require(ids.SequenceEqual(entries.Select(x=>(string)x["authority"]["catalog_id"])),"Input/requested catalog order must agree exactly");
                return entries.Select(entry=>
                {
                    string id=(string)entry["authority"]["catalog_id"];
                    var matches=authority.OfType<JObject>().Where(x=>(string)x["catalog_id"]==id).ToArray();
                    Require(matches.Length==1&&JToken.DeepEquals(matches[0],entry["authority"]),"Caller authority differs from exact immutable mission");
                    return(JObject)matches[0].DeepClone();
                }).ToArray();
            }

            JObject Entry(JObject authority)
            {
                RequireLogicalGroundBaMission(authority);
                var matches=entries.Where(x=>(string)x["authority"]["catalog_id"]==(string)authority["catalog_id"]).ToArray();
                Require(matches.Length==1&&JToken.DeepEquals(matches[0]["authority"],authority),"Exact recorded mission authority required");return matches[0];
            }

            internal void Resolve(JObject authority,out JObject candidate,out JObject template)
            {
                var entry=Entry(authority);candidate=(JObject)authority.DeepClone();
                template=(JObject)authority.DeepClone();
                template["layout_input"]=entry["layout_input"].DeepClone();template["source_candidate"]=pins.DeepClone();template["collector_source_group"]=group;
            }

            internal bool CreateCandidate(JObject authority,OperationMapDefinition source,out GroundObjective30CandidateOwner owner,out string error)
            {
                owner=null;error=null;SkirmishMapLayoutConfig layout=null;
                try
                {
                    var entry=Entry(authority);layout=ReadLayout((string)entry["layoutJson"],(string)entry["layoutSemanticSha256"]);
                    Require(TryCreateGroundBaLogicalSourceLayoutInput(source,layout,pins,(string)entry["layoutSemanticSha256"],out var input,out error),error);
                    using(input)return TryCreateGroundBa10Candidate(authority,source,input,out owner,out error);
                }
                catch(Exception failure){error=failure.ToString();return false;}
                finally{if(layout!=null)UnityEngine.Object.DestroyImmediate(layout);}
            }

            internal JObject Record()=>new JObject{["schemaVersion"]=1,["rawJson"]=raw,["rawSha256"]=rawSha,["sourcePins"]=pins.DeepClone()};
            internal JObject RecordFor(JObject authority,SkirmishMapLayoutConfig initialOwnedLayout)
            {
                var entry=Entry(authority);var record=Record();record["catalogId"]=authority["catalog_id"].DeepClone();
                record["layoutJson"]=entry["layoutJson"].DeepClone();record["layoutRawSha256"]=Digest((string)entry["layoutJson"]);record["layoutSemanticSha256"]=entry["layoutSemanticSha256"].DeepClone();
                foreach(string name in InitialFields)record[name]=null;
                if(initialOwnedLayout!=null)
                {
                    VerifyIdentityClone(entry,initialOwnedLayout);string json=JsonUtility.ToJson(initialOwnedLayout);
                    record[InitialFields[0]]=json;record[InitialFields[1]]=Digest(json);record[InitialFields[2]]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(initialOwnedLayout);
                }
                return record;
            }

            internal static RequestedLogicalSourceLayouts FromReport(JObject report,JObject row,JObject authority)
            {
                var global=report?[Field];var mission=row?[Field];var join=row?["measurement"]?[JoinField];
                if(global==null&&mission==null&&join==null)return null;
                Require(global is JObject&&mission is JObject&&join is JObject,"Complete recorded global/mission input and measurement join required");
                var input=FromRecord((JObject)global);Require(Equivalent(global,input.Record()),"Global input record differs from its pinned raw JSON");
                var expected=input.RecordFor(authority,null);var supplied=(JObject)mission.DeepClone();
                foreach(string name in InitialFields){Require(supplied[name]?.Type==JTokenType.String,"Successful mission needs recorded initial owned layout");supplied[name]=null;}
                Require(Equivalent(expected,supplied),"Global/mission raw input, source pins and original layout records differ");
                VerifyMeasurementJoin((JObject)mission,(JObject)row["measurement"]);return input;
            }

            internal static void VerifyReconstructedLayout(JObject record,SkirmishMapLayoutConfig initialOwnedLayout)
            {
                Require(initialOwnedLayout!=null,"Reconstructed initial owned layout required");var input=FromRecord(record);var authority=input.Authority(record);var expected=input.RecordFor(authority,initialOwnedLayout);
                Require(Equivalent(expected,record),"Reconstructed initial owned layout differs from recorded exact input clone");
            }

            internal static void AttachMeasurementJoin(JObject record,JObject measurement)
            {
                Require(measurement!=null&&measurement[JoinField]==null,"New measurement join required");measurement[JoinField]=BuildJoin(record,measurement);
            }
            internal static void VerifyMeasurementJoin(JObject record,JObject measurement)
            {Require(measurement?[JoinField] is JObject&&Equivalent(measurement[JoinField],BuildJoin(record,measurement)),"Exact original/initial/final layout and authored binding graph join required");}

            static JObject BuildJoin(JObject record,JObject measurement)
            {
                var input=FromRecord(record);var authority=input.Authority(record);string initial=String(record,InitialFields[0]);
                var layout=ReadLayout(initial,String(record,InitialFields[2]));try{VerifyReconstructedLayout(record,layout);}finally{UnityEngine.Object.DestroyImmediate(layout);}
                Require(Equivalent(measurement["sourcePins"],record["sourcePins"]),"Measured source pins differ from original input");
                string final=String(measurement,"proposedLayoutJson"),semantic=String(measurement,"proposedLayoutSemanticSha256");
                layout=ReadLayout(final,semantic);UnityEngine.Object.DestroyImmediate(layout);
                Require(measurement["authoredDefinitionLogicalBindingGraph"] is JObject,"Actual authored logical binding graph required");
                string graph=Canonical(measurement["authoredDefinitionLogicalBindingGraph"]);
                return new JObject{["schemaVersion"]=1,["catalogId"]=authority["catalog_id"].DeepClone(),["inputRawSha256"]=record["rawSha256"].DeepClone(),
                    ["originalLayoutRawSha256"]=record["layoutRawSha256"].DeepClone(),["initialOwnedLayoutRawSha256"]=record[InitialFields[1]].DeepClone(),["initialOwnedLayoutSemanticSha256"]=record[InitialFields[2]].DeepClone(),
                    ["sourcePinsSha256"]=Digest(Canonical(record["sourcePins"])),["finalLayoutRawSha256"]=Digest(final),["finalLayoutSemanticSha256"]=semantic,
                    ["authoredDefinitionLogicalBindingGraphRawJson"]=graph,["authoredDefinitionLogicalBindingGraphRawSha256"]=Digest(graph)};
            }

            JObject Authority(JObject record)
            {var matches=entries.Where(x=>(string)x["authority"]["catalog_id"]==(string)record?["catalogId"]).ToArray();Require(matches.Length==1,"Recorded mission catalog required");return(JObject)matches[0]["authority"].DeepClone();}
            static RequestedLogicalSourceLayouts FromRecord(JObject record)
            {Require(record!=null,"Recorded input required");return FromRaw(Utf8.GetBytes(String(record,"rawJson")),String(record,"rawSha256"));}
            static void VerifyIdentityClone(JObject entry,SkirmishMapLayoutConfig initial)
            {
                var original=ReadLayout((string)entry["layoutJson"],(string)entry["layoutSemanticSha256"]);
                try
                {var expected=ObjectJson(JsonUtility.ToJson(original));expected["layoutId"]="layout."+((string)entry["authority"]["catalog_id"]).ToLowerInvariant()+".measured-pad-candidate.v2";
                    Require(Equivalent(expected,ObjectJson(JsonUtility.ToJson(initial))),"Initial owned layout must be the entire original recipe with only canonical candidate identity changed");}
                finally{UnityEngine.Object.DestroyImmediate(original);}
            }
            static SkirmishMapLayoutConfig ReadLayout(string json,string semantic)
            {
                Require(Hex(semantic),"Exact layout semantic SHA required");var layout=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
                try{ObjectJson(json);JsonUtility.FromJsonOverwrite(json,layout);Require(SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout)==semantic,"Typed layout semantic SHA mismatch");return layout;}
                catch{UnityEngine.Object.DestroyImmediate(layout);throw;}
            }

            static void ValidatePins(JObject source)
            {
                foreach(string key in new[]{"sourceIdentityHash","contentHash","sourceAssetSha256","gridAssetSha256","surfaceAssetSha256","sourceSemanticFingerprint"})Require(Hex(String(source,key)),"Exact source hash required: "+key);
                foreach(string key in new[]{"sourceSceneGuid","authoredSubSceneGuid","gridAssetGuid","surfaceAssetGuid"}){string value=String(source,key);Require(value.Length==32&&value.All(x=>x>='0'&&x<='9'||x>='a'&&x<='f'),"Exact source GUID required: "+key);}
                foreach(string key in new[]{"sourcePath","gridPath","surfacePath"}){string value=String(source,key);Require(value.StartsWith("Assets/",StringComparison.Ordinal)&&!value.Contains("\\")&&!value.Split('/').Any(x=>x==".."||x=="."||x.Length==0),"Repository-relative source asset path required");}
                String(source,"operationMapId");
            }
            void ValidateTemplate(JObject entry,JObject layout)
            {
                Require((string)layout["operationMapId"]==(string)pins["operationMapId"],"Full typed layout physical source identity differs");
                var input=entry["layout_input"] as JObject;var pilot=input?["pilot_candidate"] as JObject;Require(pilot!=null,"Reviewed measurement layout_input/pilot_candidate required");
                var aliases=new Dictionary<string,string>{{"source_path","sourcePath"},{"source_asset_sha256","sourceAssetSha256"},{"grid_sha256","gridAssetSha256"},{"surface_sha256","surfaceAssetSha256"},{"source_identity","sourceIdentityHash"},{"content_hash","contentHash"}};
                foreach(var alias in aliases)if(pilot[alias.Key]!=null)Require(pilot[alias.Key].Type==JTokenType.String&&(string)pilot[alias.Key]==(string)pins[alias.Value],"Pilot source alias differs from actual source pins: "+alias.Key);
                foreach(var item in new[]{new[]{"catalog_id","catalog_id"},new[]{"canonical_budget_rows_sha256","canonical_size_rows_sha256"}})
                    if(pilot[item[0]]!=null)Require(Equivalent(pilot[item[0]],entry["authority"][item[1]]),"Pilot metadata differs from immutable authority");
                if(pilot["group"]!=null)Require((string)pilot["group"]==(string)entry["authority"]["tuple"]["map_id"],"Pilot logical group differs from authority");
                var compounds=layout["factionCompounds"] as JArray;Require(compounds!=null&&compounds.Count==2,"Full typed faction compounds required");
                var rows=pilot["typed_pad_world_axis_candidates"] as JArray;Require(rows!=null&&rows.Count>=2&&rows.Count<=256&&rows.All(x=>x is JObject),"Explicit bounded compound search rectangles required");
                foreach(var row in rows.OfType<JObject>())Require(row["faction"]?.Type==JTokenType.Integer&&((int)row["faction"]==1||(int)row["faction"]==2),"Exact search rectangle faction required");
                foreach(int faction in new[]{1,2})
                {
                    var own=compounds.OfType<JObject>().Where(x=>(int?)x["FactionId"]==faction).ToArray();Require(own.Length==1,"One exact authored world compound per faction required");
                    double[] bounds={Number(own[0]["MinX"]),Number(own[0]["MinZ"]),Number(own[0]["MaxX"]),Number(own[0]["MaxZ"])};
                    var rects=rows.OfType<JObject>().Where(x=>(int)x["faction"]==faction).Select(x=>Rect(x["world_rect"])).ToArray();Require(rects.Length>0,"Both faction compound rectangles required");
                    Require(bounds[0]<bounds[2]&&bounds[1]<bounds[3]&&rects.All(x=>x[0]>=bounds[0]&&x[1]>=bounds[1]&&x[2]<=bounds[2]&&x[3]<=bounds[3])&&
                        rects.Min(x=>x[0])==bounds[0]&&rects.Min(x=>x[1])==bounds[1]&&rects.Max(x=>x[2])==bounds[2]&&rects.Max(x=>x[3])==bounds[3],"Search rectangle hull must equal actual authored faction compound");
                }
                double ox=Number(layout["originX"]),oz=Number(layout["originZ"]),width=Number(layout["worldWidthMetres"]),depth=Number(layout["worldDepthMetres"]);
                Require(width>0&&depth>0,"Positive typed logical envelope required");var envelope=new JArray(ox,oz,ox+width,oz+depth);
                foreach(var value in new[]{input["source_envelope_world"],pilot["source_envelope_world"],pilot["proposed_logical_world_rect"]})if(value!=null)Require(Rect(value).SequenceEqual(Rect(envelope)),"Reviewed source envelope differs from actual typed logical envelope");
                input["source_envelope_world"]=envelope;
            }
            static double[] Rect(JToken token){Require(token is JArray array&&array.Count==4,"Four finite world rectangle scalars required");var values=((JArray)token).Select(Number).ToArray();Require(values[0]<values[2]&&values[1]<values[3],"Ordered positive world rectangle required");return values;}
            static double Number(JToken token){Require(token!=null&&(token.Type==JTokenType.Integer||token.Type==JTokenType.Float),"Numeric geometry scalar required");double value=(double)token;Require(!double.IsNaN(value)&&!double.IsInfinity(value),"Finite geometry scalar required");return value;}
            static string String(JObject value,string name){Require(value?[name]?.Type==JTokenType.String&&!string.IsNullOrEmpty((string)value[name]),"Required recorded string: "+name);return(string)value[name];}
            static JObject ObjectJson(string text)
            {
                Require(text!=null&&Utf8.GetByteCount(text)<=MaximumBytes,"Bounded JSON object required");using var reader=new JsonTextReader(new StringReader(text.Length>0&&text[0]=='\ufeff'?text.Substring(1):text)){DateParseHandling=DateParseHandling.None,MaxDepth=64};
                var value=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});Require(!reader.Read(),"Additional JSON content rejected");return value;
            }
            static string Canonical(JToken token){Require(token!=null,"Required canonical evidence graph");return JToken.Parse(token.ToString(Formatting.None)).ToString(Formatting.None);}
            static bool Equivalent(JToken left,JToken right)=>left!=null&&right!=null&&JToken.DeepEquals(JToken.Parse(Canonical(left)),JToken.Parse(Canonical(right)));
            static bool Hex(string text)=>SkirmishMissionLogicalBindingCompiler.Hex(text);
            static string Digest(string text)=>Digest(Utf8.GetBytes(text));
            static string Digest(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        }
    }
}
#endif
