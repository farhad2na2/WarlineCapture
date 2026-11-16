using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal static class SourceCameraFunctionReferenceControls
    {
        private static int negatives;
        private static void Need(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Rejects(string name, Action action)
        {
            try { action(); }
            catch (Exception) { negatives++; return; }
            throw new InvalidOperationException("Negative camera function control accepted: " + name);
        }

        internal static int Main(string[] args)
        {
            try
            {
                Need(args.Length == 1, "Repository root argument required");
                string root = Path.GetFullPath(args[0]);
                const string adapterSha = "57e1d7569b8d8f773bbcb68f50ee342f71e41cf4d92aeec11ebc1df525237d91";
                const string functionSha = "26188d80946e4d287a085dff6e01a3aafa2fd8f1ff2788b59ff376372cf3760b";
                const string valuesSha = "d37c03922b44fbfc10cfcf371244569851145ce142d0a13c2246922c578e182f";
                const string motionSha = "502b3ec9f76282f92abe84264f01c8a99e4e9e871ce00bf97a4400bffa549925";
                string adapterPath = Path.Combine(root, "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s026-current-camera-adapter-candidate.json");
                var request = new JObject { ["camera_engine_adapter"] = new JObject { ["path"] = adapterPath, ["sha256"] = adapterSha } };
                Need(SourceNumericCameraEngineAdapterContract.FunctionProfileSha(request) == functionSha,
                    "Pinned adapter did not select its exact historical function profile");
                var contract = SourceNumericCameraEngineAdapterContract.ReadEngineering(request, functionSha);
                Need(contract.Engineering && contract.ProfileSha == functionSha && contract.ValuesSha == valuesSha &&
                    contract.MotionSha == motionSha && contract.ContractSha == adapterSha,
                    "Validated adapter/function/oracle tuple changed");

                var wrongAdapterHash = (JObject)request.DeepClone();
                wrongAdapterHash["camera_engine_adapter"]["sha256"] = new string('0', 64);
                Rejects("unknown adapter digest", () => SourceNumericCameraEngineAdapterContract.FunctionProfileSha(wrongAdapterHash));
                Rejects("adapter/profile disagreement", () => SourceNumericCameraEngineAdapterContract.ReadEngineering(request, new string('0', 64)));
                var wrongPath = (JObject)request.DeepClone();
                wrongPath["camera_engine_adapter"]["path"] = Path.Combine(root, "Tools/CI/SkirmishSourceSimulation/Adapters/SourceNumericCameraEngineAdapterContract.cs");
                Rejects("adapter bytes at wrong pinned path", () => SourceNumericCameraEngineAdapterContract.FunctionProfileSha(wrongPath));
                Need(negatives == 3, "Exact three camera adapter mismatch controls required");
                Console.WriteLine("[SourceCameraFunctionReferenceControls] result=Passed positive=1 negative=3 nativeCameraContextQualified=False missionReady=False");
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[SourceCameraFunctionReferenceControls] result=Failed type=" + e.GetType().Name + " message=" + e.Message);
                return 1;
            }
        }
    }
}
