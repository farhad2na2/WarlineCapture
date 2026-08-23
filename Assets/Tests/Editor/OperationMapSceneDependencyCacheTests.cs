using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;

namespace Game.Tests.Editor
{
    public sealed class OperationMapSceneDependencyCacheTests
    {
        [Test]
        public void ProjectBakersAndBakingSystems_HaveExplicitVersions()
        {
            var missing = new List<string>();
            Type bakerBaseType = Type.GetType(
                "Unity.Entities.IBaker, Unity.Entities.Hybrid",
                throwOnError: true);
            foreach (Type bakerType in TypeCache.GetTypesDerivedFrom(bakerBaseType))
            {
                if (IsProjectAssembly(bakerType) && !HasBakingVersion(bakerType))
                    missing.Add(bakerType.FullName);
            }

            IReadOnlyList<Type> bakingSystems = DefaultWorldInitialization.GetAllSystems(
                WorldSystemFilterFlags.BakingSystem |
                WorldSystemFilterFlags.EntitySceneOptimizations);
            for (int index = 0; index < bakingSystems.Count; index++)
            {
                Type systemType = bakingSystems[index];
                if (IsProjectAssembly(systemType) && !HasBakingVersion(systemType))
                    missing.Add(systemType.FullName);
            }

            missing.Sort(StringComparer.Ordinal);
            Assert.That(
                missing,
                Is.Empty,
                "Unversioned project baking types make Unity Entities depend on their entire " +
                "assembly and invalidate every SubScene after unrelated script changes:\n" +
                string.Join("\n", missing));
        }

        private static bool IsProjectAssembly(Type type)
        {
            return type != null &&
                   type.Assembly.GetName().Name.StartsWith("Game.", StringComparison.Ordinal);
        }

        private static bool HasBakingVersion(Type type)
        {
            return type.GetCustomAttribute<BakingVersionAttribute>(inherit: false) != null;
        }
    }
}
