using System;using System.IO;using System.Reflection;using Game.Components;
internal static class PhysicalProductionInputChecks{
 static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&Math.Abs(value)<=1e9f;
 internal static void Validate(BuildingResourceStorageComponent storage,bool hasProfile,BuildingResourceProductionProfileComponent profile,float seconds,float delta,float ratio){
 Need(Finite(seconds)&&Finite(delta)&&Math.Abs(delta)<=86400&&Finite(ratio),"Bounded actual production scalars required");
 foreach(var f in typeof(BuildingResourceStorageComponent).GetFields(BindingFlags.Instance|BindingFlags.Public))if(f.FieldType==typeof(float))Need(Finite((float)f.GetValue(storage)),"Finite storage input required");
 Need(Math.Abs((long)storage.OilStorageCapacity)<=1000000000&&Math.Abs((long)storage.FuelStorageCapacity)<=1000000000,"Storage capacity input bound exceeded");
 if(!hasProfile)return;
 foreach(var f in typeof(BuildingResourceProductionProfileComponent).GetFields(BindingFlags.Instance|BindingFlags.Public))Need(Finite((float)f.GetValue(profile)),"Finite profile input required");
 Need(profile.FuelBatchProgressSeconds>=0&&profile.FuelBatchProgressSeconds<=86400,"Unsupported profile progress input");
 if(profile.OilPerPeriod<=0&&profile.OilPerFuelBatch>0&&profile.FuelPerBatch>0&&profile.FuelBatchSeconds>0){Need((profile.FuelBatchProgressSeconds+Math.Max(0,delta))/profile.FuelBatchSeconds<int.MaxValue&&Math.Max(0,storage.StoredOilBarrels-storage.ReservedOilOutboundBarrels)/profile.OilPerFuelBatch<int.MaxValue&&Math.Max(0,storage.FuelStorageCapacity-storage.StoredFuelBarrels-storage.ReservedFuelInboundBarrels)/profile.FuelPerBatch<int.MaxValue,"Batch integer-overflow input unsupported");}
 }
}
