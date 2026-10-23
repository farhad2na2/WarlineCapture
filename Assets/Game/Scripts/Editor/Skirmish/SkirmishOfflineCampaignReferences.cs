#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Configs;
using UnityEditor;

namespace Game.Editor.SkirmishOffline
{
    // Reads the exact serialized Menu authority without opening or saving a scene.
    // Unsupported YAML forms fail closed instead of selecting a similarly named catalog.
    public static class SkirmishOfflineCampaignReferences
    {
        public const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        public const string BootstrapSourcePath = "Assets/Game/Scripts/Composition/MenuBootstrapView.cs";
        public sealed class SerializedReferences
        {
            public long BootstrapFileId, MissionCatalogFileId, OperationMapCatalogFileId;
            public string BootstrapScriptGuid, MissionCatalogGuid, OperationMapCatalogGuid;
        }
        public sealed class ReferenceSet
        {
            public string MenuPath, BootstrapScriptPath, MissionCatalogPath, OperationMapCatalogPath;
            public string MissionCatalogGuid, OperationMapCatalogGuid;
            public long BootstrapFileId, MissionCatalogFileId, OperationMapCatalogFileId;
            public MissionDefinitionCatalogConfig MissionCatalog;
            public OperationMapCatalogConfig OperationMapCatalog;
        }
        private static readonly Regex Headers = new Regex(@"^--- !u!(\d+) &(-?\d+)( stripped)?[ \t]*\r?$", RegexOptions.Multiline);
        private static readonly Regex Reference = new Regex(@"^  ([A-Za-z_][A-Za-z_0-9]*): \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: ([23])\}[ \t]*$");
        private static string[] Lines(string body) => body.Replace("\r\n", "\n").Split('\n');
        private static (long id, string guid, int type) ReadReference(string body, string field)
        {
            var lines = Lines(body).Where(x => x.StartsWith("  " + field + ":", StringComparison.Ordinal)).ToArray();
            if (lines.Length != 1) throw new InvalidDataException("Expected exactly one serialized " + field + " reference.");
            var match = Reference.Match(lines[0]);
            if (!match.Success || !long.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id) || id == 0)
                throw new InvalidDataException("Unsupported or null serialized " + field + " reference.");
            return (id, match.Groups[3].Value, int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture));
        }
        public static SerializedReferences Parse(string sceneText, string bootstrapScriptGuid)
        {
            if (sceneText == null || sceneText.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal))
                throw new InvalidDataException("Menu scene payload unavailable.");
            if (bootstrapScriptGuid == null || !Regex.IsMatch(bootstrapScriptGuid, @"\A[0-9a-f]{32}\z"))
                throw new InvalidDataException("Explicit MenuBootstrapView script GUID required.");
            var headers = Headers.Matches(sceneText); SerializedReferences selected = null;
            for (int i = 0; i < headers.Count; i++)
            {
                var header = headers[i]; if (header.Groups[1].Value != "114") continue;
                int begin = header.Index + header.Length;
                string body = sceneText.Substring(begin, (i + 1 < headers.Count ? headers[i + 1].Index : sceneText.Length) - begin);
                var scriptLines = Lines(body).Where(x => x.StartsWith("  m_Script:", StringComparison.Ordinal)).ToArray();
                if (!scriptLines.Any(x => x.Contains(bootstrapScriptGuid))) continue;
                var script = ReadReference(body, "m_Script");
                if (script.guid != bootstrapScriptGuid || script.id != 11500000 || script.type != 3 || header.Groups[3].Success)
                    throw new InvalidDataException("Unsupported MenuBootstrapView serialization.");
                if (selected != null) throw new InvalidDataException("Ambiguous MenuBootstrapView components; no catalog selected.");
                if (Lines(body).Count(x => x.StartsWith("  m_Enabled:", StringComparison.Ordinal)) != 1 || !Lines(body).Contains("  m_Enabled: 1"))
                    throw new InvalidDataException("MenuBootstrapView must be explicitly enabled.");
                var missions = ReadReference(body, "campaignMissionCatalog");
                var maps = ReadReference(body, "campaignOperationMapCatalog");
                if (missions.type != 2 || maps.type != 2) throw new InvalidDataException("Campaign references must identify serialized assets.");
                selected = new SerializedReferences { BootstrapFileId = long.Parse(header.Groups[2].Value, CultureInfo.InvariantCulture),
                    BootstrapScriptGuid = script.guid, MissionCatalogGuid = missions.guid, MissionCatalogFileId = missions.id,
                    OperationMapCatalogGuid = maps.guid, OperationMapCatalogFileId = maps.id };
            }
            return selected ?? throw new InvalidDataException("MenuBootstrapView reference owner missing.");
        }
        private static string Payload(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Required source payload missing.", path);
            string text = File.ReadAllText(path);
            if (text.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal))
                throw new InvalidDataException("LFS pointer without payload: " + path);
            return text;
        }
        public static T LoadExact<T>(string guid, long localId, out string path) where T : UnityEngine.Object
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidDataException("Referenced Campaign asset GUID has no local Assets path: " + guid);
            Payload(path);
            T found = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is T typed && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string actualGuid, out long actualId) && actualGuid == guid && actualId == localId)
                {
                    if (found != null) throw new InvalidDataException("Ambiguous Campaign asset identity: " + path);
                    found = typed;
                }
            return found != null ? found : throw new InvalidDataException("Campaign reference type/local file ID mismatch: " + path);
        }
        public static ReferenceSet ReadCurrent()
        {
            Payload(BootstrapSourcePath);
            string guid = AssetDatabase.AssetPathToGUID(BootstrapSourcePath);
            var refs = Parse(Payload(MenuScenePath), guid);
            var missions = LoadExact<MissionDefinitionCatalogConfig>(refs.MissionCatalogGuid, refs.MissionCatalogFileId, out string missionPath);
            var maps = LoadExact<OperationMapCatalogConfig>(refs.OperationMapCatalogGuid, refs.OperationMapCatalogFileId, out string mapPath);
            return new ReferenceSet { MenuPath = MenuScenePath, BootstrapScriptPath = BootstrapSourcePath,
                MissionCatalogPath = missionPath, OperationMapCatalogPath = mapPath, MissionCatalog = missions, OperationMapCatalog = maps,
                MissionCatalogGuid = refs.MissionCatalogGuid, OperationMapCatalogGuid = refs.OperationMapCatalogGuid,
                MissionCatalogFileId = refs.MissionCatalogFileId, OperationMapCatalogFileId = refs.OperationMapCatalogFileId, BootstrapFileId = refs.BootstrapFileId };
        }
    }
}
#endif
