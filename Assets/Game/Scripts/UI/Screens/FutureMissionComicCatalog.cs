using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    [Serializable]
    public sealed class FutureMissionComicLine
    {
        public string stage;
        public string speaker;
        public string en;
        public string fa;
    }

    [Serializable]
    public sealed class FutureMissionComicMission
    {
        public int chapter;
        public int number;
        public string id;
        public string titleEn;
        public string titleFa;
        public string summaryEn;
        public string summaryFa;
        public string objectiveEn;
        public string objectiveFa;
        public string image;
        public string commsImage;
        public string debriefImage;
        public FutureMissionComicLine[] lines;

        public string Title(bool persian) => persian ? titleFa : titleEn;
        public string Summary(bool persian) => persian ? summaryFa : summaryEn;
        public string Objective(bool persian) => persian ? objectiveFa : objectiveEn;
        public string ImageForStage(string stage) => stage switch
        {
            "comms" => commsImage,
            "debrief" => debriefImage,
            _ => image
        };
    }

    [Serializable]
    internal sealed class FutureMissionComicCatalogFile
    {
        public FutureMissionComicMission[] missions;
    }

    [Serializable]
    public sealed class BookendComicPage
    {
        public string image;
        public string speaker;
        public string en;
        public string fa;
        public string lowEn;
        public string lowFa;
    }

    [Serializable]
    public sealed class BookendComicSequence
    {
        public string id;
        public BookendComicPage[] pages;
    }

    [Serializable]
    internal sealed class BookendComicCatalogFile
    {
        public BookendComicSequence[] sequences;
    }

    public static class BookendComicCatalog
    {
        private static BookendComicCatalogFile catalog;

        public static BookendComicSequence Find(string id)
        {
            if (catalog == null)
            {
                TextAsset source = Resources.Load<TextAsset>("FutureMissionComics/bookends");
                if (source == null)
                {
                    Debug.LogError("[BookendComics] Catalog resource missing.");
                    return null;
                }
                catalog = JsonUtility.FromJson<BookendComicCatalogFile>(source.text);
            }
            BookendComicSequence[] sequences = catalog?.sequences;
            if (sequences == null)
                return null;
            for (int index = 0; index < sequences.Length; index++)
                if (sequences[index].id == id)
                    return sequences[index];
            return null;
        }
    }

    public static class FutureMissionComicCatalog
    {
        private const string ResourcePath = "FutureMissionComics/catalog";
        private static FutureMissionComicCatalogFile catalog;

        public static FutureMissionComicMission Find(int chapter, int number)
        {
            if (catalog == null)
            {
                TextAsset source = Resources.Load<TextAsset>(ResourcePath);
                if (source == null)
                {
                    Debug.LogError("[FutureMissionComics] Catalog resource missing.");
                    return null;
                }
                catalog = JsonUtility.FromJson<FutureMissionComicCatalogFile>(source.text);
            }

            FutureMissionComicMission[] missions = catalog?.missions;
            if (missions == null)
                return null;
            for (int index = 0; index < missions.Length; index++)
                if (missions[index].chapter == chapter && missions[index].number == number)
                    return missions[index];
            return null;
        }
    }
}
