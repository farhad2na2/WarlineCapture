namespace Game.UI.Contracts
{
    public readonly struct UiAssistantHighlightModel
    {
        public readonly uint Version;
        public readonly bool Active;
        public readonly int RequestId;
        public readonly int RecommendationId;
        public readonly byte RecommendationKind;
        public readonly byte TargetKind;
        public readonly float WorldX;
        public readonly float WorldY;
        public readonly float WorldZ;
        public readonly float Strength;

        public UiAssistantHighlightModel(
            uint version,
            bool active,
            int requestId,
            int recommendationId,
            byte recommendationKind,
            byte targetKind,
            float worldX,
            float worldY,
            float worldZ,
            float strength)
        {
            Version = version;
            Active = active;
            RequestId = requestId;
            RecommendationId = recommendationId;
            RecommendationKind = recommendationKind;
            TargetKind = targetKind;
            WorldX = worldX;
            WorldY = worldY;
            WorldZ = worldZ;
            Strength = strength;
        }

        public static UiAssistantHighlightModel Empty =>
            new(0, false, 0, 0, 0, 0, 0f, 0f, 0f, 0f);
    }
}
