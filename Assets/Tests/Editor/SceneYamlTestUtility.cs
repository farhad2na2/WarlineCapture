using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

internal sealed class SceneYamlTestUtility
{
    private readonly Dictionary<string, string> _blocksByFileId = new();

    private SceneYamlTestUtility(string text)
    {
        MatchCollection matches = Regex.Matches(
            text.Replace("\r\n", "\n"),
            @"(?ms)^--- !u!\d+ &(-?\d+)\n.*?(?=^--- !u!|\z)");

        foreach (Match match in matches)
            _blocksByFileId[match.Groups[1].Value] = match.Value;
    }

    public static SceneYamlTestUtility Load(string path)
    {
        return new SceneYamlTestUtility(File.ReadAllText(path));
    }

    public string FindRequiredBlockContaining(string value)
    {
        foreach (string block in _blocksByFileId.Values)
        {
            if (block.IndexOf(value, StringComparison.Ordinal) >= 0)
                return block;
        }

        Assert.Fail($"Scene YAML does not contain block marker: {value}");
        return string.Empty;
    }

    public string GetRequiredFieldFileId(string block, string fieldName)
    {
        Match match = Regex.Match(block, @"(?m)^\s*" + Regex.Escape(fieldName) + @": \{fileID: (-?\d+)\}");
        Assert.IsTrue(match.Success, $"{fieldName} must be assigned in the serialized scene.");
        Assert.AreNotEqual("0", match.Groups[1].Value, $"{fieldName} must not point to fileID 0.");
        return match.Groups[1].Value;
    }

    public string GetRequiredGameObjectNameForReference(string referenceFileId)
    {
        string gameObjectBlock = GetRequiredGameObjectBlockForReference(referenceFileId);
        Match match = Regex.Match(gameObjectBlock, @"(?m)^\s*m_Name:\s*(.*)$");
        Assert.IsTrue(match.Success, $"Reference {referenceFileId} must resolve to a named GameObject.");
        return match.Groups[1].Value.TrimEnd();
    }

    public bool GetRequiredActiveStateForReference(string referenceFileId)
    {
        string gameObjectBlock = GetRequiredGameObjectBlockForReference(referenceFileId);
        Match match = Regex.Match(gameObjectBlock, @"(?m)^\s*m_IsActive:\s*([01])$");
        Assert.IsTrue(match.Success, $"Reference {referenceFileId} must resolve to a GameObject active state.");
        return match.Groups[1].Value == "1";
    }

    public IReadOnlyList<string> GetDropdownOptionTexts(string dropdownFileId)
    {
        string dropdownBlock = GetRequiredBlock(dropdownFileId);
        MatchCollection matches = Regex.Matches(dropdownBlock, @"(?m)^\s*-\s*m_Text:\s*(.*)$");
        var options = new List<string>(matches.Count);

        foreach (Match match in matches)
            options.Add(match.Groups[1].Value.TrimEnd());

        return options;
    }

    public void AssertPersistentCallsAreEmpty(string componentFileId, string description)
    {
        string block = GetRequiredBlock(componentFileId);
        Assert.That(block, Does.Contain("m_Calls: []"), $"{description} should not keep imported persistent callbacks.");
    }

    public string GetRectTransformParentFileIdForReference(string referenceFileId)
    {
        string gameObjectFileId = GetGameObjectFileIdForReference(referenceFileId);
        string rectTransformBlock = GetRequiredComponentBlockForGameObject(gameObjectFileId, "224");
        Match match = Regex.Match(rectTransformBlock, @"(?m)^\s*m_Father: \{fileID: (-?\d+)\}");
        Assert.IsTrue(match.Success, $"Reference {referenceFileId} must have a RectTransform parent.");
        return match.Groups[1].Value;
    }

    public string GetRectTransformFileIdForReference(string referenceFileId)
    {
        string gameObjectFileId = GetGameObjectFileIdForReference(referenceFileId);
        return GetRequiredComponentFileIdForGameObject(gameObjectFileId, "224");
    }

    private string GetRequiredGameObjectBlockForReference(string referenceFileId)
    {
        string gameObjectFileId = GetGameObjectFileIdForReference(referenceFileId);
        string block = GetRequiredBlock(gameObjectFileId);
        Assert.That(block, Does.StartWith("--- !u!1 "), $"Reference {referenceFileId} did not resolve to a GameObject.");
        return block;
    }

    private string GetGameObjectFileIdForReference(string referenceFileId)
    {
        string block = GetRequiredBlock(referenceFileId);
        if (block.StartsWith("--- !u!1 ", StringComparison.Ordinal))
            return referenceFileId;

        Match match = Regex.Match(block, @"(?m)^\s*m_GameObject: \{fileID: (-?\d+)\}");
        Assert.IsTrue(match.Success, $"Reference {referenceFileId} must resolve through m_GameObject.");
        return match.Groups[1].Value;
    }

    private string GetRequiredComponentFileIdForGameObject(string gameObjectFileId, string unityTypeId)
    {
        foreach (KeyValuePair<string, string> pair in _blocksByFileId)
        {
            if (pair.Value.StartsWith("--- !u!" + unityTypeId + " ", StringComparison.Ordinal) &&
                pair.Value.IndexOf("m_GameObject: {fileID: " + gameObjectFileId + "}", StringComparison.Ordinal) >= 0)
            {
                return pair.Key;
            }
        }

        Assert.Fail($"GameObject fileID {gameObjectFileId} must have component type {unityTypeId}.");
        return string.Empty;
    }

    private string GetRequiredComponentBlockForGameObject(string gameObjectFileId, string unityTypeId)
    {
        return GetRequiredBlock(GetRequiredComponentFileIdForGameObject(gameObjectFileId, unityTypeId));
    }

    private string GetRequiredBlock(string fileId)
    {
        Assert.IsTrue(_blocksByFileId.TryGetValue(fileId, out string block), $"Scene YAML is missing fileID {fileId}.");
        return block;
    }
}
