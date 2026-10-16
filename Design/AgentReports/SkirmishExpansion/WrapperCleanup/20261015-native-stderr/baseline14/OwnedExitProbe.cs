
using System;
using System.IO;
using System.Threading;
class ExitProbe {
 static string Arg(string[] args,string key,string fallback) {
   for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];
   return fallback;
 }
 static int Main(string[] args) {
   string log=Arg(args,"-logFile","");
   if(Arg(args,"--fatal","0")=="1")File.AppendAllText(log,"\nCrash!!! owned synthetic child only\n");
   if(Arg(args,"--fatal","0")=="2")File.AppendAllText(log,"\nAssets/Game/Scripts/OwnedSyntheticProbe.cs(5,13): error CS0234: Synthetic compiler failure only\n");
   if(Arg(args,"--fatal","0")=="3")File.AppendAllText(log,"\nAssets/Game/Scripts/OwnedSyntheticProbe.cs(5,13): warning CS0168: Synthetic warning only\n");
   if(Arg(args,"--fatal","0")=="4")File.AppendAllText(log,"\nAssets/Game/Scripts/OwnedSyntheticProbe.cs(5,13):\nerror CS0234: Separate synthetic line only\n");
   if(Arg(args,"--fatal","0")=="5")File.AppendAllText(log,"\nUnity.Entities.CodeGen.EntitiesILPostProcessors: ILPostProcessor has thrown an exception: System.ArgumentException: OwnedSyntheticRecord requires a default constructor.\n");
   Thread.Sleep(int.Parse(Arg(args,"--sleep","0")));
   return int.Parse(Arg(args,"--exit","0"));
 }
}
