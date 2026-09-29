using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools;

[assembly: TestPlayerBuildModifier(typeof(Game.Editor.MapVariants.MapVariantNativeTestPlayerBuildModifier))]

namespace Game.Editor.MapVariants
{
    public sealed class MapVariantNativeTestPlayerBuildModifier : ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions options)
        {
            // Other test-player builds keep their normal assembly selection.
            if(options.target==BuildTarget.StandaloneOSX &&
               Path.GetFullPath(options.locationPathName).StartsWith(Path.GetFullPath("Build/MapVariantPreparedPlayer")+Path.DirectorySeparatorChar,StringComparison.Ordinal))
            {
                options.options &= ~BuildOptions.ConnectWithProfiler;
                options.extraScriptingDefines=(options.extraScriptingDefines ?? Array.Empty<string>())
                    .Concat(new[]{"WARLINE_MAP_NATIVE_TEST_PLAYER"}).Distinct().ToArray();
            }
            return options;
        }
    }
}
