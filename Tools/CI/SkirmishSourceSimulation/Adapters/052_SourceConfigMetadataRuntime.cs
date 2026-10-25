using System; namespace UnityEngine {
[AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute:Attribute { public string menuName,fileName;public int order; }
[AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute{} [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeReference:Attribute{}
[AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute:Attribute{public HeaderAttribute(string value){}}
[AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute:Attribute{public TooltipAttribute(string value){}}
[AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute:Attribute{public MinAttribute(float value){}}
[AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute:Attribute{public RangeAttribute(float min,float max){}}
// Config-only plain managed metadata. No Unity object/native handle is created.
public abstract class ScriptableObject { protected static T Instantiate<T>(T source)=>throw new NotSupportedException("Native configuration clone entry point is not used by admitted source config compilation"); public string name; public HideFlags hideFlags; public static T CreateInstance<T>() where T:ScriptableObject,new()=>new T(); }
public enum HideFlags { None=0,DontSave=52,HideAndDontSave=61 }
} namespace Unity.Entities { public class InitializationSystemGroup{} }

namespace UnityEngine { public sealed class TextAsset{public readonly string text;public TextAsset(string value){text=value??throw new ArgumentNullException();}} public static class Resources{static System.Collections.Generic.Dictionary<string,object> values;public static void BindSourceResources(System.Collections.Generic.Dictionary<string,object> source){if(values!=null)throw new InvalidOperationException("Source resources bind once");values=source??throw new ArgumentNullException();}public static T Load<T>(string key)where T:class{if(values==null||!values.ContainsKey(key))throw new NotSupportedException("Source resource not admitted: "+key);return values[key]as T??throw new NotSupportedException("Source resource type mismatch: "+key);}} }
