using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class TacticalResourceOwnershipArchitectureTests
{
    private const string SourceRoot = "Assets/Game/Scripts";
    private const string FactionEconomyPath =
        "Assets/Game/Scripts/Components/FactionAIComponents.cs";
    private const string TacticalMaterialsPath =
        "Assets/Game/Scripts/Components/FactionTacticalMaterialsComponents.cs";

    private static readonly Regex ComponentDeclarationRegex = new(
        @"public\s+struct\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*IComponentData\s*\{(?<body>[\s\S]*?)\n\s*\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TacticalCreditsFieldRegex = new(
        @"\b(?:public|internal)\s+int\s+(?<name>Credits|Money)\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TacticalMaterialsFieldRegex = new(
        @"\b(?:public|internal)\s+int\s+(?<name>Materials|TacticalMaterials)\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FactionMaterialsComponentRegex = new(
        @"^Faction[A-Za-z0-9_]*Materials[A-Za-z0-9_]*(?:Component|Wallet)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void RunFocusedValidation()
    {
        try
        {
            var test = new TacticalResourceOwnershipArchitectureTests();
            test.ProductionEcs_HasOneTacticalCreditsAndMaterialsAuthority();
            Debug.Log("[TacticalResourceOwnershipArchitecture] result=Passed tests=1");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[TacticalResourceOwnershipArchitecture] result=Failed\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ProductionEcs_HasOneTacticalCreditsAndMaterialsAuthority()
    {
        Assert.IsTrue(File.Exists(FactionEconomyPath), $"Missing {FactionEconomyPath}.");
        Assert.IsTrue(File.Exists(TacticalMaterialsPath), $"Missing {TacticalMaterialsPath}.");

        int factionEconomyCount = 0;
        int tacticalMaterialsCount = 0;
        int creditsOwnerCount = 0;
        int materialsOwnerCount = 0;
        string[] files = Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);

        for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
        {
            string path = files[fileIndex].Replace('\\', '/');
            string source = File.ReadAllText(path);
            StringAssert.DoesNotContain(
                "_dollars",
                source,
                $"{path} reintroduces the removed managed tactical Credits authority.");

            MatchCollection declarations = ComponentDeclarationRegex.Matches(source);
            for (int declarationIndex = 0; declarationIndex < declarations.Count; declarationIndex++)
            {
                Match declaration = declarations[declarationIndex];
                string typeName = declaration.Groups["name"].Value;
                string body = declaration.Groups["body"].Value;

                if (typeName == "FactionEconomy")
                    factionEconomyCount++;
                if (typeName == "FactionTacticalMaterialsComponent")
                    tacticalMaterialsCount++;

                MatchCollection creditsFields = TacticalCreditsFieldRegex.Matches(body);
                for (int fieldIndex = 0; fieldIndex < creditsFields.Count; fieldIndex++)
                {
                    creditsOwnerCount++;
                    Assert.AreEqual(
                        "FactionEconomy",
                        typeName,
                        $"{path} declares tactical Credits field `{creditsFields[fieldIndex].Value}` on `{typeName}`.");
                    Assert.AreEqual(
                        "Money",
                        creditsFields[fieldIndex].Groups["name"].Value,
                        "FactionEconomy.Money is the locked tactical Credits field.");
                }

                Assert.IsFalse(
                    TacticalMaterialsFieldRegex.IsMatch(body),
                    $"{path} declares a parallel Materials currency field on `{typeName}`. Use FactionTacticalMaterialsComponent.Current.");

                if (!FactionMaterialsComponentRegex.IsMatch(typeName))
                    continue;

                materialsOwnerCount++;
                Assert.AreEqual(
                    "FactionTacticalMaterialsComponent",
                    typeName,
                    $"{path} declares another faction Materials owner `{typeName}`.");
            }
        }

        Assert.AreEqual(1, factionEconomyCount, "FactionEconomy must have one production declaration.");
        Assert.AreEqual(1, tacticalMaterialsCount, "FactionTacticalMaterialsComponent must have one production declaration.");
        Assert.AreEqual(1, creditsOwnerCount, "FactionEconomy.Money must be the only tactical Credits currency field.");
        Assert.AreEqual(1, materialsOwnerCount, "FactionTacticalMaterialsComponent must be the only faction Materials owner.");
    }
}
