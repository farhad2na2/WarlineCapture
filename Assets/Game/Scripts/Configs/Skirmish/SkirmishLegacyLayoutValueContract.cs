using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Game.Configs
{
    // Data-only equality for the unchanged reviewed S002/S003 source layout.
    // Changes fall through to the original generic native layout/source-fit path.
    public static class SkirmishLegacyLayoutValueContract
    {
        public const string ReviewedAssetSha256 = "a9ff73f6a6852e76f013cf91ab31e59d3b1318b2ced4656f7da61c5c70e988f1";
        public const string ReviewedCanonicalSha256 = "027594f6891322a2f6e61096ee980a5451e6ed78f48bfe67888e68393c58bf6d";

        private static bool Fields(Type type, params string[] expected)
        {
            string[] actual = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => !field.IsStatic && !field.IsInitOnly && !field.IsNotSerialized &&
                    (field.IsPublic || field.IsDefined(typeof(SerializeField), true)))
                .Select(field => field.Name + ":" + field.FieldType.FullName)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            return actual.SequenceEqual(expected.OrderBy(value => value, StringComparer.Ordinal));
        }

        // The serializer's complete membership is guarded as well as the encoded values.
        // A newly serialized nested field can never retain the old reviewed identity.
        public static bool HasSupportedSchema()
        {
            return SkirmishBindingCanonicalValues.HasLayoutDeclarationSchema() && Fields(typeof(SkirmishMeasuredLayoutBinder.LayoutValues),
                "Id:System.String", "OperationMapId:System.String", "ContentHash:System.String", "Version:System.Int32",
                "Width:System.Single", "Depth:System.Single", "OriginX:System.Single", "OriginZ:System.Single", "CellSize:System.Single",
                "Frame:Game.Configs.SkirmishLayoutWorldBindingConfig", "Anchors:Game.Configs.SkirmishLayoutAnchorConfig[]",
                "Routes:Game.Configs.SkirmishLayoutRouteConfig[]", "Pads:Game.Configs.SkirmishLegalPadConfig[]") &&
                Fields(typeof(SkirmishLayoutWorldBindingConfig),
                "OriginX:System.Single", "OriginZ:System.Single", "ForwardX:System.Single", "ForwardZ:System.Single",
                "AcrossX:System.Single", "AcrossZ:System.Single", "ForwardMetres:System.Single", "AcrossMetres:System.Single",
                "PlayerAnchorId:System.String", "EnemyAnchorId:System.String", "SourceHash:System.String", "HasMeasuredDeployments:System.Boolean",
                "PlayerDeployment:UnityEngine.Vector2", "EnemyDeployment:UnityEngine.Vector2", "BindAllConfigurations:System.Boolean") &&
                Fields(typeof(SkirmishLayoutAnchorConfig), "AnchorId:System.String", "RoleId:System.String",
                "NormalizedU:System.Single", "NormalizedV:System.Single", "WorldX:System.Single", "WorldZ:System.Single") &&
                Fields(typeof(SkirmishLayoutRouteConfig), "RouteId:System.String", "WaypointAnchorIds:System.String[]",
                "Kind:Game.Configs.SkirmishMeasuredRouteKind", "LengthMetres:System.Single", "InfantryTransitSeconds:System.Single",
                "GroundTransitSeconds:System.Single", "AirTransitSeconds:System.Single", "InfantryFirstContactSeconds:System.Single",
                "GroundFirstContactSeconds:System.Single", "ProvisionalTimes:System.Boolean", "WidthMetres:System.Single",
                "AllowsVehicles:System.Boolean", "AllowsAircraft:System.Boolean") &&
                Fields(typeof(SkirmishLegalPadConfig), "PadId:System.String", "AnchorId:System.String", "RoleId:System.String",
                "Kind:Game.Configs.SkirmishLegalPadKind", "CenterX:System.Single", "CenterZ:System.Single",
                "WidthMetres:System.Single", "DepthMetres:System.Single", "FactionId:System.Byte") &&
                Fields(typeof(Vector2), "x:System.Single", "y:System.Single") &&
                Enum.GetUnderlyingType(typeof(SkirmishMeasuredRouteKind)) == typeof(byte) &&
                Enum.GetUnderlyingType(typeof(SkirmishLegalPadKind)) == typeof(byte);
        }

        private static void WriteString(BinaryWriter w,string value){
            w.Write(value!=null);
            if(value!=null)w.Write(value);
        }
        private static void WriteArray<T>(BinaryWriter w,T[] values,Action<BinaryWriter,T> item){
            w.Write(values==null?-1:values.Length);
            if(values!=null)foreach(var value in values)item(w,value);
        }
        public static string CanonicalFieldsSha256(SkirmishMapLayoutConfig layout){
            if(layout==null)throw new ArgumentNullException(nameof(layout));
            using var memory=new MemoryStream();
            using(var w=new BinaryWriter(memory,Encoding.UTF8,true)){
                w.Write("ReviewedLegacyLayoutFields/v1");
                WriteString(w,layout.LayoutId);
                WriteString(w,layout.OperationMapId);
                WriteString(w,layout.ContentHash);
                w.Write(layout.ContentVersion);
                w.Write(layout.WorldWidthMetres);
                w.Write(layout.WorldDepthMetres);
                w.Write(layout.OriginX);
                w.Write(layout.OriginZ);
                w.Write(layout.CellSize);
                var f=layout.WorldBinding;
                w.Write(f.OriginX);
                w.Write(f.OriginZ);
                w.Write(f.ForwardX);
                w.Write(f.ForwardZ);
                w.Write(f.AcrossX);
                w.Write(f.AcrossZ);
                w.Write(f.ForwardMetres);
                w.Write(f.AcrossMetres);
                WriteString(w,f.PlayerAnchorId);
                WriteString(w,f.EnemyAnchorId);
                WriteString(w,f.SourceHash);
                w.Write(f.HasMeasuredDeployments);
                w.Write(f.PlayerDeployment.x);
                w.Write(f.PlayerDeployment.y);
                w.Write(f.EnemyDeployment.x);
                w.Write(f.EnemyDeployment.y);
                w.Write(f.BindAllConfigurations);
                WriteArray(w,layout.Anchors,(b,v)=>{
                    WriteString(b,v.AnchorId);
                    WriteString(b,v.RoleId);
                    b.Write(v.NormalizedU);
                    b.Write(v.NormalizedV);
                    b.Write(v.WorldX);
                    b.Write(v.WorldZ);
                }
                );
                WriteArray(w,layout.Routes,(b,v)=>{
                    WriteString(b,v.RouteId);
                    WriteArray(b,v.WaypointAnchorIds,(c,x)=>WriteString(c,x));
                    b.Write((byte)v.Kind);
                    b.Write(v.LengthMetres);
                    b.Write(v.InfantryTransitSeconds);
                    b.Write(v.GroundTransitSeconds);
                    b.Write(v.AirTransitSeconds);
                    b.Write(v.InfantryFirstContactSeconds);
                    b.Write(v.GroundFirstContactSeconds);
                    b.Write(v.ProvisionalTimes);
                    b.Write(v.WidthMetres);
                    b.Write(v.AllowsVehicles);
                    b.Write(v.AllowsAircraft);
                }
                );
                WriteArray(w,layout.Pads,(b,v)=>{
                    WriteString(b,v.PadId);
                    WriteString(b,v.AnchorId);
                    WriteString(b,v.RoleId);
                    b.Write((byte)v.Kind);
                    b.Write(v.CenterX);
                    b.Write(v.CenterZ);
                    b.Write(v.WidthMetres);
                    b.Write(v.DepthMetres);
                    b.Write(v.FactionId);
                }
                );
            }
            using var sha=SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(memory.ToArray())).Replace("-","").ToLowerInvariant();
        }

        public static bool TryGetCompatibilityFingerprint(SkirmishMapLayoutConfig layout, out string fingerprint)
        {
            fingerprint = null;
            if (layout == null || layout.BindingValueFormat != SkirmishBindingValueFormat.NativeJsonV1 || !HasSupportedSchema() || CanonicalFieldsSha256(layout) != ReviewedCanonicalSha256)
                return false;
            return SkirmishLegacyLayoutBaselineContract.TryGetReviewedLayoutFingerprint("S002", out fingerprint);
        }
    }
}
