using System;
using System.IO;
using System.Text;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks that the City Crossroads walkable surface agrees with the corrected authored geometry after
/// E0.1, and that the height transitions units can actually attempt stay inside the traversal budgets.
///
/// <para>Two distinct properties are measured. Agreement compares the baked surface height against the
/// authored ground top per cell — this is what regressed when the shelves were lowered. Traversal only
/// considers steps between cells that are <em>both</em> traversable by the unit class, because a cliff
/// between walkable ground and a blocked cell is terrain, not a navigation fault.</para>
/// </summary>
public static class CityCrossroadsSurfaceAgreementProbe
{
    private const string SurfaceAssetPath = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset";
    private const string EvidenceDirectory = "/private/tmp/warline-e01-cc-shelf";

    private const float AgreementTolerance = 0.50f;
    private const float InfantryStepBudget = 1.00f;
    private const float HeavyVehicleStepBudget = 0.60f;

    private static readonly (string Name, int X, int Z)[] Probes =
    {
        ("civic-shelf-centre", 1033, 509),
        ("civic-statue", 1027, 523),
        ("civic-hall", 1031, 540),
        ("civic-plinth", 1043, 488),
        ("northern-shelf-centre", 986, 698),
        ("northern-statue", 1000, 704),
        ("northern-base-spawn", 1020, 750),
        ("east-shelf-centre", 1072, 620),
        ("boulevard-mid", 1020, 600),
        ("southern-spawn", 1100, 400)
    };

    private static readonly (int MinX, int MaxX, int MinZ, int MaxZ)[] Windows =
    {
        (1015, 1055, 480, 545),
        (960, 1012, 665, 715),
        (1055, 1090, 595, 645)
    };

    public static void RunFocusedValidation()
    {
        var asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfaceAssetPath);
        if (asset == null)
            throw new InvalidOperationException($"Missing surface asset at {SurfaceAssetPath}.");

        var dimensions = new int2(asset.Dimensions.x, asset.Dimensions.y);

        // Opening the presentation scene sweeps unused assets, so sample geometry before reloading the asset.
        float[] authoredTops = Game.Editor.CityCrossroadsSurfaceRebake.BuildAuthoredGroundTops(dimensions);

        asset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(SurfaceAssetPath);
        if (!asset.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> blob))
            throw new InvalidOperationException("Failed to build the runtime surface blob.");

        var report = new StringBuilder();
        report.AppendLine("# City Crossroads surface / navigation agreement");
        report.AppendLine();
        report.AppendLine($"- Surface asset: `{SurfaceAssetPath}`");
        report.AppendLine($"- Dimensions: {dimensions.x} x {dimensions.y}, cell {asset.CellSize} m");
        report.AppendLine();
        report.AppendLine("| probe | cell | surface height | authored ground top | delta | walkable |");
        report.AppendLine("|---|---|---:|---:|---:|---|");

        try
        {
            ref MapSurfaceBlob surface = ref blob.Value;

            for (int i = 0; i < Probes.Length; i++)
            {
                (string name, int x, int z) = Probes[i];
                string height = "none";
                string walkable = "n/a";
                string authored = "n/a";
                string delta = "n/a";
                if (MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(x, z), out MapSurfaceSample sample))
                {
                    height = sample.Height.ToString("0.000");
                    walkable = sample.MovementMask.ToString();
                    float top = authoredTops[x + (z * dimensions.x)];
                    if (!float.IsNaN(top))
                    {
                        authored = top.ToString("0.000");
                        delta = (sample.Height - top).ToString("0.000");
                    }
                }

                report.AppendLine($"| {name} | ({x},{z}) | {height} | {authored} | {delta} | {walkable} |");
            }

            // Overhang is the E0.1 failure mode: baked surface standing above the authored ground, so units
            // float. Underhang is the opposite -- authored mounds the bake never described - which this
            // correction cannot create, because it only ever lowered geometry and lowered surface.
            float worstOverhang = 0f;
            string worstOverhangDetail = "none";
            float worstUnderhang = 0f;
            string worstUnderhangDetail = "none";
            float worstInfantryStep = 0f;
            string worstInfantryDetail = "none";
            float worstVehicleStep = 0f;
            string worstVehicleDetail = "none";
            int sampled = 0;

            for (int w = 0; w < Windows.Length; w++)
            {
                (int minX, int maxX, int minZ, int maxZ) = Windows[w];
                for (int x = minX; x < maxX; x++)
                {
                    for (int z = minZ; z < maxZ; z++)
                    {
                        if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(x, z), out MapSurfaceSample a))
                            continue;
                        sampled++;

                        float top = authoredTops[x + (z * dimensions.x)];
                        if (!float.IsNaN(top))
                        {
                            float signed = a.Height - top;
                            if (signed > worstOverhang)
                            {
                                worstOverhang = signed;
                                worstOverhangDetail = $"({x},{z}) surface {a.Height:0.00} above ground {top:0.00}";
                            }
                            else if (-signed > worstUnderhang)
                            {
                                worstUnderhang = -signed;
                                worstUnderhangDetail = $"({x},{z}) ground {top:0.00} above surface {a.Height:0.00}";
                            }
                        }

                        TrackStep(ref surface, a, x + 1, z, ref worstInfantryStep, ref worstInfantryDetail,
                            MapSurfaceMovementMask.Infantry);
                        TrackStep(ref surface, a, x, z + 1, ref worstInfantryStep, ref worstInfantryDetail,
                            MapSurfaceMovementMask.Infantry);
                        TrackStep(ref surface, a, x + 1, z, ref worstVehicleStep, ref worstVehicleDetail,
                            MapSurfaceMovementMask.TrackedVehicle);
                        TrackStep(ref surface, a, x, z + 1, ref worstVehicleStep, ref worstVehicleDetail,
                            MapSurfaceMovementMask.TrackedVehicle);
                    }
                }
            }

            report.AppendLine();
            report.AppendLine($"- Cells sampled across corrected windows: {sampled}");
            report.AppendLine($"- Worst overhang (surface above ground, units float): {worstOverhang:0.000} m ({worstOverhangDetail})");
            report.AppendLine($"- Worst underhang (authored mound not in bake): {worstUnderhang:0.000} m ({worstUnderhangDetail})");
            report.AppendLine($"- Agreement tolerance: {AgreementTolerance:0.00} m");
            report.AppendLine($"- Worst infantry-traversable step: {worstInfantryStep:0.000} m ({worstInfantryDetail})");
            report.AppendLine($"- Worst tracked-vehicle-traversable step: {worstVehicleStep:0.000} m ({worstVehicleDetail})");
            report.AppendLine($"- Infantry step budget: {InfantryStepBudget:0.00} m");
            report.AppendLine($"- Heavy vehicle step budget: {HeavyVehicleStepBudget:0.00} m");

            Directory.CreateDirectory(EvidenceDirectory);
            File.WriteAllText(Path.Combine(EvidenceDirectory, "cc-surface-agreement.md"), report.ToString());

            // The gate gates on overhang and on traversal steps. Underhang is reported, not gated: it is a
            // pre-existing bake gap unrelated to E0.1, and the baseline run records its value before the fix.
            bool agreementOk = worstOverhang <= AgreementTolerance;
            bool infantryOk = worstInfantryStep <= InfantryStepBudget;
            bool vehicleOk = worstVehicleStep <= HeavyVehicleStepBudget;
            bool passed = agreementOk && infantryOk && vehicleOk;

            string marker =
                $"[CityCrossroadsSurfaceAgreement] result={(passed ? "Passed" : "Failed")} " +
                $"cellsSampled={sampled} worstOverhang={worstOverhang:0.000} agreementOk={agreementOk} " +
                $"worstUnderhang={worstUnderhang:0.000} " +
                $"worstInfantryStep={worstInfantryStep:0.000} infantryOk={infantryOk} " +
                $"worstVehicleStep={worstVehicleStep:0.000} heavyVehicleOk={vehicleOk}";

            if (!passed)
            {
                Debug.LogError(marker);
                throw new InvalidOperationException(marker);
            }

            Debug.Log(marker);
        }
        finally
        {
            if (blob.IsCreated)
                blob.Dispose();
        }
    }

    private static void TrackStep(
        ref MapSurfaceBlob surface,
        MapSurfaceSample from,
        int x,
        int z,
        ref float worstStep,
        ref string worstDetail,
        MapSurfaceMovementMask required)
    {
        if ((from.MovementMask & required) == 0)
            return;
        if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface, new int2(x, z), out MapSurfaceSample to))
            return;
        if ((to.MovementMask & required) == 0)
            return;

        float step = Mathf.Abs(to.Height - from.Height);
        if (step <= worstStep)
            return;
        worstStep = step;
        worstDetail = $"({from.Cell.x},{from.Cell.y}) {from.Height:0.00} -> ({x},{z}) {to.Height:0.00}";
    }
}
