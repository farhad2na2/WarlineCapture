#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static bool refinementLogicalFrameSearchRequested;
        static readonly float[] RefinementLogicalFrameMargins={0,32,64,128};
        static bool ProtectedRefinementCatalog(string id)=>new[]{"S001","S002","S003","S004","S005","S025","S073"}.Contains(id);

        public static void RunAll120BoundedLogicalFrameRefinement()
        {
            Require(!refinementLogicalFrameSearchRequested&&!refinementCompoundSearchRequested&&
                !EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating,
                "Fresh idle bounded logical-frame authoring required");
            string id=Environment.GetEnvironmentVariable("WARLINE_ALL120_REFINEMENT_CATALOG");
            Require(!string.IsNullOrEmpty(id)&&!ProtectedRefinementCatalog(id),"One unprotected catalog required; coded bindings remain unchanged");
            RunBoundedLogicalFrameControls();
            refinementLogicalFrameSearchRequested=refinementCompoundSearchRequested=true;
            try{RunAll120SimultaneousLogicalRefinement();}
            finally{refinementLogicalFrameSearchRequested=refinementCompoundSearchRequested=false;}
        }

        static bool TryNextRefinementLogicalFrame(string originalJson,JObject originalCompounds,int attempt,
            SkirmishMapLayoutConfig current,OperationMapDefinition source,GridConfig grid,out JObject compounds,out string error)
        {
            compounds=null;error=null;
            if(attempt<1||attempt>=RefinementLogicalFrameMargins.Length){error="Finite current/32/64/128 logical frames exhausted";return false;}
            var original=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            var proposed=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                JsonUtility.FromJsonOverwrite(originalJson,original);
                Require(source!=null&&original.OperationMapId==source.OperationMapId&&original.WorldBinding.SourceHash==source.ContentHash,
                    "Exact original Campaign source binding required");
                var oldRect=new Vector4(original.OriginX,original.OriginZ,original.OriginX+original.WorldWidthMetres,original.OriginZ+original.WorldDepthMetres);
                var bounds=new Vector4(math.max(source.Bounds.PlayableMin.x,grid.Origin.x),math.max(source.Bounds.PlayableMin.z,grid.Origin.z),
                    math.min(source.Bounds.PlayableMax.x,grid.Origin.x+grid.Width*grid.CellSize),
                    math.min(source.Bounds.PlayableMax.z,grid.Origin.z+grid.Height*grid.CellSize));
                float margin=RefinementLogicalFrameMargins[attempt];
                if(!TryBoundedLogicalFrameRect(oldRect,bounds,margin,out var rect,out error))return false;
                if(math.abs(current.OriginX-rect.x)<.0001f&&math.abs(current.OriginZ-rect.y)<.0001f&&
                    math.abs(current.WorldWidthMetres-(rect.z-rect.x))<.0001f&&math.abs(current.WorldDepthMetres-(rect.w-rect.y))<.0001f)
                {error="No additional logical frame inside exact physical playable/grid bounds";return false;}
                if(!TryExpandRefinementRectangles(originalCompounds,margin,rect,out compounds,out error))return false;
                string next=EncodeWidenedRefinementLayout(original,rect);
                JsonUtility.FromJsonOverwrite(next,proposed);
                RequireLogicalFrameWorldEquivalence(original,proposed);
                JsonUtility.FromJsonOverwrite(next,current);
                RequireLogicalFrameWorldEquivalence(original,current);
                return true;
            }
            finally{UnityEngine.Object.DestroyImmediate(proposed);UnityEngine.Object.DestroyImmediate(original);}
        }

        static bool TryBoundedLogicalFrameRect(Vector4 original,Vector4 allowed,float margin,out Vector4 result,out string error)
        {
            result=default;error=null;
            if(!math.all(math.isfinite(new float4(original.x,original.y,original.z,original.w)))||
                !math.all(math.isfinite(new float4(allowed.x,allowed.y,allowed.z,allowed.w)))||!float.IsFinite(margin)||margin<=0||
                original.z<=original.x||original.w<=original.y||allowed.z<=allowed.x||allowed.w<=allowed.y||
                original.x<allowed.x-.001f||original.y<allowed.y-.001f||original.z>allowed.z+.001f||original.w>allowed.w+.001f)
            {error="Finite original frame wholly inside exact Campaign playable/grid required";return false;}
            result=new Vector4(math.max(allowed.x,original.x-margin),math.max(allowed.y,original.y-margin),
                math.min(allowed.z,original.z+margin),math.min(allowed.w,original.w+margin));
            if(result==original){error="Physical source/grid permits no wider frame";return false;}
            return true;
        }

        static string EncodeWidenedRefinementLayout(SkirmishMapLayoutConfig original,Vector4 rect)
        {
            Require(original.HasWorldBinding&&!original.WorldBinding.HasMeasuredDeployments&&!original.WorldBinding.BindAllConfigurations,
                "Only unqualified ephemeral authoring layout may widen");
            // This is the same source-axis metre convention used by MaterializePilotClone.
            // Refuse scaled/rotated input instead of silently changing pad dimensions.
            foreach(var point in new[]{new Vector2(original.OriginX,original.OriginZ),new Vector2(original.OriginX+1,original.OriginZ),
                new Vector2(original.OriginX,original.OriginZ+1)})
                Require(original.TryProjectLocalToMap(point.x,point.y,out float x,out float z)&&
                    math.abs(x-point.x)<.002f&&math.abs(z-point.y)<.002f,"Exact source-axis metre frame required");
            var data=JObject.Parse(JsonUtility.ToJson(original));
            float width=rect.z-rect.x,depth=rect.w-rect.y;
            Require(width>0&&depth>0,"Positive wider authoring frame required");
            data["originX"]=rect.x;data["originZ"]=rect.y;data["worldWidthMetres"]=width;data["worldDepthMetres"]=depth;
            var anchors=new JArray();
            foreach(var anchor in original.Anchors)
            {
                Require(original.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float x,out float z),"Original native world anchor projection required");
                var row=EncodePilotAnchor(anchor);row["WorldX"]=x;row["WorldZ"]=z;
                row["NormalizedU"]=(x-rect.x)/width;row["NormalizedV"]=(z-rect.y)/depth;anchors.Add(row);
            }
            data["anchors"]=anchors;
            // Identity source-axis projection keeps every pad centre/dimension,
            // route waypoint ID and timed route declaration unchanged.
            data["pads"]=new JArray(original.Pads.Select(EncodePilotPad));
            data["routes"]=new JArray(original.Routes.Select(EncodePilotRoute));
            var frame=EncodePilotFrame(original.WorldBinding);
            frame["OriginX"]=rect.x;frame["OriginZ"]=rect.y+depth*.5f;
            frame["ForwardX"]=1;frame["ForwardZ"]=0;frame["AcrossX"]=0;frame["AcrossZ"]=1;
            frame["ForwardMetres"]=width;frame["AcrossMetres"]=depth;
            data["worldBinding"]=frame;
            return data.ToString(Newtonsoft.Json.Formatting.None);
        }

        static void RequireLogicalFrameWorldEquivalence(SkirmishMapLayoutConfig original,SkirmishMapLayoutConfig next)
        {
            Require(next.LayoutId==original.LayoutId&&next.OperationMapId==original.OperationMapId&&next.ContentVersion==original.ContentVersion&&
                next.ContentHash==original.ContentHash&&next.CellSize==original.CellSize&&
                next.WorldBinding.SourceHash==original.WorldBinding.SourceHash&&!next.WorldBinding.HasMeasuredDeployments&&!next.WorldBinding.BindAllConfigurations,
                "Logical widening cannot change source identity, cell size or claim measured readiness");
            Require(next.WorldBinding.PlayerDeployment==original.WorldBinding.PlayerDeployment&&next.WorldBinding.EnemyDeployment==original.WorldBinding.EnemyDeployment&&
                next.WorldBinding.PlayerAnchorId==original.WorldBinding.PlayerAnchorId&&next.WorldBinding.EnemyAnchorId==original.WorldBinding.EnemyAnchorId,
                "Actual deployment points/anchor owners remain fixed");
            Require(next.Anchors.Length==original.Anchors.Length&&next.Pads.Length==original.Pads.Length&&next.Routes.Length==original.Routes.Length,
                "Exact original typed hierarchy required");
            foreach(var anchor in original.Anchors)
            {
                Require(next.TryGetAnchorById(anchor.AnchorId,out var kept)&&kept.RoleId==anchor.RoleId&&
                    original.TryProjectLocalToMap(anchor.WorldX,anchor.WorldZ,out float x,out float z)&&
                    next.TryProjectLocalToMap(kept.WorldX,kept.WorldZ,out float nx,out float nz)&&
                    next.TryProjectToMap(kept.NormalizedU,kept.NormalizedV,out float ux,out float uz)&&
                    math.abs(x-nx)<.002f&&math.abs(z-nz)<.002f&&math.abs(x-ux)<.002f&&math.abs(z-uz)<.002f&&
                    kept.NormalizedU>=0&&kept.NormalizedU<=1&&kept.NormalizedV>=0&&kept.NormalizedV<=1,
                    "Every actual world anchor and normalized binder projection must remain identical: "+anchor.AnchorId);
            }
            for(int i=0;i<original.Pads.Length;i++)
            {
                var a=original.Pads[i];var b=next.Pads[i];
                Require(JToken.DeepEquals(EncodePilotPad(a),EncodePilotPad(b)),"Typed pad metadata remains exact");
                foreach(int sx in new[]{-1,0,1})foreach(int sz in new[]{-1,0,1})
                    Require(original.TryProjectLocalToMap(a.CenterX+sx*a.WidthMetres*.5f,a.CenterZ+sz*a.DepthMetres*.5f,out float x,out float z)&&
                        next.TryProjectLocalToMap(b.CenterX+sx*b.WidthMetres*.5f,b.CenterZ+sz*b.DepthMetres*.5f,out float nx,out float nz)&&
                        math.abs(x-nx)<.002f&&math.abs(z-nz)<.002f,"All typed pad centres/corners retain actual world coordinates");
            }
            Require(JToken.DeepEquals(new JArray(original.Routes.Select(EncodePilotRoute)),new JArray(next.Routes.Select(EncodePilotRoute))),
                "Every route waypoint identity, width and timing must remain unchanged");
        }

        public static void RunBoundedLogicalFrameControls()
        {
            Require(RefinementLogicalFrameMargins.SequenceEqual(new[]{0f,32f,64f,128f}),"Exactly four finite frame candidates");
            var originalRect=new Vector4(900,567,1447,738);var allowed=new Vector4(877,400,1700,900);
            Require(TryBoundedLogicalFrameRect(originalRect,allowed,128,out var grown,out _)&&grown==new Vector4(877,439,1575,866)&&grown.z>=1522,
                "Actual east module witness requires available logical room, not a fit claim");
            Require(!TryBoundedLogicalFrameRect(originalRect,new Vector4(901,400,1700,900),32,out _,out _)&&
                !TryBoundedLogicalFrameRect(originalRect,allowed,float.NaN,out _,out _)&&
                !TryBoundedLogicalFrameRect(originalRect,originalRect,32,out _,out _),"Outside-source/nonfinite/no-room inputs refuse");
            var old=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();var next=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            try
            {
                var data=new JObject{["layoutId"]="layout.control",["operationMapId"]="opmap.control",["contentHash"]="unqualified.control",["contentVersion"]=1,
                    ["originX"]=900,["originZ"]=567,["worldWidthMetres"]=547,["worldDepthMetres"]=171,["cellSize"]=1,
                    ["anchors"]=new JArray(new JObject{["AnchorId"]="own",["RoleId"]="base.player",["WorldX"]=950,["WorldZ"]=650,["NormalizedU"]=50f/547,["NormalizedV"]=83f/171},
                        new JObject{["AnchorId"]="enemy",["RoleId"]="base.enemy",["WorldX"]=1380,["WorldZ"]=650,["NormalizedU"]=480f/547,["NormalizedV"]=83f/171}),
                    ["pads"]=new JArray(new JObject{["PadId"]="pad",["AnchorId"]="own",["RoleId"]="base.player",["Kind"]=1,["FactionId"]=1,
                        ["CenterX"]=950,["CenterZ"]=650,["WidthMetres"]=30,["DepthMetres"]=20}),
                    ["routes"]=new JArray(new JObject{["RouteId"]="route",["WaypointAnchorIds"]=new JArray("own","enemy"),["Kind"]=1,["LengthMetres"]=430,["WidthMetres"]=10}),
                    ["worldBinding"]=new JObject{["OriginX"]=900,["OriginZ"]=652.5,["ForwardX"]=1,["ForwardZ"]=0,["AcrossX"]=0,["AcrossZ"]=1,
                        ["ForwardMetres"]=547,["AcrossMetres"]=171,["SourceHash"]="source",["PlayerAnchorId"]="own",["EnemyAnchorId"]="enemy"}};
                JsonUtility.FromJsonOverwrite(data.ToString(),old);string before=JsonUtility.ToJson(old);
                JsonUtility.FromJsonOverwrite(EncodeWidenedRefinementLayout(old,grown),next);RequireLogicalFrameWorldEquivalence(old,next);
                Require(before==JsonUtility.ToJson(old)&&next.WorldWidthMetres==698,"Original immutable and full typed projection retained");
                data["worldBinding"]["ForwardX"]=.5f;JsonUtility.FromJsonOverwrite(data.ToString(),old);
                bool rejected=false;try{EncodeWidenedRefinementLayout(old,grown);}catch(InvalidOperationException){rejected=true;}
                Require(rejected,"Scaled source frame cannot silently widen");
            }
            finally{UnityEngine.Object.DestroyImmediate(next);UnityEngine.Object.DestroyImmediate(old);}
            Require(new[]{"S001","S002","S003","S004","S005","S025","S073"}.All(ProtectedRefinementCatalog)&&!ProtectedRefinementCatalog("S026"),
                "Exact coded seven remain protected");
            Debug.Log("[BoundedLogicalFrameControls] result=Passed controls=6 frameCandidates=4 physicalSourceChanged=0 sourceFitCertified=0");
        }
    }
}
#endif
