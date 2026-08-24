using Game.Components;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Authoring
{
    public partial class UnitGridAuthoring
    {
        private partial class UnitGridBaker
        {
            private void AddAttachedLightSetup(Transform root, Entity entity)
            {
                if (root == null)
                    return;

                Light[] lights = root.GetComponentsInChildren<Light>(true);
                if (lights == null || lights.Length == 0)
                    return;

                DynamicBuffer<UnitAttachedLightSetupElement> entries = default;
                bool hasEntries = false;
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i];
                    if (light == null)
                        continue;

                    Transform transform = light.transform;
                    if (!hasEntries)
                    {
                        entries = AddBuffer<UnitAttachedLightSetupElement>(entity);
                        hasEntries = true;
                    }

                    string lightName = string.IsNullOrWhiteSpace(light.name) ? "UnitLight" : light.name;
                    entries.Add(new UnitAttachedLightSetupElement
                    {
                        Name = new FixedString64Bytes(lightName),
                        Type = light.type,
                        Color = light.color,
                        Intensity = light.intensity,
                        Range = light.range,
                        SpotAngle = light.spotAngle,
                        InnerSpotAngle = light.innerSpotAngle,
                        CastShadows = (byte)(light.shadows != LightShadows.None ? 1 : 0),
                        LocalPosition = root.InverseTransformPoint(transform.position),
                        LocalRotation = Quaternion.Inverse(root.rotation) * transform.rotation
                    });
                }
            }

            private static bool ShouldUseDualSideAttackTrace(UnitGridAuthoring authoring)
            {
                if (authoring == null || !authoring.IsAirUnit)
                    return false;

                string sourceName = authoring.config != null ? authoring.config.name : authoring.gameObject.name;
                string display = authoring.ConfiguredDisplayName;
                return ContainsIgnoreCase(sourceName, "Veh_Helicopter_Attack") ||
                       ContainsIgnoreCase(display, "Attack Helicopter");
            }

            private static float ResolveDualSideAttackTraceLateralOffset(UnitGridAuthoring authoring)
            {
                string sourceName = authoring.config != null ? authoring.config.name : authoring.gameObject.name;
                string display = authoring.ConfiguredDisplayName;
                bool lightAttackHelicopter =
                    ContainsIgnoreCase(sourceName, "Small") ||
                    ContainsIgnoreCase(display, "Light Attack Helicopter");
                return lightAttackHelicopter ? 0.62f : 0.88f;
            }

            private static bool ContainsIgnoreCase(string value, string token)
            {
                return !string.IsNullOrEmpty(value) &&
                       !string.IsNullOrEmpty(token) &&
                       value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static int2 ResolveFootprint(UnitGridAuthoring authoring, bool hasModelBounds, Bounds modelBounds)
            {
                int2 configured = new int2(math.max(1, authoring.footprintCells.x), math.max(1, authoring.footprintCells.y));

                if (!authoring.UsesVehicleMotion)
                    return configured;

                if (configured.x > 1 || configured.y > 1)
                    return configured;

                if (!hasModelBounds)
                    return configured;

                int2 modelFootprint = new int2(
                    math.max(1, (int)math.ceil(modelBounds.size.x)),
                    math.max(1, (int)math.ceil(modelBounds.size.z)));

                if (!authoring.autoCalculateFootprint)
                    return configured;

                return modelFootprint;
            }

            private static UnitVehicleMovement ResolveVehicleMovement(UnitGridAuthoring authoring, int2 footprint, bool hasModelBounds, Bounds modelBounds)
            {
                bool isVehicle = authoring.UsesVehicleMotion;
                float modelLength = hasModelBounds ? math.max(modelBounds.size.x, modelBounds.size.z) : math.max(footprint.x, footprint.y);

                return new UnitVehicleMovement
                {
                    TurnSpeedDegrees = isVehicle ? 180f : 720f,
                    Acceleration = isVehicle ? math.max(6f, modelLength * 3f) : 999f,
                    Braking = isVehicle ? math.max(8f, modelLength * 4f) : 999f,
                    RearPivotOffset = isVehicle ? math.max(0.35f, modelLength * 0.22f) : 0f
                };
            }

            private void AddHelicopterBladeReferences(DynamicBuffer<UnitHelicopterBladeReference> bladeBuffer, Transform root)
            {
                if (root == null)
                    return;

                var stack = new Stack<Transform>();
                stack.Push(root);
                while (stack.Count > 0)
                {
                    Transform current = stack.Pop();
                    if (TryGetBladeAxis(current.name, out byte axis))
                    {
                        Entity bladeEntity = GetEntity(current.gameObject, TransformUsageFlags.Dynamic);
                        if (HasBladeReference(bladeBuffer, bladeEntity))
                        {
                            for (int i = 0; i < current.childCount; i++)
                                stack.Push(current.GetChild(i));
                            continue;
                        }

                        bladeBuffer.Add(new UnitHelicopterBladeReference
                        {
                            Blade = bladeEntity,
                            Axis = axis
                        });
                    }

                    for (int i = 0; i < current.childCount; i++)
                        stack.Push(current.GetChild(i));
                }
            }

            private static bool HasBladeReference(DynamicBuffer<UnitHelicopterBladeReference> bladeBuffer, Entity blade)
            {
                for (int i = 0; i < bladeBuffer.Length; i++)
                {
                    if (bladeBuffer[i].Blade == blade)
                        return true;
                }

                return false;
            }

            private void AddUnitVisualPrefabReferences(UnitGridAuthoring authoring, Entity entity)
            {
                if (authoring.UnitSelectionMarkerPrefab != null)
                {
                    AddComponent(entity, new UnitSelectionMarkerPrefabReference
                    {
                        Prefab = GetEntity(authoring.UnitSelectionMarkerPrefab, TransformUsageFlags.Dynamic)
                    });
                }

                if (authoring.UnitHealthBarPrefab != null)
                {
                    AddComponent(entity, new UnitHealthBarPrefabReference
                    {
                        Prefab = GetEntity(authoring.UnitHealthBarPrefab, TransformUsageFlags.Dynamic)
                    });
                }
            }

            private void AddVehicleVisualPrefabReferences(UnitGridAuthoring authoring, Entity entity)
            {
                if (authoring.VehicleDestroyedVisualPrefab != null)
                {
                    AddComponent(entity, new VehicleDestroyedVisualPrefabReference
                    {
                        Prefab = GetEntity(authoring.VehicleDestroyedVisualPrefab, TransformUsageFlags.Dynamic)
                    });
                }
            }

            private void AddTransportAirdropVisualPrefabReferences(UnitGridAuthoring authoring, Entity entity)
            {
                GameObject parachutePrefab = authoring.SoldierParachuteVisualPrefab;
                GameObject emergencyDropPrefab = authoring.VehicleEmergencyDropVisualPrefab;
                bool requiresRunwayAirdropVisuals = authoring.IsAirUnit &&
                                                   authoring.ProductionTransportUsesRunwayLanding &&
                                                   (authoring.SoldierTransportCapacity > 0 || authoring.VehicleTransportCapacity > 0);
                if (requiresRunwayAirdropVisuals)
                {
                    RequireAssignedPrefabReference(
                        parachutePrefab,
                        authoring,
                        nameof(authoring.SoldierParachuteVisualPrefab));
                    RequireAssignedPrefabReference(
                        emergencyDropPrefab,
                        authoring,
                        nameof(authoring.VehicleEmergencyDropVisualPrefab));
                }

                if (parachutePrefab == null && emergencyDropPrefab == null)
                    return;

                RequireValidPrefabReference(
                    parachutePrefab,
                    authoring,
                    nameof(authoring.SoldierParachuteVisualPrefab));
                RequireValidPrefabReference(
                    emergencyDropPrefab,
                    authoring,
                    nameof(authoring.VehicleEmergencyDropVisualPrefab));
                if (parachutePrefab != null)
                    DependsOn(parachutePrefab);
                if (emergencyDropPrefab != null)
                    DependsOn(emergencyDropPrefab);

                AddComponent(entity, new UnitTransportAirdropVisualPrefabs
                {
                    SoldierParachuteVisualPrefab = parachutePrefab != null
                        ? GetEntity(parachutePrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable)
                        : Entity.Null,
                    VehicleEmergencyDropVisualPrefab = emergencyDropPrefab != null
                        ? GetEntity(emergencyDropPrefab, TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable)
                        : Entity.Null
                });
            }

            private static void RequireAssignedPrefabReference(GameObject prefab, UnitGridAuthoring authoring, string fieldName)
            {
                if (prefab != null)
                    return;

                string configName = authoring.config != null ? authoring.config.name : authoring.gameObject.name;
                throw new InvalidOperationException(
                    $"{nameof(UnitGridAuthoring)} requires {fieldName} on '{configName}' for runway transport airdrops.");
            }

            private static void RequireValidPrefabReference(GameObject prefab, UnitGridAuthoring authoring, string fieldName)
            {
                if (prefab == null)
                    return;

                try
                {
                    _ = prefab.scene;
                }
                catch (MissingReferenceException exception)
                {
                    string configName = authoring.config != null ? authoring.config.name : authoring.gameObject.name;
                    throw new InvalidOperationException(
                        $"{nameof(UnitGridAuthoring)} requires a valid prefab reference for {fieldName} on '{configName}', " +
                        "but Unity loaded a stale or destroyed object reference during baking. Reassign or reimport the prefab reference in the config asset.",
                        exception);
                }
            }

            private void AddTransportPlaneDoorMetadata(UnitGridAuthoring authoring, Entity entity)
            {
                if (authoring.VehicleTransportCapacity <= 0)
                    return;

                Transform door = FindTransportPlaneDoorTransform(authoring.transform);
                if (door == null)
                    return;

                Vector3 doorLocalPosition = door.localPosition;
                doorLocalPosition.x = 0f;
                Vector3 openEuler = door.localEulerAngles;
                Vector3 closedEuler = openEuler;
                closedEuler.x = 0f;
                Quaternion closedLocalRotation = Quaternion.Euler(closedEuler);

                AddComponent(entity, new UnitTransportPlaneDoorReference
                {
                    DoorEntity = GetEntity(door.gameObject, TransformUsageFlags.Dynamic),
                    ClosedLocalRotation = ToMathQuaternion(closedLocalRotation),
                    OpenLocalRotation = ToMathQuaternion(door.localRotation),
                    OpenSeconds = 1.1f,
                    CloseSeconds = 0.9f,
                    DoorLocalPosition = doorLocalPosition,
                    InteriorLocalPosition = doorLocalPosition + new Vector3(0f, 1.45f, 9.5f),
                    ApproachLocalPosition = doorLocalPosition + new Vector3(0f, 0f, -6f),
                    RolloutLocalPosition = doorLocalPosition + new Vector3(0f, 0f, -6f)
                });
                AddComponent(entity, new UnitTransportPlaneDoorState
                {
                    Open01 = 0f,
                    TargetOpen = 0
                });
            }

            private static Transform FindTransportPlaneDoorTransform(Transform root)
            {
                return FindDescendantByName(root, "Door_X") ??
                       FindDescendantByName(root, "SM_Veh_TransportPlane_Door_High_01") ??
                       FindDescendantByName(root, "SM_Veh_TransportPlane_Door_Low_01") ??
                       FindDescendantByName(root, "Door_High") ??
                       FindDescendantByName(root, "Door_Low");
            }

            private static quaternion ToMathQuaternion(Quaternion rotation)
            {
                return new quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }

            private static bool TryGetBladeAxis(string name, out byte axis)
            {
                axis = 0;
                if (string.IsNullOrEmpty(name) || !name.Contains("Blade", System.StringComparison.Ordinal))
                    return false;
                if (name.EndsWith("_X", System.StringComparison.Ordinal))
                {
                    axis = 0;
                    return true;
                }
                if (name.EndsWith("_Y", System.StringComparison.Ordinal))
                {
                    axis = 1;
                    return true;
                }
                if (name.EndsWith("_Z", System.StringComparison.Ordinal))
                {
                    axis = 2;
                    return true;
                }

                return false;
            }

            private static Transform FindDescendantByName(Transform root, string name)
            {
                if (root == null || string.IsNullOrEmpty(name))
                    return null;

                foreach (Transform child in root)
                {
                    if (child.name == name)
                        return child;

                    Transform nested = FindDescendantByName(child, name);
                    if (nested != null)
                        return nested;
                }

                return null;
            }

            private static bool TryGetModelLocalBounds(UnitGridAuthoring authoring, out Bounds combinedBounds)
            {
                combinedBounds = default;

                Transform modelRoot = ResolveModelRoot(authoring);
                if (modelRoot == null)
                    return false;

                return TryGetCombinedLocalBounds(modelRoot, authoring.transform.worldToLocalMatrix, out combinedBounds);
            }

            private static bool TryGetCombinedLocalBounds(Transform modelRoot, Matrix4x4 worldToLocal, out Bounds combinedBounds)
            {
                combinedBounds = default;
                if (modelRoot == null)
                    return false;

                Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
                bool hasBounds = false;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                        continue;

                    Bounds localBounds = TransformBounds(worldToLocal * renderer.localToWorldMatrix, renderer.localBounds);
                    if (!hasBounds)
                    {
                        combinedBounds = localBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(localBounds);
                    }
                }

                return hasBounds;
            }

            private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
            {
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;

                Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                            Vector3 transformed = matrix.MultiplyPoint3x4(corner);
                            min = Vector3.Min(min, transformed);
                            max = Vector3.Max(max, transformed);
                        }
                    }
                }

                Bounds transformedBounds = new();
                transformedBounds.SetMinMax(min, max);
                return transformedBounds;
            }
        }
    }
}
