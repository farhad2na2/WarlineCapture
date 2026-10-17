using System.Linq;
using Game.Components;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class AriaBoardingFootprintTests
    {
        static bool Inside(Vector2[] points,float minX,float minZ,float maxX,float maxZ)
            =>points!=null&&points.Length==4&&points.All(p=>p.x>=minX&&p.y>=minZ&&p.x<=maxX&&p.y<=maxZ);

        [TestCase(3,3)][TestCase(4,2)]
        public void ActualOddAndEvenFootprintsRejectShiftedExitContainment(int width,int depth)
        {
            using var world=new World("Actual native carrier footprint edges");var em=world.EntityManager;
            var actor=em.CreateEntity(typeof(UnitGrid),typeof(UnitFootprint));
            em.SetComponentData(actor,new UnitGrid{Cell=new int2(10,10)});
            em.SetComponentData(actor,new UnitFootprint{Size=new int2(width,depth)});
            var grid=new GridConfig{Width=64,Height=64,CellSize=1};
            Assert.IsTrue(SkirmishBoardingFootprintProjection.TryRead(em,actor,grid,out var corners));
            var minimum=UnitFootprintUtility.GetMinCell(new int2(10,10),new int2(width,depth));
            Assert.AreEqual(new Vector2(minimum.x,minimum.y),corners[0]);
            Assert.AreEqual(new Vector2(minimum.x+width,minimum.y+depth),corners[2]);
            var shifted=new[]{new Vector2(10,10),new Vector2(10+width,10),new Vector2(10+width,10+depth),new Vector2(10,10+depth)};
            Assert.IsTrue(Inside(shifted,10,9,15,15),"Historical shifted quad falsely appears inside this exit");
            Assert.IsFalse(Inside(corners,10,9,15,15),"Actual negative edge is outside");
            Assert.IsTrue(Inside(corners,minimum.x,minimum.y,minimum.x+width,minimum.y+depth));
            Assert.IsFalse(Inside(shifted,minimum.x,minimum.y,minimum.x+width,minimum.y+depth),"Historical shift must reject a physically exact legal exit");
        }

        [TestCase(3,3)][TestCase(4,2)]
        public void NativeDestinationRequiresAllOddAndEvenFootprintCornersInside(int width,int depth)
        {
            using var world=new World("Actual native goal footprint edges");var em=world.EntityManager;
            var actor=em.CreateEntity(typeof(UnitGrid),typeof(UnitFootprint));
            em.SetComponentData(actor,new UnitGrid{Cell=new int2(2,2)});
            em.SetComponentData(actor,new UnitFootprint{Size=new int2(width,depth)});
            var grid=new GridConfig{Width=64,Height=64,CellSize=1};var goal=new int2(10,10);
            Assert.IsTrue(SkirmishBoardingFootprintProjection.TryReadAt(em,actor,grid,goal,out var corners));
            Assert.IsTrue(10.5f>=10&&10.5f<=15,"Old target center is inside");
            Assert.IsFalse(Inside(corners,10,9,15,15),"A target center cannot certify the full future footprint");
            var min=UnitFootprintUtility.GetMinCell(goal,new int2(width,depth));
            Assert.IsTrue(Inside(corners,min.x,min.y,min.x+width,min.y+depth));
            Assert.AreEqual(new int2(2,2),em.GetComponentData<UnitGrid>(actor).Cell,"Readout must not move the actor");
        }

        [Test]
        public void NativeOriginAndCellScaleAreAppliedToAllCorners()
        {
            using var world=new World("Native footprint scale");var em=world.EntityManager;
            var actor=em.CreateEntity(typeof(UnitGrid),typeof(UnitFootprint));
            em.SetComponentData(actor,new UnitGrid{Cell=new int2(10,20)});em.SetComponentData(actor,new UnitFootprint{Size=new int2(3,4)});
            Assert.IsTrue(SkirmishBoardingFootprintProjection.TryRead(em,actor,new GridConfig{CellSize=2,Origin=new float3(-100,7,300)},out var corners));
            Assert.AreEqual(new Vector2(-82,338),corners[0]);Assert.AreEqual(new Vector2(-76,346),corners[2]);
        }

        [Test]
        public void MissingAndMalformedNativeFootprintsRemainUnknown()
        {
            using var world=new World("Missing native footprint");var em=world.EntityManager;var actor=em.CreateEntity(typeof(UnitGrid));
            var grid=new GridConfig{CellSize=1};
            Assert.IsFalse(SkirmishBoardingFootprintProjection.TryRead(em,actor,grid,out _));
            em.AddComponentData(actor,new UnitFootprint{Size=new int2(0,3)});
            Assert.IsFalse(SkirmishBoardingFootprintProjection.TryRead(em,actor,grid,out _));
            em.SetComponentData(actor,new UnitFootprint{Size=new int2(3,3)});grid.CellSize=float.NaN;
            Assert.IsFalse(SkirmishBoardingFootprintProjection.TryRead(em,actor,grid,out _));
        }
    }
}
