#if UNITY_EDITOR
using System;
using System.Linq;
using Game.Configs;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        static bool refinementCompoundSearchRequested;
        static readonly float[] RefinementCompoundMargins={0,16,32,64};

        // Explicit authoring experiment. Existing entries and immutable inputs
        // keep their original search policy unless this entry owns the call.
        public static void RunAll120BoundedCompoundRefinement()
        {
            Require(!refinementCompoundSearchRequested&&!EditorApplication.isPlayingOrWillChangePlaymode&&
                !EditorApplication.isCompiling&&!EditorApplication.isUpdating,"Fresh idle bounded logical authoring required");
            RunBoundedCompoundSearchControls();
            refinementCompoundSearchRequested=true;
            try{RunAll120SimultaneousLogicalRefinement();}
            finally{refinementCompoundSearchRequested=false;}
        }

        static bool TryExpandedRefinementCompounds(JObject original,int attempt,SkirmishMapLayoutConfig layout,
            OperationMapDefinition source,GridConfig grid,out JObject expanded,out string error)
        {
            expanded=null;error=null;
            if(attempt<1||attempt>=RefinementCompoundMargins.Length){error="Bounded logical compound candidates exhausted";return false;}
            // The actual native pad helper refuses scale/rotation drift. Then
            // projected layout corners describe the exact axis-aligned logical
            // envelope, not a rotated enclosing-box acceptance shortcut.
            Require(layout.TryGetPad(1,SkirmishLegalPadKind.GroundStaging,out var staging),"Actual typed staging required");
            PilotExactAxisAlignedPadCells(layout,staging,grid);
            float left=float.PositiveInfinity,bottom=float.PositiveInfinity,right=float.NegativeInfinity,top=float.NegativeInfinity;
            foreach(var u in new[]{0f,1f})foreach(var v in new[]{0f,1f})
            {
                Require(layout.TryProjectToMap(u,v,out float x,out float z)&&float.IsFinite(x)&&float.IsFinite(z),"Actual logical envelope corners required");
                left=math.min(left,x);right=math.max(right,x);bottom=math.min(bottom,z);top=math.max(top,z);
            }
            left=math.max(left,math.max(source.Bounds.PlayableMin.x,grid.Origin.x));
            bottom=math.max(bottom,math.max(source.Bounds.PlayableMin.z,grid.Origin.z));
            right=math.min(right,math.min(source.Bounds.PlayableMax.x,grid.Origin.x+grid.Width*grid.CellSize));
            top=math.min(top,math.min(source.Bounds.PlayableMax.z,grid.Origin.z+grid.Height*grid.CellSize));
            return TryExpandRefinementRectangles(original,RefinementCompoundMargins[attempt],new Vector4(left,bottom,right,top),out expanded,out error);
        }

        static bool TryExpandRefinementRectangles(JObject original,float margin,Vector4 envelope,out JObject result,out string error)
        {
            result=null;error=null;
            if(original==null||original.Properties().Count()!=2||original["1"] is not JArray a||original["2"] is not JArray b||
                !float.IsFinite(margin)||margin<=0||!float.IsFinite(envelope.x)||!float.IsFinite(envelope.y)||
                !float.IsFinite(envelope.z)||!float.IsFinite(envelope.w)||envelope.z<=envelope.x||envelope.w<=envelope.y)
            {error="Exact two compounds and finite positive source-bounded envelope required";return false;}
            var next=new JObject();
            foreach(string side in new[]{"1","2"})
            {
                var old=(JArray)original[side];
                if(old.Count!=4||old.Any(v=>v.Type!=JTokenType.Float&&v.Type!=JTokenType.Integer))
                {error="Malformed original compound rectangle";return false;}
                float x=(float)old[0],z=(float)old[1],xx=(float)old[2],zz=(float)old[3];
                if(!float.IsFinite(x)||!float.IsFinite(z)||!float.IsFinite(xx)||!float.IsFinite(zz)||xx<=x||zz<=z||
                    x<envelope.x-.001f||z<envelope.y-.001f||xx>envelope.z+.001f||zz>envelope.w+.001f)
                {error="Original compound outside exact current logical/source/grid envelope";return false;}
                next[side]=new JArray(math.max(envelope.x,x-margin),math.max(envelope.y,z-margin),
                    math.min(envelope.z,xx+margin),math.min(envelope.w,zz+margin));
            }
            var one=(JArray)next["1"];var two=(JArray)next["2"];
            if((float)one[0]<(float)two[2]&&(float)two[0]<(float)one[2]&&
                (float)one[1]<(float)two[3]&&(float)two[1]<(float)one[3])
            {error="Expanded faction compounds overlap; no territory waiver";return false;}
            if(JToken.DeepEquals(original,next)){error="No additional logical search area inside current envelope";return false;}
            result=next;return true;
        }

        public static void RunBoundedCompoundSearchControls()
        {
            var source=new JObject{["1"]=new JArray(10,10,30,30),["2"]=new JArray(70,10,90,30)};
            string before=source.ToString();
            Require(TryExpandRefinementRectangles(source,8,new Vector4(0,0,100,80),out var grown,out _),"Bounded positive expansion required");
            Require((float)grown["1"][0]==2&&(float)grown["2"][3]==38&&source.ToString()==before,"Actual margin and immutable original required");
            Require(TryExpandRefinementRectangles(source,8,new Vector4(5,5,95,35),out var clipped,out _)&&
                (float)clipped["1"][0]==5&&(float)clipped["2"][3]==35,"Source/logical/grid clipping required");
            Require(!TryExpandRefinementRectangles(source,32,new Vector4(0,0,100,80),out _,out _),"Faction overlap must refuse");
            Require(!TryExpandRefinementRectangles(source,8,new Vector4(11,0,100,80),out _,out _),"Original outside authorized envelope must refuse");
            Require(!TryExpandRefinementRectangles(source,float.NaN,new Vector4(0,0,100,80),out _,out _),"Nonfinite margin must refuse");
            var malformed=(JObject)source.DeepClone();malformed["2"]=new JArray(70,10,90);
            Require(!TryExpandRefinementRectangles(malformed,8,new Vector4(0,0,100,80),out _,out _),"Incomplete faction rectangle must refuse");
            Require(!TryExpandRefinementRectangles(source,8,new Vector4(10,10,90,30),out var bounded,out _)||
                (float)bounded["1"][0]>=10&&(float)bounded["2"][2]<=90,"Every accepted candidate remains bounded");
            Debug.Log("[BoundedLogicalCompoundSearchControls] result=Passed controls=7 physicalSourceChanged=0 nativePlacementAcceptance=Pending");
        }
    }
}
#endif
