using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class FreshContractorSelectionPanelEditorValidation
    {
        private const string Marker = "[FreshContractorSelectionPanelEditorValidation]";
        private const string HudPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
        private const string BarracksConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset";
        private const string ContractorConfigPath = "Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Tent_Contractor_Config.asset";
        private const string EvidenceRelativePath = "Build/EditorEvidence/FreshContractorSelectionPanelTransition.png";

        [MenuItem("Tools/Validation/Fresh Contractor Selection Panel Transition")]
        public static void Run()
        {
            World world = null;
            GameObject hudRoot = null;
            GameObject cameraObject = null;
            try
            {
                GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
                BuildingDefinitionAuthoringConfig barracks = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(BarracksConfigPath);
                BuildingDefinitionAuthoringConfig contractor = AssetDatabase.LoadAssetAtPath<BuildingDefinitionAuthoringConfig>(ContractorConfigPath);
                Sprite barracksPortrait = ResolvePortrait(barracks);
                Sprite contractorPortrait = ResolvePortrait(contractor);
                if (hudPrefab == null || barracksPortrait == null || contractorPortrait == null)
                    throw new InvalidOperationException("HUD prefab and both canonical building portraits are required.");

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                hudRoot = UnityEngine.Object.Instantiate(hudPrefab);
                hudRoot.name = "Fresh Contractor Panel Validation HUD";
                hudRoot.SetActive(true);
                MatchHudSelectionPanelView panel = hudRoot.GetComponentInChildren<MatchHudSelectionPanelView>(true);
                if (panel == null)
                    throw new InvalidOperationException("Match HUD prefab has no selection panel view.");

                world = new World("FreshContractorSelectionPanelEditorValidation");
                EntityManager entityManager = world.EntityManager;
                var feedback = new SelectionHudFeedbackUiSystemHelper();
                feedback.BindMatchHudSelectionPanel(panel);
                var context = new SelectionHudFeedbackUiSystemHelper.Context(
                    new SelectionUiReadModelLookup(),
                    (out EntityManager em) =>
                    {
                        em = entityManager;
                        return true;
                    });
                Entity staleBarracks = CreateStaleFocusedBarracks(entityManager);
                var selectionState = new SelectionStateCompositionSystemHelper();
                selectionState.SetFocusedUnit(staleBarracks);
                var focusedLifecycle = new FocusedUnitLifecycleCompositionSystemHelper();

                string selectedLabel = "Barracks (100,100)";
                Sprite selectedPortrait = barracksPortrait;
                ApplyBuildingModel(feedback, context, selectionState, focusedLifecycle, () => selectedLabel, () => selectedPortrait);
                AssertPanel(panel, "Barracks", "(100,100)", barracksPortrait);

                selectedLabel = "Contractor Tent (113,100)";
                selectedPortrait = contractorPortrait;
                ApplyBuildingModel(feedback, context, selectionState, focusedLifecycle, () => selectedLabel, () => selectedPortrait);
                AssertPanel(panel, "Contractor Tent", "(113,100)", contractorPortrait);

                string evidencePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", EvidenceRelativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(evidencePath) ?? throw new InvalidOperationException("Evidence directory is invalid."));
                RenderHudEvidence(hudRoot, evidencePath, out cameraObject);
                if (!File.Exists(evidencePath) || new FileInfo(evidencePath).Length <= 0)
                    throw new InvalidOperationException("Fresh Contractor panel evidence was not written.");

                Debug.Log($"{Marker} result=Passed title=Contractor Tent origin=(113,100) portrait={contractorPortrait.name} subtitle=Base Structure order=Structure selected health=- evidence={evidencePath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Marker} result=Failed\n{exception}");
                throw;
            }
            finally
            {
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                if (hudRoot != null)
                    UnityEngine.Object.DestroyImmediate(hudRoot);
                world?.Dispose();
            }
        }

        private static void ApplyBuildingModel(
            SelectionHudFeedbackUiSystemHelper feedback,
            SelectionHudFeedbackUiSystemHelper.Context context,
            SelectionStateCompositionSystemHelper selectionState,
            FocusedUnitLifecycleCompositionSystemHelper focusedLifecycle,
            Func<string> selectedLabel,
            Func<Sprite> selectedPortrait)
        {
            feedback.UpdateMatchHudSelectionPanel(
                context: context,
                selectionStateSystem: selectionState,
                focusedUnitLifecycleSystem: focusedLifecycle,
                focusedUnitUiReadModelSystem: null,
                transportPassengerPanelItems: new List<MatchHudSelectionPanelPassengerItemModel>(),
                ensureEntityQueries: null,
                tryGetAttackModeOrderSnapshot: null,
                resolveSelectionCardPortraitSprite: null,
                resolveSelectedBuildingPortraitSprite: selectedPortrait,
                resolveActiveSquadTrayPortraitSprite: null,
                hasSelectedBuilding: () => true,
                selectedBuildingLabel: selectedLabel,
                tryGetSelectedBuildingResourceStorage: null,
                tryGetSelectedBuildingResourceStorageSnapshot: null,
                tryGetSelectedMaterialFabricationReadModel: null,
                isBoardCommandAvailable: null,
                hasSelectedBoardAction: null);
        }

        private static Entity CreateStaleFocusedBarracks(EntityManager entityManager)
        {
            Entity entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new Faction { Id = 0 });
            entityManager.AddComponentData(entity, new UnitGrid { Cell = new int2(100, 100) });
            entityManager.AddComponentData(entity, new UnitHealth { Current = 1200, Max = 1200 });
            entityManager.AddComponentData(entity, new UnitDisplayInfo
            {
                Name = new FixedString64Bytes("Barracks"),
                Description = new FixedString128Bytes("Stale ECS selection owner")
            });
            entityManager.AddComponentData(entity, new UnitMove
            {
                Speed = 5f,
                WalkSpeed = 5f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.05f
            });
            entityManager.AddComponentData(entity, LocalTransform.FromPosition(new float3(100f, 0f, 100f)));
            entityManager.AddComponent<SelectedUnitTag>(entity);
            return entity;
        }

        private static void AssertPanel(
            MatchHudSelectionPanelView panel,
            string expectedTitle,
            string expectedOrigin,
            Sprite expectedPortrait)
        {
            GameObject panelRoot = ReadField<GameObject>(panel, "selectedSquadPanel");
            Image portrait = ReadField<Image>(panel, "selectedPortraitImage");
            TMP_Text title = ReadField<TMP_Text>(panel, "titleText");
            TMP_Text subtitle = ReadField<TMP_Text>(panel, "subtitleText");
            TMP_Text order = ReadField<TMP_Text>(panel, "currentOrderText");
            TMP_Text health = ReadField<TMP_Text>(panel, "healthText");
            if (panelRoot == null || !panelRoot.activeInHierarchy ||
                portrait == null || portrait.sprite != expectedPortrait ||
                title == null || !title.text.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase) ||
                !title.text.Contains(expectedOrigin, StringComparison.Ordinal) ||
                subtitle == null || !subtitle.text.Contains("Base Structure", StringComparison.OrdinalIgnoreCase) ||
                order == null || !order.text.Contains("Structure selected", StringComparison.OrdinalIgnoreCase) ||
                health == null || !string.Equals(health.text.Trim(), "-", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Panel mismatch expected={expectedTitle}{expectedOrigin} actualTitle={title?.text ?? "null"} " +
                    $"portrait={portrait?.sprite?.name ?? "null"} subtitle={subtitle?.text ?? "null"} " +
                    $"order={order?.text ?? "null"} health={health?.text ?? "null"}.");
            }
        }

        private static void RenderHudEvidence(GameObject hudRoot, string evidencePath, out GameObject cameraObject)
        {
            cameraObject = new GameObject("Fresh Contractor Panel Evidence Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.075f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            Canvas[] canvases = hudRoot.GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
                throw new InvalidOperationException("Match HUD prefab has no canvas.");
            MatchHudSelectionPanelView panel = hudRoot.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            GameObject panelRoot = ReadField<GameObject>(panel, "selectedSquadPanel");
            Canvas panelCanvas = canvases[0];
            if (panelRoot == null || panelCanvas == null)
                throw new InvalidOperationException("Selection panel canvas is unavailable for evidence.");

            panelRoot.transform.SetParent(panelCanvas.transform, false);
            for (int childIndex = 0; childIndex < panelCanvas.transform.childCount; childIndex++)
            {
                GameObject child = panelCanvas.transform.GetChild(childIndex).gameObject;
                if (child != panelRoot)
                    child.SetActive(false);
            }
            panelRoot.SetActive(true);
            if (panelRoot.transform is RectTransform panelRect)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.localRotation = Quaternion.identity;
                panelRect.localScale = Vector3.one;
                panelRect.sizeDelta = new Vector2(Mathf.Max(900f, panelRect.sizeDelta.x), panelRect.sizeDelta.y);
            }
            TMP_Text evidenceTitle = ReadField<TMP_Text>(panel, "titleText");
            if (evidenceTitle != null)
            {
                evidenceTitle.enableAutoSizing = true;
                evidenceTitle.fontSizeMin = 18f;
                evidenceTitle.fontSizeMax = Mathf.Max(18f, evidenceTitle.fontSize);
                evidenceTitle.overflowMode = TextOverflowModes.Overflow;
                RectTransform titleRect = evidenceTitle.rectTransform;
                titleRect.sizeDelta = new Vector2(Mathf.Max(850f, titleRect.sizeDelta.x), titleRect.sizeDelta.y);
            }
            for (int index = 0; index < canvases.Length; index++)
            {
                if (canvases[index].transform.parent != null && canvases[index].transform.parent.GetComponentInParent<Canvas>() != null)
                    continue;
                canvases[index].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[index].worldCamera = camera;
                canvases[index].planeDistance = 1f;
            }

            Canvas.ForceUpdateCanvases();
            RenderTexture target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(evidencePath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (image != null)
                    UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static Sprite ResolvePortrait(BuildingDefinitionAuthoringConfig config)
        {
            return config != null
                ? config.PortraitActionSprite != null ? config.PortraitActionSprite : config.PortraitCardSprite
                : null;
        }

        private static T ReadField<T>(MatchHudSelectionPanelView panel, string name) where T : class
        {
            return typeof(MatchHudSelectionPanelView)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(panel) as T;
        }
    }
}
