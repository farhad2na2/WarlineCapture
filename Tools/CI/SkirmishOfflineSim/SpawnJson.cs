using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

static class SpawnJson
{
    internal static readonly JsonSerializerOptions Options=new(){IncludeFields=true,Converters={new JsonStringEnumConverter(allowIntegerValues:false)}};
    internal static void Need(bool value,string message){if(!value)throw new ArgumentException(message);}
    internal static void Shape(JsonElement value,string[] required,params string[] optional)
    {
        Need(value.ValueKind==JsonValueKind.Object,"Object required");var names=value.EnumerateObject().Select(p=>p.Name).ToHashSet();
        Need(required.All(names.Contains),"Missing fields: "+string.Join(",",required.Except(names)));
        Need(!names.Except(required.Concat(optional)).Any(),"Unknown fields: "+string.Join(",",names.Except(required.Concat(optional))));
    }
    internal static void Unique(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.Object){var seen=new HashSet<string>();foreach(var p in value.EnumerateObject()){Need(seen.Add(p.Name),"Duplicate key "+p.Name);Unique(p.Value);}}
        else if(value.ValueKind==JsonValueKind.Array)foreach(var p in value.EnumerateArray())Unique(p);
    }
    internal static void FactShape(JsonElement value,Type type)
    {
        if(type.IsValueType&&!type.IsPrimitive&&!type.IsEnum)
        {var fields=type.GetFields(BindingFlags.Public|BindingFlags.Instance);Shape(value,fields.Select(f=>f.Name).ToArray());foreach(var f in fields)FactShape(value.GetProperty(f.Name),f.FieldType);}
    }
    internal static void Finite(object value)
    {
        if(value is float f)Need(float.IsFinite(f),"Nonfinite scalar");
        else if(value is double d)Need(double.IsFinite(d),"Nonfinite scalar");
        else if(value!=null&&value.GetType().IsValueType&&!value.GetType().IsPrimitive&&!value.GetType().IsEnum)
            foreach(var field in value.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))Finite(field.GetValue(value));
    }
    internal static T Facts<T>(JsonElement value)where T:struct
    {FactShape(value,typeof(T));var result=value.Deserialize<T>(Options);Finite(result);return result;}
    internal static int Int(JsonElement value,int min,int max){int n=value.GetInt32();Need(n>=min&&n<=max,"Integer outside bounded profile");return n;}
    internal static string Id(JsonElement value){string s=value.GetString();Need(!string.IsNullOrWhiteSpace(s)&&s.Length<=256,"Bounded nonempty ID required");return s;}
    internal static int[] Cell(JsonElement value,int max=1048576)
    {Need(value.ValueKind==JsonValueKind.Array&&value.GetArrayLength()==2,"Two cell integers required");return new[]{Int(value[0],-max,max),Int(value[1],-max,max)};}
    internal static JsonElement[] Array(JsonElement value,int max)
    {Need(value.ValueKind==JsonValueKind.Array&&value.GetArrayLength()<=max,"Bounded array required");return value.EnumerateArray().ToArray();}
    internal static void Expect(JsonElement expected,JsonElement actual,string path)
    {
        if(expected.ValueKind==JsonValueKind.Object){Need(actual.ValueKind==JsonValueKind.Object&&expected.EnumerateObject().Any(),path+": object");foreach(var field in expected.EnumerateObject()){Need(actual.TryGetProperty(field.Name,out var found),path+": absent "+field.Name);Expect(field.Value,found,path+"."+field.Name);}}
        else if(expected.ValueKind==JsonValueKind.Array){Need(actual.ValueKind==JsonValueKind.Array&&expected.GetArrayLength()==actual.GetArrayLength(),path+": length");for(int i=0;i<expected.GetArrayLength();i++)Expect(expected[i],actual[i],path+"["+i+"]");}
        else if(expected.ValueKind==JsonValueKind.Number&&actual.ValueKind==JsonValueKind.Number)Need(expected.TryGetDecimal(out var ed)&&actual.TryGetDecimal(out var ad)?ed==ad:expected.GetDouble()==actual.GetDouble(),path+": numeric mismatch");
        else Need(expected.ValueKind==actual.ValueKind&&expected.ToString()==actual.ToString(),path+": mismatch");
    }
}
