using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct SkirmishResolvedConvoyBay
    {
        public string ObjectiveRoleId;
        public float CenterX,CenterZ,ForwardX,ForwardZ,AcrossX,AcrossZ,HalfForward,HalfAcross;
    }
    // Mode-owned logical definition; references existing chassis/visual assets.
    // No physical map, chassis prefab or Campaign config is copied or modified.
    [CreateAssetMenu(menuName="Game/SkirmishExpansion/Objective Cargo Truck")]
    public sealed class SkirmishConvoyTruckDefinition : ScriptableObject
    {
        public const string ResourceName="SkirmishConvoyTruckDefinition";
        public const string AssetPath="Assets/Game/Resources/SkirmishConvoyTruckDefinition.asset";
        public const string ExistingChassisKey="Unit_Veh_Truck_Tray";
        public const string ExistingConfigPath="Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Truck_Tray.asset";
        public int ContentVersion=1;
        public GameObject ChassisPrefab;
        public UnitGridAuthoringConfig ChassisConfig;
        public string ChassisPrefabGuid,ChassisPrefabSha256,ChassisConfigGuid,ChassisConfigSha256;
        public static SkirmishConvoyTruckDefinition Load()=>Resources.Load<SkirmishConvoyTruckDefinition>(ResourceName);
        public bool IsValid=>ContentVersion==1&&ChassisPrefab!=null&&ChassisPrefab.name==ExistingChassisKey&&ChassisConfig!=null&&
            !ChassisConfig.IsAirUnit&&ChassisConfig.UsesVehicleMotion&&!ChassisConfig.CanAttack&&ChassisConfig.MaxHealth>0&&
            float.IsFinite(ChassisConfig.Speed)&&ChassisConfig.Speed>0&&
            Hex(ChassisPrefabGuid,32)&&Hex(ChassisConfigGuid,32)&&Hex(ChassisPrefabSha256,64)&&Hex(ChassisConfigSha256,64);
        public string Fingerprint()
        {
            if(!IsValid)throw new InvalidOperationException("Explicit pinned existing Cargo Truck chassis required.");
            using var stream=new MemoryStream();
            using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                writer.Write("SkirmishConvoyCargo/v1/no-weapon-fuel-logistics-passenger-sale-production-wander");
                writer.Write(ContentVersion);writer.Write(ExistingChassisKey);writer.Write(ExistingConfigPath);
                writer.Write(ChassisPrefabGuid);writer.Write(ChassisPrefabSha256);writer.Write(ChassisConfigGuid);writer.Write(ChassisConfigSha256);
                writer.Write(ChassisConfig.MaxHealth);writer.Write(ChassisConfig.Speed);writer.Write(ChassisConfig.WalkSpeed);
                writer.Write(ChassisConfig.RoadSpeedMultiplier);writer.Write(ChassisConfig.ArriveDistance);
            }
            using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();
        }
        public SkirmishRoleBindingConfig RoleBinding()=>new SkirmishRoleBindingConfig
        {
            RoleId=SkirmishRoleIds.ConvoyLogic,Kind=SkirmishRoleKind.ConvoyLogic,SourceConfigPath=AssetPath,
            RuntimePrefabKey=ExistingChassisKey,Producer=SkirmishProducerKind.AuthoredOnly,
            RequiredReadiness=SkirmishReadinessStage.Field,Disposition=SkirmishRosterDisposition.CanonicalRoleCandidate,
            CapabilityCertified=false
        };

        // Exactly three explicit, non-overlapping authored parking bays. The
        // selected source must still pass actual native terrain/exit/path fit.
        public bool TryAppendForces(SkirmishMapLayoutConfig layout,int canonicalTruckCount,int canonicalExtraAllowance,
            List<SkirmishResolvedForceEntry> forces,out string error)
            =>TryAppendForces(layout,canonicalTruckCount,canonicalExtraAllowance,forces,out _,out error);

        public bool TryAppendForces(SkirmishMapLayoutConfig layout,int canonicalTruckCount,int canonicalExtraAllowance,
            List<SkirmishResolvedForceEntry> forces,out SkirmishResolvedConvoyBay[] bays,out string error)
        {
            bays=null;
            error="Three measured objective parking bays and exact +3 allowance required.";
            if(!IsValid||layout==null||!layout.HasWorldBinding||canonicalTruckCount!=3||canonicalExtraAllowance!=3||forces==null)return false;
            foreach(var existing in forces)if(existing.RoleKind==SkirmishRoleKind.ConvoyLogic)return false;
            var pads=new SkirmishLegalPadConfig[3];var staged=new SkirmishResolvedForceEntry[3];var stagedBays=new SkirmishResolvedConvoyBay[3];
            for(int i=0;i<3;i++)
            {
                string role="convoy.origin."+(i+1);
                if(!layout.TryGetPad(role,out pads[i])||pads[i].FactionId!=1||(byte)pads[i].Kind!=10||
                    !layout.TryGetAnchor(role,out var anchor)||anchor.AnchorId!=pads[i].AnchorId||
                    !Positive(pads[i].WidthMetres)||!Positive(pads[i].DepthMetres)||
                    !float.IsFinite(pads[i].CenterX)||!float.IsFinite(pads[i].CenterZ)||
                    !Positive(layout.WorldWidthMetres)||!Positive(layout.WorldDepthMetres)||
                    !layout.TryProjectLocalToMap(pads[i].CenterX,pads[i].CenterZ,out float x,out float z))return false;
                for(int j=0;j<i;j++)if(Mathf.Abs(pads[i].CenterX-pads[j].CenterX)<(pads[i].WidthMetres+pads[j].WidthMetres)/2&&
                    Mathf.Abs(pads[i].CenterZ-pads[j].CenterZ)<(pads[i].DepthMetres+pads[j].DepthMetres)/2)return false;
                staged[i]=new SkirmishResolvedForceEntry{FactionId=1,RoleId=SkirmishRoleIds.ConvoyLogic,RoleKind=SkirmishRoleKind.ConvoyLogic,
                    RuntimePrefabKey=ExistingChassisKey,SourceConfigPath=AssetPath,Quantity=1,SupplyCost=0,
                    SpawnAnchorId=anchor.AnchorId,ObjectiveRoleId="convoy."+(i+1)};
                var frame=layout.WorldBinding;
                stagedBays[i]=new SkirmishResolvedConvoyBay{ObjectiveRoleId=staged[i].ObjectiveRoleId,CenterX=x,CenterZ=z,
                    ForwardX=frame.ForwardX,ForwardZ=frame.ForwardZ,AcrossX=frame.AcrossX,AcrossZ=frame.AcrossZ,
                    HalfForward=pads[i].WidthMetres/layout.WorldWidthMetres*frame.ForwardMetres*.5f,
                    HalfAcross=pads[i].DepthMetres/layout.WorldDepthMetres*frame.AcrossMetres*.5f};
            }
            forces.AddRange(staged);bays=stagedBays;error=null;return true;
        }
        private static bool Positive(float value)=>float.IsFinite(value)&&value>0;
        private static bool Hex(string value,int length)
        {if(value==null||value.Length!=length)return false;foreach(char c in value)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))return false;return true;}
    }
}
