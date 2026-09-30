using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        // The chapter plates are evocative district overviews. The mission cards use
        // each operation's own preview, so the atlas never suggests one playable map.
        [SerializeField] private Texture[] districtAtlasScenes;
        [SerializeField] private RawImage districtAtlasBackdrop;
        [SerializeField] private RawImage[] atlasChapterArtwork;
        [SerializeField] private RawImage[] atlasMissionArtwork;
        [SerializeField] private Texture atlasFirstContactArtwork;
        [SerializeField] private Texture atlasEstablishBaseArtwork;
        [SerializeField] private Image[] atlasChapterProgress;
        [SerializeField] private Button atlasStoryArchiveButton;

        private void ApplyAtlasChapterProgress(uint completedMask, uint availableMask)
        {
            if (atlasChapterProgress == null) return;
            for (int i = 0; i < atlasChapterProgress.Length; i++)
            {
                Image mark = atlasChapterProgress[i];
                if (mark == null) continue;
                uint bit = 1u << i;
                mark.color = (completedMask & bit) != 0 ? new Color32(63, 213, 103, 255) :
                    (availableMask & bit) != 0 ? new Color32(250, 186, 19, 255) :
                    new Color32(93, 113, 118, 230);
            }
        }

        private void ApplyDistrictAtlas(int chapter)
        {
            int sceneIndex = Mathf.Clamp(chapter - 1, 0, 4);
            Texture scene = districtAtlasScenes != null && sceneIndex < districtAtlasScenes.Length
                ? districtAtlasScenes[sceneIndex] : null;
            if (scene != null)
            {
                if (districtMapImage != null) districtMapImage.texture = scene;
                if (districtAtlasBackdrop != null) districtAtlasBackdrop.texture = scene;
            }

            if (atlasChapterArtwork != null && districtAtlasScenes != null)
                for (int i = 0; i < atlasChapterArtwork.Length && i < districtAtlasScenes.Length; i++)
                    if (atlasChapterArtwork[i] != null) atlasChapterArtwork[i].texture = districtAtlasScenes[i];

            if (atlasMissionArtwork == null) return;
            for (int i = 0; i < atlasMissionArtwork.Length; i++)
            {
                RawImage art = atlasMissionArtwork[i];
                if (art == null) continue;
                Texture preview = ResolveAtlasMissionPreview(chapter, i);
                art.texture = preview != null ? preview : scene;
            }
        }

        private Texture ResolveAtlasMissionPreview(int chapter, int index)
        {
            switch (chapter)
            {
                case 1:
                    return index switch
                    {
                        0 => atlasFirstContactArtwork != null ? atlasFirstContactArtwork : m01MissionPreview,
                        1 => atlasEstablishBaseArtwork != null ? atlasEstablishBaseArtwork : m02MissionPreview,
                        2 => m03MissionPreview, 3 => m04MissionPreview,
                        4 => m05MissionPreview, _ => null
                    };
                case 2:
                    return index switch
                    {
                        0 => gridlockMissionPreview, 1 => supplyLineMissionPreview,
                        2 => marketLifelineMissionPreview, 3 => powerRelayMissionPreview,
                        4 => routeReopenedMissionPreview, _ => null
                    };
                case 3:
                    return index switch
                    {
                        0 => signalTraceMissionPreview, 1 => safehouseSweepMissionPreview,
                        2 => falseFrontMissionPreview, 3 => evidenceChainMissionPreview,
                        4 => networkBreakMissionPreview, _ => null
                    };
                default:
                    FutureMissionComicMission mission = FutureMissionComicCatalog.Find(chapter, index + 1);
                    return mission == null ? null : Resources.Load<Texture2D>("FutureMissionComics/" + mission.image);
            }
        }
    }
}
