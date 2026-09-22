using UnityEngine;

namespace Game.Configs
{
    public static class SkirmishGroundStagingBuilder
    {
        public const string RootName = "Building_GroundStaging";
        public const string VehicleQueueName = "VehicleQueue";
        public const string LogisticsQueueName = "LogisticsQueue";
        public const string SpawnPadName = "SpawnPad";
        public const string RallyPadName = "RallyPad";
        public const string HealthName = "Health";
        public const string SelectionName = "Selection";
        public const string BuildControlsName = "BuildControls";

        public static GameObject BuildHierarchy(SkirmishGroundStagingConfig config = null)
        {
            int vehicleQueues = config != null ? Mathf.Max(1, config.VehicleQueues) : 1;
            int logisticsQueues = config != null ? Mathf.Max(1, config.LogisticsQueues) : 1;
            var root = new GameObject(RootName);
            root.hideFlags = HideFlags.HideAndDontSave;
            var identity = root.AddComponent<SkirmishGroundStagingIdentity>();
            identity.ConfigureEstablished();

            AddSurface(root.transform, VehicleQueueName, new Vector3(0f, 0.05f, 2.4f), new Vector3(6f, 0.1f, 2.2f),
                new Color(0.35f, 0.45f, 0.22f, 1f));
            AddSurface(root.transform, LogisticsQueueName, new Vector3(0f, 0.05f, -2.4f), new Vector3(6f, 0.1f, 2.2f),
                new Color(0.32f, 0.38f, 0.48f, 1f));
            AddSurface(root.transform, SpawnPadName, new Vector3(4.5f, 0.02f, 0f), new Vector3(3.2f, 0.06f, 3.2f),
                new Color(0.55f, 0.5f, 0.28f, 1f));
            AddSurface(root.transform, RallyPadName, new Vector3(8.5f, 0.02f, 0f), new Vector3(2.4f, 0.06f, 2.4f),
                new Color(0.5f, 0.42f, 0.22f, 1f));
            AddSocket(root.transform, HealthName, new Vector3(0f, 2.2f, 0f));
            AddSocket(root.transform, SelectionName, new Vector3(0f, 0.4f, 0f));
            AddSocket(root.transform, BuildControlsName, new Vector3(-2.2f, 1.2f, 0f));

            var yard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yard.name = "YardDeck";
            yard.transform.SetParent(root.transform, false);
            yard.transform.localScale = new Vector3(8f, 0.4f, 6f);
            yard.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            ApplyColor(yard, new Color(0.42f, 0.4f, 0.34f, 1f));
            DisableCollider(yard);

            _ = vehicleQueues;
            _ = logisticsQueues;
            return root;
        }

        public static bool HasRequiredSurfaces(GameObject root)
        {
            if (root == null || root.name != RootName)
                return false;
            if (root.name.IndexOf("Expert", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                root.name.IndexOf("Tent", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return FindChild(root.transform, VehicleQueueName) != null &&
                   FindChild(root.transform, LogisticsQueueName) != null &&
                   FindChild(root.transform, SpawnPadName) != null &&
                   FindChild(root.transform, RallyPadName) != null &&
                   FindChild(root.transform, HealthName) != null &&
                   FindChild(root.transform, SelectionName) != null &&
                   FindChild(root.transform, BuildControlsName) != null &&
                   root.GetComponent<SkirmishGroundStagingIdentity>() != null &&
                   !root.GetComponent<SkirmishGroundStagingIdentity>().IsExpertTent;
        }

        public static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static void AddSurface(Transform parent, string name, Vector3 local, Vector3 scale, Color color)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = name;
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = local;
            pad.transform.localScale = scale;
            ApplyColor(pad, color);
            DisableCollider(pad);
        }

        private static void AddSocket(Transform parent, string name, Vector3 local)
        {
            var socket = new GameObject(name);
            socket.transform.SetParent(parent, false);
            socket.transform.localPosition = local;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;
            renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = color };
        }

        private static void DisableCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }
    }
}
