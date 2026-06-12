using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class MatchSceneAuthoringNameValidationTests
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private static readonly Regex RuntimeUnitSceneNameRegex = new(
        @"(?m)^\s*m_Name:\s*Unit_(?:Veh|Chr)_",
        RegexOptions.Compiled);

    [Test]
    public void MatchScene_DoesNotContainRuntimeUnitIdNamedAuthoringObjects()
    {
        string sceneYaml = File.ReadAllText(MatchScenePath);
        Match match = RuntimeUnitSceneNameRegex.Match(sceneYaml);

        Assert.IsFalse(
            match.Success,
            match.Success
                ? $"Match scene authoring GameObjects must not use runtime unit ID names. Rename '{match.Value.Trim()}' to a map-decoration name."
                : string.Empty);
    }
}
