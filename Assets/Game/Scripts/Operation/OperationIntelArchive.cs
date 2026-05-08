using System;

public static class OperationIntelArchive
{
    public static OperationIntelEvidenceData Latest(OperationSaveData state, string districtId = null)
    {
        if (state?.intelEvidence == null)
            return null;

        for (int i = state.intelEvidence.Length - 1; i >= 0; i--)
        {
            OperationIntelEvidenceData evidence = state.intelEvidence[i];
            if (MatchesDistrict(evidence, districtId))
                return evidence;
        }

        return null;
    }

    public static OperationIntelEvidenceData At(OperationSaveData state, int newestFirstIndex, string districtId = null)
    {
        if (state?.intelEvidence == null || newestFirstIndex < 0)
            return null;

        int found = 0;
        for (int i = state.intelEvidence.Length - 1; i >= 0; i--)
        {
            OperationIntelEvidenceData evidence = state.intelEvidence[i];
            if (!MatchesDistrict(evidence, districtId))
                continue;

            if (found == newestFirstIndex)
                return evidence;

            found++;
        }

        return null;
    }

    public static int Count(OperationSaveData state, string districtId = null)
    {
        if (state?.intelEvidence == null)
            return 0;

        int count = 0;
        foreach (OperationIntelEvidenceData evidence in state.intelEvidence)
        {
            if (MatchesDistrict(evidence, districtId))
                count++;
        }

        return count;
    }

    public static int CountUnread(OperationSaveData state, string districtId = null)
    {
        if (state?.intelEvidence == null)
            return 0;

        int count = 0;
        foreach (OperationIntelEvidenceData evidence in state.intelEvidence)
        {
            if (MatchesDistrict(evidence, districtId) && evidence.unread)
                count++;
        }

        return count;
    }

    public static bool MarkRead(OperationSaveData state, string evidenceId)
    {
        if (state?.intelEvidence == null || string.IsNullOrWhiteSpace(evidenceId))
            return false;

        foreach (OperationIntelEvidenceData evidence in state.intelEvidence)
        {
            if (evidence == null || !string.Equals(evidence.evidenceId, evidenceId, StringComparison.Ordinal))
                continue;

            if (!evidence.unread)
                return false;

            evidence.unread = false;
            return true;
        }

        return false;
    }

    private static bool MatchesDistrict(OperationIntelEvidenceData evidence, string districtId)
    {
        if (evidence == null)
            return false;

        return string.IsNullOrWhiteSpace(districtId)
            || string.Equals(evidence.districtId, districtId, StringComparison.Ordinal);
    }
}
