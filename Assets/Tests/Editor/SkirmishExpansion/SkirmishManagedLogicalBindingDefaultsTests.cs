#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class SkirmishManagedLogicalBindingDefaultsTests
    {
        [Test] public void EmptyManagedConstructorsExistAndCannotAuthorizeLogicalIdentity()
        {
            foreach(var type in new[]{typeof(SkirmishLaunchDefinitionRecord),typeof(SkirmishNativeMissionLogicalBindingRecord),typeof(SkirmishNativeObjectiveRecoveryBinding)})
                Assert.IsNotNull(type.GetConstructor(Type.EmptyTypes),type.FullName);
            var launch=new SkirmishLaunchDefinitionRecord();
            Assert.IsNull(launch.Definition);Assert.IsFalse(launch.Matches(new SkirmishResolvedSetup(),default));
            var recovery=new SkirmishNativeObjectiveRecoveryBinding();
            Assert.IsNull(recovery.Areas);Assert.IsNull(recovery.MissionViewProfileFingerprint);
            using var world=new World("EmptyManagedLogicalBinding");var em=world.EntityManager;var session=em.CreateEntity();
            em.AddComponentData(session,new SkirmishExpandedSessionComponent());
            em.AddComponentObject(session,new SkirmishResolvedSetupRecord());
            em.AddComponentObject(session,new SkirmishNativeMissionLogicalBindingRecord());
            int before=em.UniversalQuery.CalculateEntityCount();
            Assert.IsFalse(SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var compiled));Assert.IsNull(compiled);
            Assert.AreEqual(before,em.UniversalQuery.CalculateEntityCount());
        }
        public static void RunFocusedValidation()
        {
            new SkirmishManagedLogicalBindingDefaultsTests().EmptyManagedConstructorsExistAndCannotAuthorizeLogicalIdentity();
            UnityEngine.Debug.Log("[SkirmishManagedLogicalBindingDefaultsTests] result=Passed constructors=3 tests=1 nativeMission=0");
        }
    }
}
#endif
