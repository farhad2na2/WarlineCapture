using System;
using System.Collections.Generic;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishAuthoredDefinitionLookupTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (UnityEngine.Object asset in created)
                UnityEngine.Object.DestroyImmediate(asset);
            created.Clear();
        }

        [Test]
        public void PackagedConversionPreservesEveryPilotDefinition()
        {
            var catalog = Create<SkirmishExpansionCatalogConfig>();
            string[] ids = { "S026", "S049", "S050", "S074", "S097", "S098" };
            var definitions = new SkirmishScenarioDefinitionConfig[ids.Length];
            for (int i = 0; i < ids.Length; i++) definitions[i] = Definition(ids[i]);
            SetDefinitions(catalog, definitions);

            SkirmishExpansionAuthoredSet authored = catalog.ToAuthoredSet();
            Assert.AreEqual(ids.Length, authored.Definitions.Count);
            for (int i = 0; i < ids.Length; i++)
            {
                Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, ids[i], out var resolved));
                Assert.AreSame(definitions[i], resolved);
            }

            SetDefinitions(catalog, Definition("S120"));
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, "S026", out _),
                "A converted set owns its definition list snapshot.");
        }

        [Test]
        public void UnknownAndNonExactIdentitiesFailWithoutFallback()
        {
            var authored = new SkirmishExpansionAuthoredSet
            {
                DefinitionS002 = Definition("S002"),
                Definitions = new[] { Definition("S026") }
            };
            foreach (string id in new[] { null, "", "S001", "S027", "s026", " S026", "S026 " })
            {
                Assert.IsFalse(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, id, out var resolved), id);
                Assert.IsNull(resolved);
            }
            Assert.IsFalse(SkirmishExpansionCatalogFactory.TryGetDefinition(null, "S026", out _));
        }

        [Test]
        public void LegacyAdaptersRemainAuthoritativeAfterReplacement()
        {
            var catalog = Create<SkirmishExpansionCatalogConfig>();
            var originals = new[] { Definition("S002"), Definition("S003"), Definition("S004"), Definition("S005") };
            SetDefinitions(catalog, originals);
            SkirmishExpansionAuthoredSet authored = catalog.ToAuthoredSet();
            Assert.AreSame(originals[0], authored.DefinitionS002);
            Assert.AreSame(originals[1], authored.DefinitionS003);
            Assert.AreSame(originals[2], authored.DefinitionS004);
            Assert.AreSame(originals[3], authored.DefinitionS005);

            var replacement = Definition("S002");
            authored.DefinitionS002 = replacement;
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, "S002", out var resolved));
            Assert.AreSame(replacement, resolved, "Preserve packaged/fixture compatibility field replacement.");
        }

        [Test]
        public void NullSlotsAreSkippedAndNullCollectionsFailCleanly()
        {
            var definition = Definition("S120");
            var authored = new SkirmishExpansionAuthoredSet { Definitions = new[] { null, definition } };
            Assert.IsTrue(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, "S120", out var resolved));
            Assert.AreSame(definition, resolved);
            authored.Definitions = null;
            Assert.IsFalse(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, "S120", out resolved));
            Assert.IsNull(resolved);
        }

        [Test]
        public void DuplicateGenericIdentityFailsRatherThanChoosingFirst()
        {
            var authored = new SkirmishExpansionAuthoredSet
            {
                Definitions = new[] { Definition("S026"), Definition("S026") }
            };
            Assert.IsFalse(SkirmishExpansionCatalogFactory.TryGetDefinition(authored, "S026", out var resolved));
            Assert.IsNull(resolved);
        }

        private T Create<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        private SkirmishScenarioDefinitionConfig Definition(string id)
        {
            var definition = Create<SkirmishScenarioDefinitionConfig>();
            definition.Configure(id, null, null, null, null, null, null, null, null);
            return definition;
        }

        private static void SetDefinitions(SkirmishExpansionCatalogConfig catalog,
            params SkirmishScenarioDefinitionConfig[] definitions)
        {
            var serialized = new SerializedObject(catalog);
            SerializedProperty array = serialized.FindProperty("definitions");
            array.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishAuthoredDefinitionLookupTests();
            try
            {
                suite.PackagedConversionPreservesEveryPilotDefinition();
                suite.UnknownAndNonExactIdentitiesFailWithoutFallback();
                suite.LegacyAdaptersRemainAuthoritativeAfterReplacement();
                suite.NullSlotsAreSkippedAndNullCollectionsFailCleanly();
                suite.DuplicateGenericIdentityFailsRatherThanChoosingFirst();
                Debug.Log("[SkirmishAuthoredDefinitionLookupTests] result=Passed");
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishAuthoredDefinitionLookupTests] result=Failed\n" + exception);
                throw;
            }
            finally { suite.Cleanup(); }
        }
    }
}
