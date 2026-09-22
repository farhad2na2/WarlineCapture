using System;
using Game.Operations.Contracts;
using Game.Operations.Loop;
using Game.Operations.Strategic;

namespace Game.Operations.Content
{
    public readonly struct OperationsDayReportFrame
    {
        public OperationsDayReportFrame(
            int day,
            string titleKey,
            string continueKey,
            int actionPoints,
            int[] districtSecurity,
            string[] missionVictories,
            bool canEndDay)
        {
            Day = day;
            TitleKey = titleKey ?? string.Empty;
            ContinueKey = continueKey ?? string.Empty;
            ActionPoints = actionPoints;
            DistrictSecurity = districtSecurity ?? Array.Empty<int>();
            MissionVictories = missionVictories ?? Array.Empty<string>();
            CanEndDay = canEndDay;
        }

        public int Day { get; }
        public string TitleKey { get; }
        public string ContinueKey { get; }
        public int ActionPoints { get; }
        public int[] DistrictSecurity { get; }
        public string[] MissionVictories { get; }
        public bool CanEndDay { get; }
    }

    public static class OperationsDayReportProjection
    {
        public static bool TryRead(OperationsLoopSession loop, out OperationsDayReportFrame frame)
        {
            frame = default;
            if (loop == null)
                return false;
            if (loop.ReadShell().Top != OperationsShellNames.EndOfDayReport &&
                loop.Phase != OperationsLoopPhase.Dashboard)
                return false;

            var security = new int[OperationsIdentityRules.DistrictCount];
            for (int index = 0; index < security.Length; index++)
            {
                OperationsDistrictComponent district = loop.District(index + 1);
                security[index] = district.Security;
            }

            string[] victories =
            {
                loop.MissionVictory("operation.o001") ? "operation.o001" : string.Empty,
                loop.MissionVictory("operation.o002") ? "operation.o002" : string.Empty,
                loop.MissionVictory("operation.o003") ? "operation.o003" : string.Empty
            };
            int count = 0;
            for (int index = 0; index < victories.Length; index++)
            {
                if (victories[index].Length > 0)
                    count++;
            }

            var trimmed = new string[count];
            int write = 0;
            for (int index = 0; index < victories.Length; index++)
            {
                if (victories[index].Length == 0)
                    continue;
                trimmed[write++] = victories[index];
            }

            frame = new OperationsDayReportFrame(
                loop.Day,
                "operations.day_report.title",
                "operations.day_report.continue",
                loop.ActionPoints,
                security,
                trimmed,
                loop.Phase == OperationsLoopPhase.Dashboard && loop.ReadShell().History.Length == 1);
            return true;
        }
    }
}
