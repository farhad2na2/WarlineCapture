using System;using System.Linq;using System.Collections;using System.Collections.Generic;using Unity.Collections;namespace Unity.Entities{
public class OwnedQueryShape{protected readonly EntityManager em;readonly Type[] values;readonly List<Type> all=new List<Type>(),none=new List<Type>();Type changed;EntityQueryOptions options;internal OwnedQueryShape(EntityManager manager,Type[] wrappers){em=manager;values=wrappers;foreach(var w in wrappers)all.Add(Inner(w));}static Type Inner(Type t){if(!t.IsGenericType||(t.GetGenericTypeDefinition()!=typeof(RefRO<>)&&t.GetGenericTypeDefinition()!=typeof(RefRW<>)&&t.GetGenericTypeDefinition()!=typeof(DynamicBuffer<>)))throw new NotSupportedException("Unbound original query ref wrapper");return t.GetGenericArguments()[0];}protected void AddAll(params Type[] ts){all.AddRange(ts);}protected void AddNone(params Type[] ts){none.AddRange(ts);}protected void Change(Type t){changed=t;}protected void Options(EntityQueryOptions value){if((value&~EntityQueryOptions.IncludeDisabledEntities)!=0)throw new NotSupportedException("Only actual fabrication IncludeDisabledEntities query option admitted");options=value;}protected IEnumerable<Entity> Rows(){var q=em.CreateEntityQuery(new EntityQueryDesc{Options=options,All=all.Distinct().Select(t=>(ComponentType)t).ToArray(),None=none.Distinct().Select(t=>(ComponentType)t).ToArray()});foreach(var e in em.ReadOnlyOwnedQueryRows(q))if(changed==null||em.Changed(e,changed,SystemAPI.LastVersion))yield return e;}protected T Ref<T>(Entity e)=>WrapperFactory<T>.Invoke(em,e);
// Cache only the existing source RefRO/RefRW constructor or actual GetBuffer(false) call.
// The typed wrapper is recreated each time; no component values/query rows are cached.
static class WrapperFactory<T>{internal static readonly Func<EntityManager,Entity,T> Invoke=Build();static Func<EntityManager,Entity,T> Build(){var type=typeof(T);if(!type.IsGenericType)throw new NotSupportedException("Original query wrapper required");var definition=type.GetGenericTypeDefinition();var manager=System.Linq.Expressions.Expression.Parameter(typeof(EntityManager),"manager");var entity=System.Linq.Expressions.Expression.Parameter(typeof(Entity),"entity");System.Linq.Expressions.Expression call;if(definition==typeof(DynamicBuffer<>)){var method=typeof(EntityManager).GetMethods().Single(x=>x.Name=="GetBuffer"&&x.IsGenericMethodDefinition&&x.GetParameters().Length==2).MakeGenericMethod(type.GetGenericArguments()[0]);call=System.Linq.Expressions.Expression.Call(manager,method,entity,System.Linq.Expressions.Expression.Constant(false));}else if(definition==typeof(RefRO<>)||definition==typeof(RefRW<>)){var constructor=type.GetConstructor(new[]{typeof(EntityManager),typeof(Entity)})??throw new MissingMethodException(type.FullName,".ctor(EntityManager,Entity)");call=System.Linq.Expressions.Expression.New(constructor,manager,entity);}else throw new NotSupportedException("Original query wrapper required");var typed=System.Linq.Expressions.Expression.Lambda<Func<EntityManager,Entity,T>>(call,manager,entity).Compile();return(m,e)=>{try{return typed(m,e);}catch(Exception error){throw new System.Reflection.TargetInvocationException(error);}};}}
}
public sealed class OwnedQuery<A>:OwnedQueryShape,IEnumerable<A>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A)}){}
public OwnedQuery<A> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<A> GetEnumerator(){foreach(var e in Rows())yield return Ref<A>(e);}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),e);}}
public sealed class OwnedQuery<A,B>:OwnedQueryShape,IEnumerable<(A,B)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B)}){}
public OwnedQuery<A,B> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),e);}}
public sealed class OwnedQuery<A,B,C>:OwnedQueryShape,IEnumerable<(A,B,C)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B),typeof(C)}){}
public OwnedQuery<A,B,C> WithOptions(EntityQueryOptions value){Options(value);return this;}
public OwnedQuery<A,B,C> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B,C)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e),Ref<C>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,C,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),Ref<C>(e),e);}}
public sealed class OwnedQuery<A,B,C,D>:OwnedQueryShape,IEnumerable<(A,B,C,D)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B),typeof(C),typeof(D)}){}
public OwnedQuery<A,B,C,D> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B,C,D)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,C,D,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),e);}}
public sealed class OwnedQuery<A,B,C,D,E>:OwnedQueryShape,IEnumerable<(A,B,C,D,E)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B),typeof(C),typeof(D),typeof(E)}){}
public OwnedQuery<A,B,C,D,E> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B,C,D,E)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,C,D,E,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),e);}}
public sealed class OwnedQuery<A,B,C,D,E,F>:OwnedQueryShape,IEnumerable<(A,B,C,D,E,F)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B),typeof(C),typeof(D),typeof(E),typeof(F)}){}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E,F> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E,F> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E,F> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B,C,D,E,F)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),Ref<F>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,C,D,E,F,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),Ref<F>(e),e);}}
public sealed class OwnedQuery<A,B,C,D,E,F,G>:OwnedQueryShape,IEnumerable<(A,B,C,D,E,F,G)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B),typeof(C),typeof(D),typeof(E),typeof(F),typeof(G)}){}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E,F,G> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B,C,D,E,F,G)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),Ref<F>(e),Ref<G>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,C,D,E,F,G,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),Ref<F>(e),Ref<G>(e),e);}}
public sealed class OwnedQuery<A,B,C,D,E,F,G,H>:OwnedQueryShape,IEnumerable<(A,B,C,D,E,F,G,H)>{internal OwnedQuery(EntityManager manager):base(manager,new[]{typeof(A),typeof(B),typeof(C),typeof(D),typeof(E),typeof(F),typeof(G),typeof(H)}){}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0>(){AddAll(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1>(){AddAll(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1,T2>(){AddAll(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1,T2,T3>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1,T2,T3,T4>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1,T2,T3,T4,T5>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1,T2,T3,T4,T5,T6>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){AddAll(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0>(){AddNone(typeof(T0));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1>(){AddNone(typeof(T0),typeof(T1));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1,T2>(){AddNone(typeof(T0),typeof(T1),typeof(T2));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1,T2,T3>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1,T2,T3,T4>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1,T2,T3,T4,T5>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1,T2,T3,T4,T5,T6>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){AddNone(typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7));return this;}
public OwnedQuery<A,B,C,D,E,F,G,H> WithChangeFilter<T>(){Change(typeof(T));return this;}public IEnumerator<(A,B,C,D,E,F,G,H)> GetEnumerator(){foreach(var e in Rows())yield return (Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),Ref<F>(e),Ref<G>(e),Ref<H>(e));}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();public IEnumerable<(A,B,C,D,E,F,G,H,Entity)> WithEntityAccess(){foreach(var e in Rows())yield return(Ref<A>(e),Ref<B>(e),Ref<C>(e),Ref<D>(e),Ref<E>(e),Ref<F>(e),Ref<G>(e),Ref<H>(e),e);}}
public sealed class EntityQueryBuilder{readonly List<Type> all=new List<Type>(),none=new List<Type>();EntityQueryOptions options;public EntityQueryBuilder(Allocator a){}
public EntityQueryBuilder WithAll<T0>(){all.AddRange(new[]{typeof(T0)});return this;}
public EntityQueryBuilder WithAll<T0,T1>(){all.AddRange(new[]{typeof(T0),typeof(T1)});return this;}
public EntityQueryBuilder WithAll<T0,T1,T2>(){all.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2)});return this;}
public EntityQueryBuilder WithAll<T0,T1,T2,T3>(){all.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3)});return this;}
public EntityQueryBuilder WithAll<T0,T1,T2,T3,T4>(){all.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4)});return this;}
public EntityQueryBuilder WithAll<T0,T1,T2,T3,T4,T5>(){all.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5)});return this;}
public EntityQueryBuilder WithAll<T0,T1,T2,T3,T4,T5,T6>(){all.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6)});return this;}
public EntityQueryBuilder WithAll<T0,T1,T2,T3,T4,T5,T6,T7>(){all.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7)});return this;}
public EntityQueryBuilder WithNone<T0>(){none.AddRange(new[]{typeof(T0)});return this;}
public EntityQueryBuilder WithNone<T0,T1>(){none.AddRange(new[]{typeof(T0),typeof(T1)});return this;}
public EntityQueryBuilder WithNone<T0,T1,T2>(){none.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2)});return this;}
public EntityQueryBuilder WithNone<T0,T1,T2,T3>(){none.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3)});return this;}
public EntityQueryBuilder WithNone<T0,T1,T2,T3,T4>(){none.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4)});return this;}
public EntityQueryBuilder WithNone<T0,T1,T2,T3,T4,T5>(){none.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5)});return this;}
public EntityQueryBuilder WithNone<T0,T1,T2,T3,T4,T5,T6>(){none.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6)});return this;}
public EntityQueryBuilder WithNone<T0,T1,T2,T3,T4,T5,T6,T7>(){none.AddRange(new[]{typeof(T0),typeof(T1),typeof(T2),typeof(T3),typeof(T4),typeof(T5),typeof(T6),typeof(T7)});return this;}
public EntityQueryBuilder WithOptions(EntityQueryOptions value){options=value;return this;}public EntityQuery Build()=>Build(SystemAPI.Manager);public EntityQuery Build(ref SystemState state)=>Build(state.EntityManager);EntityQuery Build(EntityManager em)=>em.CreateEntityQuery(new EntityQueryDesc{All=all.Select(t=>(ComponentType)t).ToArray(),None=none.Select(t=>(ComponentType)t).ToArray(),Options=options});}}
