
using System;
using System.Diagnostics;
class TaskkillStderrProbe {
 static int Main(string[] args) {
  int id=int.Parse(args[1]);
  Console.WriteLine("OWNED_SYNTHETIC_TASKKILL_TARGET="+id);
  if(Environment.GetEnvironmentVariable("WARLINE_TEST_TASKKILL_MODE")=="killthenstderr") {
   try { Process.GetProcessById(id).Kill(); } catch(ArgumentException) {}
  }
  Console.Error.WriteLine("ERROR: Owned synthetic child could not be terminated: already ended or intentional failure.");
  return 128;
 }
}
