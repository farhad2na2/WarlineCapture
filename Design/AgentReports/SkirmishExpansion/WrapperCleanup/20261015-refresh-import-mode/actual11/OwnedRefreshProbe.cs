
using System;
using System.IO;
class Probe {
 static string Arg(string[] args,string key) {
  for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];return "";
 }
 static int Main(string[] args) {
  File.WriteAllLines(Environment.GetEnvironmentVariable("WARLINE_WRAPPER_ARG_PROBE"),args);
  string method=Arg(args,"-executeMethod");
  File.AppendAllText(Arg(args,"-logFile"),method=="Probe.Missing"?"owned missing marker\n":
   method=="Probe.Failed"?"[OwnedRefreshProbe] result=Failed\n":"[OwnedRefreshProbe] result=Passed\n");
  return method=="Probe.Nonzero"?37:0;
 }
}
