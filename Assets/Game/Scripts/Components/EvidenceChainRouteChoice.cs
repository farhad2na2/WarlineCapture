namespace Game.Components
{
    public enum EvidenceChainExtractionRoute : byte
    {
        Air = 0,
        Armored = 1
    }

    // The briefing choice is copied into the mission attempt on spawn. It is not
    // read again during the match, so changing menus cannot change an active run.
    public static class EvidenceChainRouteChoice
    {
        public static EvidenceChainExtractionRoute Selected { get; set; } = EvidenceChainExtractionRoute.Air;
    }
}
