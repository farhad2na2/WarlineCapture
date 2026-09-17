using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public static class ThreatWarningDisplayText
    {
        public static string BuildCompact(in ThreatWarningRecord record)
        {
            string route=GameText.Get(record.ElementIndex==0?"mission.m03.warning.compact.first":"mission.m03.warning.compact.main");
            string timing=record.ContactWindowOpen!=0?GameText.Get("mission.m03.warning.window"):
                GameText.Format("mission.m03.warning.compact.eta","Arrival: {0}s",record.EtaSeconds);
            string confidence=record.KnownVehicleCount<0?GameText.Get("mission.m03.warning.compact.unknown"):
                GameText.Format("mission.m03.warning.compact.count","{0} vehicles",record.KnownVehicleCount);
            return route+"\n"+timing+" · "+confidence;
        }

        public static string Build(in ThreatWarningRecord record)
        {
            string sourceKey = record.Source switch
            {
                ThreatWarningSourceKind.GroundSensor => "sensor",
                ThreatWarningSourceKind.VisualContact => "visual",
                ThreatWarningSourceKind.RadarPing => "ping",
                _ => "scout"
            };
            string source = GameText.Get("mission.m03.warning." + sourceKey, sourceKey);
            string route = GameText.Get(record.ElementIndex == 0 ? "mission.m03.warning.vanguard" : "mission.m03.warning.main",
                record.ElementIndex == 0 ? "Vanguard · western road" : "Main convoy · western road");
            string confidence = record.Stale != 0 ? GameText.Get("mission.m03.warning.stale", "Last known contact") :
                record.KnownVehicleCount < 0 ? GameText.Get("mission.m03.warning.unknown", "Composition unconfirmed") :
                GameText.Format("mission.m03.warning.vehicles", "{0} vehicle(s) observed", record.KnownVehicleCount);
            string eta = record.ContactWindowOpen != 0 ? GameText.Get("mission.m03.warning.window", "Contact window open") :
                GameText.Format("mission.m03.warning.eta", "Contact estimate: {0}s", record.EtaSeconds);
            return $"{source} · {route}\n{confidence} · {eta}";
        }
    }
}
