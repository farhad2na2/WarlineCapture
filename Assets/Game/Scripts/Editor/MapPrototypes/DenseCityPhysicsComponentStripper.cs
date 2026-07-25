using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal readonly struct DenseCityPhysicsStripResult
    {
        internal DenseCityPhysicsStripResult(
            int colliders3D,
            int colliders2D,
            int rigidbodies3D,
            int rigidbodies2D)
        {
            Colliders3D = colliders3D;
            Colliders2D = colliders2D;
            Rigidbodies3D = rigidbodies3D;
            Rigidbodies2D = rigidbodies2D;
        }

        internal int Colliders3D { get; }
        internal int Colliders2D { get; }
        internal int Rigidbodies3D { get; }
        internal int Rigidbodies2D { get; }
        internal int Total => Colliders3D + Colliders2D + Rigidbodies3D + Rigidbodies2D;
    }

    internal static class DenseCityPhysicsComponentStripper
    {
        internal static GameObject CreatePrimitiveWithoutPhysics(PrimitiveType primitiveType)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            try
            {
                StripInstanceHierarchy(primitive);
                return primitive;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(primitive);
                throw;
            }
        }

        internal static GameObject InstantiatePrefabWithoutPhysics(
            GameObject prefab,
            Transform parent)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Dense-city prefab instantiation failed: '{AssetDatabase.GetAssetPath(prefab)}'.");
            }

            try
            {
                StripInstanceHierarchy(instance);
                return instance;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }
        }

        internal static bool TryValidateNoProhibitedComponents(
            GameObject root,
            out string error)
        {
            if (root == null)
            {
                error = "Dense-city physics validation requires a hierarchy root.";
                return false;
            }

            if (TryFindProhibited(root, out Component prohibited))
            {
                error = $"Dense-city generated hierarchy contains prohibited " +
                        $"{prohibited.GetType().Name} at '{GetPath(prohibited.transform)}'.";
                return false;
            }

            error = null;
            return true;
        }

        internal static DenseCityPhysicsStripResult StripInstanceHierarchy(GameObject instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));
            if (EditorUtility.IsPersistent(instanceRoot))
            {
                throw new InvalidOperationException(
                    "Dense-city physics stripping is instance-only and cannot mutate persistent assets.");
            }

            Rigidbody[] rigidbodies3D = instanceRoot.GetComponentsInChildren<Rigidbody>(true);
            Rigidbody2D[] rigidbodies2D = instanceRoot.GetComponentsInChildren<Rigidbody2D>(true);
            Collider[] colliders3D = instanceRoot.GetComponentsInChildren<Collider>(true);
            Collider2D[] colliders2D = instanceRoot.GetComponentsInChildren<Collider2D>(true);

            DestroyComponents(rigidbodies3D);
            DestroyComponents(rigidbodies2D);
            DestroyComponents(colliders3D);
            DestroyComponents(colliders2D);
            return new DenseCityPhysicsStripResult(
                colliders3D.Length,
                colliders2D.Length,
                rigidbodies3D.Length,
                rigidbodies2D.Length);
        }

        private static void DestroyComponents<T>(T[] components) where T : Component
        {
            for (int index = components.Length - 1; index >= 0; index--)
            {
                if (components[index] != null)
                    UnityEngine.Object.DestroyImmediate(components[index]);
            }
        }

        private static bool TryFindProhibited(GameObject root, out Component prohibited)
        {
            Component[][] componentSets =
            {
                root.GetComponentsInChildren<Collider>(true),
                root.GetComponentsInChildren<Collider2D>(true),
                root.GetComponentsInChildren<Rigidbody>(true),
                root.GetComponentsInChildren<Rigidbody2D>(true)
            };
            for (int setIndex = 0; setIndex < componentSets.Length; setIndex++)
            {
                Component[] components = componentSets[setIndex];
                if (components.Length == 0)
                    continue;
                prohibited = components[0];
                return true;
            }

            prohibited = null;
            return false;
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
