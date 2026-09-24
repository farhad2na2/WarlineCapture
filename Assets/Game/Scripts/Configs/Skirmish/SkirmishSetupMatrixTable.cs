using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Skirmish.Contracts;
using UnityEngine;

namespace Game.Configs
{
    public sealed class SkirmishSetupMatrixRow
    {
        public string CatalogId;
        public SkirmishSizeId SizeId;
        public SkirmishArmyProfileId ArmyProfile;
        public SkirmishStartPackageId StartProfile;
        public SkirmishObjectiveKind ObjectiveKind;
        public int Readiness;
        public int DeadlineSeconds;
        public int PlayerRifle;
        public int PlayerGunner;
        public int PlayerRocketeer;
        public int PlayerCar;
        public int PlayerApc;
        public int PlayerTank;
        public int PlayerAa;
        public int PlayerTransportHeli;
        public int PlayerInfantry;
        public int PlayerGround;
        public int PlayerAir;
        public int PlayerCombat;
        public int PlayerSupply;
        public int PlayerLogisticsSupport;
        public int PlayerObjectiveTrucks;
        public int PlayerStartingStructures;
        public int PlayerDesignatedRifles;
        public int EnemyRifle;
        public int EnemyGunner;
        public int EnemyRocketeer;
        public int EnemyCar;
        public int EnemyApc;
        public int EnemyTank;
        public int EnemyAa;
        public int EnemyTransportHeli;
        public int EnemyInfantry;
        public int EnemyGround;
        public int EnemyAir;
        public int EnemyCombat;
        public int EnemySupply;
        public int EnemyLogisticsSupport;
        public int EnemyObjectiveTrucks;
        public int EnemyStartingStructures;
        public int EnemyDesignatedRifles;
        public int MaterialsEach;
        public int OilEach;
        public int UsableFuelEach;
        public int MaterialsCapacityEach;
        public int OilCapacityEach;
        public int FuelCapacityEach;
        public int InfantryQueuesEach;
        public int VehicleQueuesEach;
        public int LogisticsQueuesEach;
        public int RefineryModulesEach;
        public int InfantryCapEach;
        public int GroundCapEach;
        public int TacticalAirCapEach;
        public int SupplyCapEach;
        public int LogisticsSupportCapEach;
        public int DeliveryCarrierCapEach;
        public int ObjectiveSupportExtraPlayer;
        public int PlayerBuiltStructureCapEach;
        public int BarrierSegmentCapEach;
        public string LoadedVehicleFuel;
        public string Status;
    }

    public static class SkirmishSetupMatrixTable
    {
        public const string RelativeCsvPath = "Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv";
        public const string PackagedResourceName = "SkirmishExpansion/InitialSetupMatrix";

        public static bool TryLoad(string projectRoot, out List<SkirmishSetupMatrixRow> rows, out string error)
        {
            string path = Path.Combine(projectRoot, RelativeCsvPath);
            if (!File.Exists(path))
            {
                rows = new List<SkirmishSetupMatrixRow>();
                error = "INITIAL_SETUP_MATRIX.csv is missing.";
                return false;
            }

            return TryLoadFromText(File.ReadAllText(path), out rows, out error);
        }

        /// <summary>
        /// Packaged runtime path: the matrix ships as a TextAsset under Resources so a
        /// standalone player never reads the repository Design directory.
        /// </summary>
        public static bool TryLoadPackaged(out List<SkirmishSetupMatrixRow> rows, out string error)
        {
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(PackagedResourceName);
            if (asset == null)
            {
                rows = new List<SkirmishSetupMatrixRow>();
                error = "Packaged skirmish setup matrix is missing (Resources/" + PackagedResourceName + ").";
                return false;
            }

            return TryLoadFromText(asset.text, out rows, out error);
        }

        public static bool TryLoadFromText(string csvText, out List<SkirmishSetupMatrixRow> rows, out string error)
        {
            rows = new List<SkirmishSetupMatrixRow>();
            if (string.IsNullOrEmpty(csvText))
            {
                error = "INITIAL_SETUP_MATRIX.csv has no data rows.";
                return false;
            }

            string[] lines = csvText.Split('\n');
            if (lines.Length < 2)
            {
                error = "INITIAL_SETUP_MATRIX.csv has no data rows.";
                return false;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                string[] cols = SplitCsv(line);
                if (cols.Length < 61)
                {
                    error = $"Matrix row {i + 1} is incomplete ({cols.Length} columns).";
                    return false;
                }

                if (!TryParse(cols, out SkirmishSetupMatrixRow row, out error))
                {
                    error = $"Matrix row {i + 1}: {error}";
                    return false;
                }

                rows.Add(row);
            }

            error = null;
            return true;
        }

        public static bool TryFind(
            IReadOnlyList<SkirmishSetupMatrixRow> rows,
            string catalogId,
            SkirmishSizeId size,
            out SkirmishSetupMatrixRow row)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].CatalogId == catalogId && rows[i].SizeId == size)
                {
                    row = rows[i];
                    return true;
                }
            }

            row = null;
            return false;
        }

        private static bool TryParse(string[] cols, out SkirmishSetupMatrixRow row, out string error)
        {
            row = new SkirmishSetupMatrixRow();
            if (!new SkirmishCatalogId(cols[0]).IsValid)
            {
                error = "invalid catalog_id";
                return false;
            }

            if (!SkirmishIdParsing.TryParseSize(cols[1], out row.SizeId) ||
                !SkirmishIdParsing.TryParseArmy(cols[2], out row.ArmyProfile) ||
                !SkirmishIdParsing.TryParseStart(cols[3], out row.StartProfile) ||
                !SkirmishObjectiveIds.TryParse(cols[4], out row.ObjectiveKind))
            {
                error = "invalid size/army/start/objective identity";
                return false;
            }

            row.CatalogId = cols[0];
            row.Readiness = Int(cols[5]);
            row.DeadlineSeconds = Int(cols[6]);
            row.PlayerRifle = Int(cols[7]);
            row.PlayerGunner = Int(cols[8]);
            row.PlayerRocketeer = Int(cols[9]);
            row.PlayerCar = Int(cols[10]);
            row.PlayerApc = Int(cols[11]);
            row.PlayerTank = Int(cols[12]);
            row.PlayerAa = Int(cols[13]);
            row.PlayerTransportHeli = Int(cols[14]);
            row.PlayerInfantry = Int(cols[15]);
            row.PlayerGround = Int(cols[16]);
            row.PlayerAir = Int(cols[17]);
            row.PlayerCombat = Int(cols[18]);
            row.PlayerSupply = Int(cols[19]);
            row.PlayerLogisticsSupport = Int(cols[20]);
            row.PlayerObjectiveTrucks = Int(cols[21]);
            row.PlayerStartingStructures = Int(cols[22]);
            row.PlayerDesignatedRifles = Int(cols[23]);
            row.EnemyRifle = Int(cols[24]);
            row.EnemyGunner = Int(cols[25]);
            row.EnemyRocketeer = Int(cols[26]);
            row.EnemyCar = Int(cols[27]);
            row.EnemyApc = Int(cols[28]);
            row.EnemyTank = Int(cols[29]);
            row.EnemyAa = Int(cols[30]);
            row.EnemyTransportHeli = Int(cols[31]);
            row.EnemyInfantry = Int(cols[32]);
            row.EnemyGround = Int(cols[33]);
            row.EnemyAir = Int(cols[34]);
            row.EnemyCombat = Int(cols[35]);
            row.EnemySupply = Int(cols[36]);
            row.EnemyLogisticsSupport = Int(cols[37]);
            row.EnemyObjectiveTrucks = Int(cols[38]);
            row.EnemyStartingStructures = Int(cols[39]);
            row.EnemyDesignatedRifles = Int(cols[40]);
            row.MaterialsEach = Int(cols[41]);
            row.OilEach = Int(cols[42]);
            row.UsableFuelEach = Int(cols[43]);
            row.MaterialsCapacityEach = Int(cols[44]);
            row.OilCapacityEach = Int(cols[45]);
            row.FuelCapacityEach = Int(cols[46]);
            row.InfantryQueuesEach = Int(cols[47]);
            row.VehicleQueuesEach = Int(cols[48]);
            row.LogisticsQueuesEach = Int(cols[49]);
            row.RefineryModulesEach = Int(cols[50]);
            row.InfantryCapEach = Int(cols[51]);
            row.GroundCapEach = Int(cols[52]);
            row.TacticalAirCapEach = Int(cols[53]);
            row.SupplyCapEach = Int(cols[54]);
            row.LogisticsSupportCapEach = Int(cols[55]);
            row.DeliveryCarrierCapEach = Int(cols[56]);
            row.ObjectiveSupportExtraPlayer = Int(cols[57]);
            row.PlayerBuiltStructureCapEach = Int(cols[58]);
            row.BarrierSegmentCapEach = Int(cols[59]);
            row.LoadedVehicleFuel = cols[60];
            row.Status = cols.Length > 61 ? cols[61] : string.Empty;
            error = null;
            return true;
        }

        private static int Int(string value) =>
            int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        private static string[] SplitCsv(string line)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            bool quotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    quotes = !quotes;
                    continue;
                }

                if (c == ',' && !quotes)
                {
                    fields.Add(current.ToString());
                    current.Length = 0;
                    continue;
                }

                current.Append(c);
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}
