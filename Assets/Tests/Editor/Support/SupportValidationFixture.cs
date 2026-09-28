#if UNITY_EDITOR
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Tests.Editor
{
    internal sealed class SupportValidationFixture : System.IDisposable
    {
        public readonly World World=new World("Support validation");
        public EntityManager Em=>World.EntityManager;
        public readonly Entity Root;
        public SupportValidationFixture()
        {
            Root=Em.CreateEntity();
            using var builder=new BlobBuilder(Allocator.Temp);
            ref var blob=ref builder.ConstructRoot<SupportCatalogBlob>();var entries=builder.Allocate(ref blob.Abilities,4);
            for(int i=0;i<4;i++)entries[i]=new SupportAbilityDefinition {Kind=(SupportAbilityKind)(i+1),Charges=i==0?2:1,FuelCost=i==0?1:4,
                CooldownSeconds=35,Radius=12,DurationSeconds=15,DirectDamagePermille=650};
            Em.AddComponentData(Root,new SupportCatalogComponent {Blob=builder.CreateBlobAssetReference<SupportCatalogBlob>(Allocator.Persistent),Revision=1,OwnsBlob=1});
            Em.AddComponentData(Root,new SupportSessionComponent {SessionToken="support-test",AttemptOrdinal=1,FactionId=1,Active=1,TestEncounter=1,CatalogRevision=1,NextRequestId=1});
            Em.AddComponentData(Root,new SupportMissionPolicyComponent {AllowedMask=15,TestGrantMask=1,GroundMin=new float2(-50),GroundMax=new float2(50)});
            Em.AddComponent<SupportCollectionFeedbackComponent>(Root);Em.AddComponent<SupportInputStateComponent>(Root);Em.AddComponent<SupportPreviewComponent>(Root);Em.AddComponent<SupportProposalComponent>(Root);
            Em.AddBuffer<SupportReceiptElement>(Root);Em.AddBuffer<SupportRequestElement>(Root);
            var abilities=Em.AddBuffer<SupportAbilityStateElement>(Root);abilities.Add(new SupportAbilityStateElement {Kind=SupportAbilityKind.Smoke,ChargesRemaining=2,StateVersion=1,Enabled=1});
            Em.SetComponentData(Root,new SupportPreviewComponent {PreviewId=1,Request=Request(),Valid=1,FuelCost=1,Radius=12});
            Em.AddBuffer<SupportGroundRegionElement>(Root).Add(new SupportGroundRegionElement {Min=new float2(-30),Max=new float2(30),Visible=1,Version=1});
        }
        public SupportRequestElement Request(uint id=1)=>new SupportRequestElement {SessionToken="support-test",AttemptOrdinal=1,RequestId=id,
            Kind=SupportAbilityKind.Smoke,ExpectedAbilityVersion=1,PreviewId=1,Position=float3.zero};
        public Entity Store(float stock,float held=0,float civilian=0)
        {
            var entity=Em.CreateEntity(typeof(BuildingResourceStorageComponent));Em.SetComponentData(entity,new BuildingResourceStorageComponent
            {OwnerFactionId=1,FuelStorageCapacity=100,StoredFuelBarrels=stock,ReservedFuelOutboundBarrels=held,CivilianFuelReserveBarrels=civilian,Version=1});return entity;
        }
        public Entity Unit(float3 position,byte faction=2,int health=1000)
        {
            var unit=Em.CreateEntity(typeof(UnitGrid),typeof(UnitHealth),typeof(Faction),typeof(LocalTransform));
            Em.SetComponentData(unit,new UnitHealth {Current=health,Max=health});Em.SetComponentData(unit,new Faction {Id=faction});
            Em.SetComponentData(unit,LocalTransform.FromPosition(position));return unit;
        }
        public void Dispose(){var catalog=Em.GetComponentData<SupportCatalogComponent>(Root);catalog.Blob.Dispose();World.Dispose();}
    }
}
#endif
