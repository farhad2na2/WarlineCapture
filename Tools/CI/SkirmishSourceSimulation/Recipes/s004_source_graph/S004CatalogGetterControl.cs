using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

internal static class S004CatalogGetterControl
{
    static bool Bool(JObject values, string name) => (string)values[name] == "1";
    static int Int(JObject values, string name) => int.Parse((string)values[name], CultureInfo.InvariantCulture);
    static float Float(JObject values, string name) => float.Parse((string)values[name], CultureInfo.InvariantCulture);
    static string Bits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("x8");
    static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }

    public static int Main(string[] args)
    {
        try {
            var input=JObject.Parse(File.ReadAllText(args[0]));
            Need((string)input["kind"]=="S004SerializedCatalogGetterInputs" && (bool?)input["native_prefab_resolution"]==false,
                "Source-only catalog inputs required");
            var rows=(JArray)input["rows"]; Need(rows.Count==3,"Three source rows required");
            var output=new JArray();
            foreach(JObject row in rows) {
                var v=(JObject)row["serialized_values"]; var sprites=(JObject)row["sprite_references"];
                var config=new S004GetterConfig {
                    IsAirUnit=false, SoldierTransportCapacity=Int(v,"soldierTransportCapacity"), AllowIdleWander=Bool(v,"allowIdleWander"),
                    Speed=Float(v,"speed"), ResourceHaulerBarrelCapacity=Int(v,"resourceHaulerBarrelCapacity"), CanAttack=Bool(v,"canAttack"),
                    AttackRange=Float(v,"attackRange"), AttackDamage=Int(v,"attackDamage"), MaxHealth=Int(v,"maxHealth"),
                    PortraitSprite=new S004SpriteReference((JObject)sprites["portraitSprite"]),
                    PortraitCardSprite=new S004SpriteReference((JObject)sprites["portraitCardSprite"]),
                    PortraitActionSprite=new S004SpriteReference((JObject)sprites["portraitActionSprite"])
                };
                // IsAirUnit is read from the independently captured matching source scalar row.
                config.IsAirUnit=Bool((JObject)row["scalar_source"],"isAirUnit");
                var authoring=new S004GetterProjection { config=config, isProductionTransportUnit=(string)row["component_is_production_transport_unit"]=="1" };
                output.Add(new JObject { ["key"]=(string)row["key"], ["IsAirUnit"]=authoring.IsAirUnit,
                    ["IsProductionTransportUnit"]=authoring.IsProductionTransportUnit,
                    ["SoldierTransportCapacity"]=authoring.SoldierTransportCapacity,["ConfiguredAllowIdleWander"]=authoring.ConfiguredAllowIdleWander,
                    ["ConfiguredSpeedBits"]=Bits(authoring.ConfiguredSpeed),["ConfiguredResourceHaulerBarrelCapacity"]=authoring.ConfiguredResourceHaulerBarrelCapacity,
                    ["ConfiguredCanAttack"]=authoring.ConfiguredCanAttack,["ConfiguredAttackRangeBits"]=Bits(authoring.ConfiguredAttackRange),
                    ["ConfiguredAttackDamage"]=authoring.ConfiguredAttackDamage,["ConfiguredMaxHealth"]=authoring.ConfiguredMaxHealth,
                    ["portraitSprite"]=authoring.PortraitSprite.Reference.DeepClone(),["portraitCardSprite"]=authoring.PortraitCardSprite.Reference.DeepClone(),
                    ["portraitActionSprite"]=authoring.PortraitActionSprite.Reference.DeepClone() });
            }
            var negative=new S004GetterProjection { config=new S004GetterConfig { Speed=-1f, AttackRange=-2f, MaxHealth=-1, SoldierTransportCapacity=-1 } };
            Need(negative.ConfiguredSpeed==0f && negative.ConfiguredAttackRange==0f && negative.ConfiguredMaxHealth==1 && negative.SoldierTransportCapacity==0,
                "Original getter clamp controls failed");
            File.WriteAllText(args[1],new JObject { ["kind"]="S004OriginalCatalogGetterProjection",["rows"]=output,
                ["scope"]="Original getter bodies with source-fed data-only config and sprite reference shims; no Unity import or native getter assertion",
                ["native_prefab_resolution"]=false,["runtime_admission"]=false,["mission_ready"]=false }.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");
            Console.WriteLine("[S004CatalogGetterControl] result=Passed rows=3 world=0 native=0 readiness=0"); return 0;
        } catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
