using System;
using Unity.Collections;
using Unity.Entities;

// Exercises the current source-host query implementation with live mutations.
internal static class S004SingletonEnumerationControl
{
    private struct A : IComponentData { }
    private struct B : IComponentData { }
    private static Entity Old(EntityManager em)
    {
        using var q=em.CreateEntityQuery(typeof(A),typeof(B));
        if(q.CalculateEntityCount()!=1) throw new NotSupportedException("One actual source session/catalog required");
        return q.GetSingletonEntity();
    }
    private static Entity Candidate(EntityManager em)
    {
        using var q=em.CreateEntityQuery(typeof(A),typeof(B));
        using var owners=q.ToEntityArray(Allocator.Temp);
        if(owners.Length!=1) throw new NotSupportedException("One actual source session/catalog required");
        return owners[0];
    }
    private static void Compare(EntityManager em,Entity expected)
    {
        Entity old=Entity.Null,next=Entity.Null;string oldError=null,nextError=null;
        try { old=Old(em); } catch(Exception e) { oldError=e.GetType().FullName+":"+e.Message; }
        try { next=Candidate(em); } catch(Exception e) { nextError=e.GetType().FullName+":"+e.Message; }
        if(oldError!=nextError||old!=next||(oldError==null&&old!=expected))throw new Exception("singleton enumeration mismatch");
    }
    private static void Main()
    {
        var em=new EntityManager { LogicalAllocatorAdmitted=true };int id=0;
        em.AllocateLogicalIdentity=()=>new Entity { Index=++id,Version=1 };
        Compare(em,Entity.Null);
        var first=em.CreateEntity();em.AddComponentData(first,new A());Compare(em,Entity.Null);
        em.AddComponentData(first,new B());Compare(em,first);
        var second=em.CreateEntity();em.AddComponentData(second,new A());em.AddComponentData(second,new B());Compare(em,Entity.Null);
        em.AddComponentData(second,new Disabled());Compare(em,first);
        em.RemoveComponent<B>(first);Compare(em,Entity.Null);
        em.RemoveComponent<Disabled>(second);Compare(em,second);
        em.DestroyEntity(second);Compare(em,Entity.Null);
        Console.WriteLine("[S004SingletonEnumerationControl] result=Passed cases=8 fixtureOnly=1 fullScopeParity=Unknown missionReady=0");
    }
}
