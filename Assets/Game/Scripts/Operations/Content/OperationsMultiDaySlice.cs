using System;
using Game.Operations.Contracts;
using Game.Operations.Loop;

namespace Game.Operations.Content
{
    /// <summary>
    /// Scaffolding for one meaningful multi-day Old Quarter loop across O001–O003.
    /// Ends the day between missions when AP is spent.
    /// </summary>
    public sealed class OperationsMultiDaySlice
    {
        readonly OperationsLoopSession _loop;
        int _commandSerial = 0xB400;

        public OperationsMultiDaySlice(OperationsLoopSession loop)
        {
            _loop = loop ?? throw new ArgumentNullException(nameof(loop));
        }

        public OperationsLoopSession Loop => _loop;
        public int Day => _loop.Day;
        public int ActionPoints => _loop.ActionPoints;

        public bool TryDeployAndWin(string missionId, out string failure)
        {
            failure = string.Empty;
            if (!_loop.TryOffer(missionId, out OperationsOfferSaveData offer))
            {
                failure = "offer";
                return false;
            }

            int number = DistrictNumber(offer.districtId);
            if (!_loop.OpenDistrict(number).Accepted || !_loop.OpenBriefing(offer.offerId).Accepted)
            {
                failure = "briefing";
                return false;
            }

            string deployId = NextId();
            if (!_loop.BeginDeploy(deployId).Accepted || !_loop.CompleteAttempt(deployId).Accepted)
            {
                failure = "deploy";
                return false;
            }

            if (!_loop.BeginLaunch().Accepted || !_loop.CompleteLaunch().Accepted)
            {
                failure = "launch";
                return false;
            }

            if (!_loop.BeginActive(true, true, true, _loop.ContentHash).Accepted ||
                !_loop.CompleteActive().Accepted)
            {
                failure = "active";
                return false;
            }

            if (!OperationsAriaInputSkills.TryPlayVisibleControlWin(_loop, missionId))
            {
                failure = "mission:" + _loop.MissionOutcome;
                return false;
            }

            if (!_loop.BeginResult().Accepted || !_loop.CompleteResult().Accepted)
            {
                failure = "result";
                return false;
            }

            string settleId = NextId();
            if (!_loop.BeginSettlement(settleId).Accepted || !_loop.CompleteSettlement(settleId).Accepted)
            {
                failure = "settlement";
                return false;
            }

            if (!_loop.BeginReturn().Accepted || !_loop.CompleteReturn().Accepted)
            {
                failure = "return";
                return false;
            }

            return _loop.MissionVictory(missionId);
        }

        public bool TryEndDay(out string failure)
        {
            failure = string.Empty;
            if (_loop.Phase != OperationsLoopPhase.Dashboard)
            {
                failure = "phase";
                return false;
            }

            if (_loop.OpenReport().Accepted)
                _loop.ContinueReport();

            string commandId = NextId();
            OperationsCommandResult end = _loop.RequestEndDay(commandId);
            if (!end.Accepted)
            {
                failure = end.ReasonCode.ToString();
                return false;
            }

            return true;
        }

        public bool TryRunThreeMissionScaffold(out string failure)
        {
            if (!TryDeployAndWin("operation.o001", out failure))
                return false;
            if (!TryEndDay(out failure))
                return false;
            if (!TryDeployAndWin("operation.o002", out failure))
                return false;
            if (!TryEndDay(out failure))
                return false;
            if (!TryDeployAndWin("operation.o003", out failure))
                return false;
            return true;
        }

        string NextId() => "cmd.operations." + (_commandSerial++).ToString("x8");

        static int DistrictNumber(string districtId)
        {
            if (districtId == null || districtId.Length < 2)
                return 0;
            char tens = districtId[districtId.Length - 2];
            char ones = districtId[districtId.Length - 1];
            if (tens < '0' || tens > '9' || ones < '0' || ones > '9')
                return 0;
            return ((tens - '0') * 10) + (ones - '0');
        }
    }
}
