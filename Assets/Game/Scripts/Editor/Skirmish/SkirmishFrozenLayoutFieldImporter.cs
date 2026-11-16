#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Reflection;using System.Security.Cryptography;using System.Text;using Game.Configs;using Newtonsoft.Json.Linq;using UnityEngine;
namespace Game.Editor {
 // Finite frozen layout field codec. No asset references, arbitrary classes or numeric normalization.
 internal static class SkirmishFrozenLayoutFieldImporter {
  static readonly Type[] Structs={typeof(SkirmishLayoutAnchorConfig),typeof(SkirmishLayoutRouteConfig),typeof(SkirmishLegalPadConfig),typeof(SkirmishLayoutWorldBindingConfig),typeof(Vector2)};
  const string S026FrozenFixtureSha256="669c8b20f55890b37657605f9c0d67087f6d4da672d2e0eae2ab9d78ca2ca14e";
  static readonly string[] S026FrozenFixtureFields={"anchors","bindingValueFormat","cellSize","contentHash","contentVersion","layoutId","operationMapId","originX","originZ","pads","routes","worldBinding","worldDepthMetres","worldWidthMetres"};
  static readonly string[] S026CurrentLayoutFields={"anchors","bindingValueFormat","cellSize","contentHash","contentVersion","declaredConfigurations","layoutId","operationMapId","originX","originZ","pads","routes","structureInstancePads","worldBinding","worldDepthMetres","worldWidthMetres"};
  static FieldInfo[] Fields(Type type)=>type.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly).Where(f=>!f.IsStatic&&!f.IsInitOnly&&(f.IsPublic||f.GetCustomAttribute<SerializeField>()!=null)).ToArray();
  internal static SkirmishMapLayoutConfig Read(JObject fields){var value=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();try{Assign(value,fields,typeof(SkirmishMapLayoutConfig),0);return value;}catch{UnityEngine.Object.DestroyImmediate(value);throw;}}
  // The exact frozen S026 fixture predates two serialized, absence-by-empty-array
  // fields. Keep this compatibility path separate from Read(): only the byte-pinned
  // fixture's exact former field set receives explicit empty defaults.
  internal static SkirmishMapLayoutConfig ReadS026FrozenFixture(JObject fields,string fixturePath){
   if(string.IsNullOrEmpty(fixturePath)||!Path.IsPathRooted(fixturePath))
    throw new InvalidDataException("Exact hash-pinned S026 frozen layout fixture required");
   byte[] fixtureBytes=File.ReadAllBytes(fixturePath);
   if(HashBytes(fixtureBytes)!=S026FrozenFixtureSha256)throw new InvalidDataException("Exact hash-pinned S026 frozen layout fixture required");
   var names=fields?.Properties().Select(p=>p.Name).OrderBy(v=>v,StringComparer.Ordinal).ToArray();
   if(names==null||!names.SequenceEqual(S026FrozenFixtureFields,StringComparer.Ordinal))
    throw new InvalidDataException("Exact 14-field S026 frozen fixture schema required");
   JObject pinnedFields;
   using(var text=new StreamReader(new MemoryStream(fixtureBytes),Encoding.UTF8,true))
   using(var reader=new Newtonsoft.Json.JsonTextReader(text){DateParseHandling=Newtonsoft.Json.DateParseHandling.None}){
    pinnedFields=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
    if(reader.Read())throw new InvalidDataException("Trailing content in hash-pinned S026 layout fixture");
   }
   if(!JToken.DeepEquals(fields,pinnedFields))throw new InvalidDataException("Supplied S026 fields differ from hash-pinned fixture content");
   var value=ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
   try{
    var expected=Fields(typeof(SkirmishMapLayoutConfig));
    if(!expected.Select(f=>f.Name).OrderBy(v=>v,StringComparer.Ordinal).SequenceEqual(S026CurrentLayoutFields,StringComparer.Ordinal))
     throw new InvalidDataException("Current S026 layout field contract changed");
    var defaults=new[]{"declaredConfigurations","structureInstancePads"};
    foreach(var field in expected){
     if(defaults.Contains(field.Name,StringComparer.Ordinal)){
      Type expectedType=field.Name=="declaredConfigurations"?typeof(SkirmishDeclaredLayoutConfiguration[]):typeof(SkirmishStructureInstancePadBinding[]);
      if(field.FieldType!=expectedType)throw new InvalidDataException("S026 absence default field type changed: "+field.Name);
      field.SetValue(value,Array.CreateInstance(field.FieldType.GetElementType(),0));continue;
     }
     if(!S026FrozenFixtureFields.Contains(field.Name,StringComparer.Ordinal))throw new InvalidDataException("Unexpected current layout field: "+field.Name);
     field.SetValue(value,Value(fields[field.Name],field.FieldType,1));
    }
    return value;
   }catch{UnityEngine.Object.DestroyImmediate(value);throw;}
  }
  internal static int ValidateS026FrozenFixtureNegativeControls(JObject fields,string fixturePath){
   int rejected=0;
   var missing=(JObject)fields.DeepClone();missing.Remove(S026FrozenFixtureFields[0]);
   ExpectS026Rejected(missing,fixturePath);rejected++;
   var extra=(JObject)fields.DeepClone();extra["unreviewedField"]=0;
   ExpectS026Rejected(extra,fixturePath);rejected++;
   var altered=(JObject)fields.DeepClone();altered["layoutId"]=(string)altered["layoutId"]+".altered";
   ExpectS026Rejected(altered,fixturePath);rejected++;
   ExpectS026Rejected(fields,"relative-s026-layout.json");rejected++;
   return rejected;
  }
  static void ExpectS026Rejected(JObject fields,string path){
   try{var unexpected=ReadS026FrozenFixture(fields,path);UnityEngine.Object.DestroyImmediate(unexpected);throw new InvalidOperationException("Invalid S026 frozen fixture input was accepted");}
   catch(InvalidDataException){}
  }
  static string HashBytes(byte[] bytes){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  static void Assign(object value,JObject fields,Type type,int depth){var expected=Fields(type);if(fields==null||fields.Properties().Count()!=expected.Length||fields.Properties().Any(p=>!expected.Any(f=>f.Name==p.Name)))throw new NotSupportedException("Exact finite layout field set required: "+type.FullName);foreach(var field in expected)field.SetValue(value,Value(fields[field.Name],field.FieldType,depth+1));}
  static object Value(JToken token,Type type,int depth){if(depth>16||token==null)throw new NotSupportedException("Bounded complete layout value required");if(token.Type==JTokenType.Null){if(type.IsValueType)throw new NotSupportedException("Layout value type null");return null;}
   if(type==typeof(string)){if(token.Type!=JTokenType.String)throw new NotSupportedException("Layout string type");return(string)token;}
   if(type==typeof(bool)){if(token.Type==JTokenType.Boolean)return(bool)token;if(token.Type==JTokenType.Integer&&((long)token==0||(long)token==1))return(long)token==1;throw new NotSupportedException("Layout boolean type");}
   if(type.IsEnum){if(token.Type!=JTokenType.Integer)throw new NotSupportedException("Layout enum integer required");object value=Enum.ToObject(type,Value(token,Enum.GetUnderlyingType(type),depth+1));if(!Enum.IsDefined(type,value))throw new NotSupportedException("Undefined layout enum");return value;}
   if(type==typeof(float)||type==typeof(int)||type==typeof(byte)){if(token.Type!=JTokenType.Integer&&token.Type!=JTokenType.Float)throw new NotSupportedException("Layout scalar type");if(type==typeof(float)){object raw=((JValue)token).Value;float f=raw is float single?single:raw is double wide?(float)wide:token.ToObject<float>();if(float.IsNaN(f)||float.IsInfinity(f))throw new NotSupportedException("Finite layout float required");return f;}return token.ToObject(type);}
   if(type.IsArray){var items=token as JArray;if(items==null||items.Count>4096)throw new NotSupportedException("Bounded layout array required");var element=type.GetElementType();var result=Array.CreateInstance(element,items.Count);for(int i=0;i<items.Count;i++)result.SetValue(Value(items[i],element,depth+1),i);return result;}
   if(!Structs.Contains(type))throw new NotSupportedException("Layout type outside finite codec: "+type.FullName);var boxed=Activator.CreateInstance(type);Assign(boxed,token as JObject,type,depth+1);return boxed;
  }
  internal static JObject FloatBits(SkirmishMapLayoutConfig layout){var rows=new JObject();Visit(layout,typeof(SkirmishMapLayoutConfig),"$",rows,0);return rows;}
  static void Visit(object value,Type type,string path,JObject rows,int depth){if(depth>16)throw new NotSupportedException("Layout diagnostic depth");if(value==null)return;if(type==typeof(float)){rows[path]=BitConverter.ToUInt32(BitConverter.GetBytes((float)value),0).ToString("x8");return;}if(type.IsArray){var items=(Array)value;for(int i=0;i<items.Length;i++)Visit(items.GetValue(i),type.GetElementType(),path+"["+i+"]",rows,depth+1);return;}if(type!=typeof(SkirmishMapLayoutConfig)&&!Structs.Contains(type))return;foreach(var field in Fields(type))Visit(field.GetValue(value),field.FieldType,path+"."+field.Name,rows,depth+1);}
 }
}
#endif
