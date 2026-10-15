using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Runtime
{
    // Exact BA7 field order from 81569142d. NonBA8 and its managed clones retain
    // their existing shape. Parse only after the outer raw checksum is checked.
    internal static class SkirmishNativeLegacyBa7Json
    {
        [Serializable] private sealed class LegacySnapshotV7
        {
            public int Version = 7;
            public string SessionId;
            public string CatalogId;
            public uint SetupHash;
            public string ContentHash;
            public string OperationMapId;
            public string LayoutId;
            public string MapMetadataHash;
            public double WorldTime;
            public float UnityTime;
            public int Frame;
            public SkirmishNativeCheckpointTransientState.Visual[] MissileVisuals = Array.Empty<SkirmishNativeCheckpointTransientState.Visual>();
            public SkirmishNativeCheckpointTransientState.Transient[] MissileStates = Array.Empty<SkirmishNativeCheckpointTransientState.Transient>();
            public SkirmishNativeCheckpointTransientState.DropVisual[] DropVisuals = Array.Empty<SkirmishNativeCheckpointTransientState.DropVisual>();
            public SkirmishNativeCheckpointSupportState.Record[] Support = Array.Empty<SkirmishNativeCheckpointSupportState.Record>();
            public SkirmishNativeCheckpointSnapshotService.Actor[] Actors;
            public SkirmishNativeCheckpointSnapshotService.MapReference[] MapReferences;
            public SkirmishNativeCheckpointSnapshotService.Bank[] Banks;
            public SkirmishNativeCheckpointParts.Part[] SessionParts;
        }
        [Serializable] private sealed class Header {public int Version;}
        internal static string EncodeInner(SkirmishNativeCheckpointSnapshotService.Snapshot value)
            =>value==null?null:value.Version==7?Encode(value):value.Version==8?JsonUtility.ToJson(value):null;
        internal static bool TryDecodeInner(string raw,out SkirmishNativeCheckpointSnapshotService.Snapshot snapshot)
        {
            snapshot=null;if(string.IsNullOrEmpty(raw))return false;
            var header=JsonUtility.FromJson<Header>(raw);if(header==null)return false;
            if(header.Version==7)return TryDecode(raw,out snapshot);
            if(header.Version!=8)return false;
            snapshot=JsonUtility.FromJson<SkirmishNativeCheckpointSnapshotService.Snapshot>(raw);
            return snapshot!=null&&snapshot.Version==8;
        }
        internal static string Encode(SkirmishNativeCheckpointSnapshotService.Snapshot value)
        {
            if(value==null||value.Version!=7||value.Objective!=null)return null;
            return JsonUtility.ToJson(new LegacySnapshotV7 {
                Version=value.Version,
                SessionId=value.SessionId,
                CatalogId=value.CatalogId,
                SetupHash=value.SetupHash,
                ContentHash=value.ContentHash,
                OperationMapId=value.OperationMapId,
                LayoutId=value.LayoutId,
                MapMetadataHash=value.MapMetadataHash,
                WorldTime=value.WorldTime,
                UnityTime=value.UnityTime,
                Frame=value.Frame,
                MissileVisuals=value.MissileVisuals,
                MissileStates=value.MissileStates,
                DropVisuals=value.DropVisuals,
                Support=value.Support,
                Actors=value.Actors,
                MapReferences=value.MapReferences,
                Banks=value.Banks,
                SessionParts=value.SessionParts
            });
        }
        internal static bool TryDecode(string raw,out SkirmishNativeCheckpointSnapshotService.Snapshot snapshot)
        {
            snapshot=null;
            if(!HasOnlyLegacyTopLevelKeys(raw))return false;
            var value=JsonUtility.FromJson<LegacySnapshotV7>(raw);
            if(value==null||value.Version!=7)return false;
            snapshot=new SkirmishNativeCheckpointSnapshotService.Snapshot {
                Version=value.Version,
                SessionId=value.SessionId,
                CatalogId=value.CatalogId,
                SetupHash=value.SetupHash,
                ContentHash=value.ContentHash,
                OperationMapId=value.OperationMapId,
                LayoutId=value.LayoutId,
                MapMetadataHash=value.MapMetadataHash,
                WorldTime=value.WorldTime,
                UnityTime=value.UnityTime,
                Frame=value.Frame,
                MissileVisuals=value.MissileVisuals,
                MissileStates=value.MissileStates,
                DropVisuals=value.DropVisuals,
                Support=value.Support,
                Actors=value.Actors,
                MapReferences=value.MapReferences,
                Banks=value.Banks,
                SessionParts=value.SessionParts,
                Objective=null,Repairs=null
            };
            return true;
        }
        // Bound the structural scan; JsonUtility still parses actual typed values.
        // Escaped keys are decoded; nested strings/keys cannot masquerade as root keys.
        internal static bool HasOnlyLegacyTopLevelKeys(string raw)
        {
            if(string.IsNullOrEmpty(raw))return false;
            try
            {
                int at=0;var keys=new HashSet<string>(StringComparer.Ordinal);
                Space(raw,ref at);Object(raw,ref at,0,keys);Space(raw,ref at);
                return at==raw.Length&&!keys.Contains("Objective")&&!keys.Contains("Repairs")&&keys.Contains("Version");
            }
            catch(FormatException){return false;}
        }
        static void Space(string s,ref int p)
        {while(p<s.Length&&(s[p]==' '||s[p]=='\t'||s[p]=='\r'||s[p]=='\n'))p++;}
        static void Token(string s,ref int p,char token)
        {Space(s,ref p);if(p>=s.Length||s[p++]!=token)throw new FormatException();}
        static string String(string s,ref int p)
        {
            Token(s,ref p,'"');var text=new StringBuilder();
            while(p<s.Length)
            {
                char c=s[p++];if(c=='"')return text.ToString();if(c<32)throw new FormatException();
                if(c!='\\'){text.Append(c);continue;}
                if(p==s.Length)throw new FormatException();char escape=s[p++];
                switch(escape)
                {
                    case '"':text.Append('"');break;case '\\':text.Append('\\');break;case '/':text.Append('/');break;
                    case 'b':text.Append('\b');break;case 'f':text.Append('\f');break;
                    case 'n':text.Append('\n');break;case 'r':text.Append('\r');break;case 't':text.Append('\t');break;
                    case 'u':
                        if(p+4>s.Length||!ushort.TryParse(s.Substring(p,4),System.Globalization.NumberStyles.AllowHexSpecifier,
                            System.Globalization.CultureInfo.InvariantCulture,out ushort unit))throw new FormatException();
                        text.Append((char)unit);p+=4;break;
                    default:throw new FormatException();
                }
            }
            throw new FormatException();
        }
        static void Object(string s,ref int p,int depth,HashSet<string> rootKeys)
        {
            if(depth>64)throw new FormatException();Token(s,ref p,'{');Space(s,ref p);
            if(p<s.Length&&s[p]=='}'){p++;return;}
            var keys=new HashSet<string>(StringComparer.Ordinal);
            while(true)
            {
                string key=String(s,ref p);if(!keys.Add(key))throw new FormatException();
                if(depth==0)rootKeys.Add(key);Token(s,ref p,':');Value(s,ref p,depth+1,rootKeys);Space(s,ref p);
                if(p>=s.Length)throw new FormatException();char next=s[p++];if(next=='}')return;if(next!=',')throw new FormatException();
            }
        }
        static void Value(string s,ref int p,int depth,HashSet<string> rootKeys)
        {
            if(depth>64)throw new FormatException();Space(s,ref p);if(p==s.Length)throw new FormatException();
            if(s[p]=='"'){String(s,ref p);return;}
            if(s[p]=='{'){Object(s,ref p,depth,rootKeys);return;}
            if(s[p]=='[')
            {
                p++;Space(s,ref p);if(p<s.Length&&s[p]==']'){p++;return;}
                while(true)
                {
                    Value(s,ref p,depth+1,rootKeys);Space(s,ref p);if(p==s.Length)throw new FormatException();
                    char next=s[p++];if(next==']')return;if(next!=',')throw new FormatException();
                }
            }
            int start=p;while(p<s.Length&&s[p]!=','&&s[p]!='}'&&s[p]!=']'&&!char.IsWhiteSpace(s[p]))p++;
            string token=s.Substring(start,p-start);
            if(token!="null"&&token!="true"&&token!="false"&&!Regex.IsMatch(token,@"\A-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?\z"))
                throw new FormatException();
        }
    }
}
