using System;
using System.Collections.Generic;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    internal static class MenuUiCampaignMediaPreservation
    {
        internal static void Apply(GameObject root)
        {
            var data=new SerializedObject(root.GetComponentInChildren<CampaignOperationsScreenView>(true));
            var media=new Dictionary<string,string> {
                {"m04MissionPreview", "2a3df06fb843e4b19a65c408ea7aecf6"},
                {"atlasFirstContactArtwork", "e7046971ea8e94860b859015e1002c7d"},
                {"atlasEstablishBaseArtwork", "64278a468d4a247be8cee6b10bb36817"},
                {"m05MissionPreview", "6fac5173b42724e0da3626d90eba0590"},
                {"evidenceChainMissionPreview", "e42b38417d4e84835a57b596ba398dbf"},
                {"falseFrontMissionPreview", "f47a92d1ccf944c579bacd75431941e9"},
                {"gridlockMissionPreview", "ae7730a65d54f44d79a4c9e1e1596f74"},
                {"marketLifelineMissionPreview", "2bd2f0098ed644d3e9e1badc21efd6e7"},
                {"networkBreakMissionPreview", "bff3262689ad4e0eb588c19c8612629a"},
                {"powerRelayMissionPreview", "ab1da26feac9148b5ab83f640a0c583a"},
                {"m03MissionPreview", "0f000af5086ec458f9fa30b1191960d6"},
                {"routeReopenedMissionPreview", "950ac7cfec1a4470d918e66910e391e3"},
                {"safehouseSweepMissionPreview", "54bc1a131ca5c4791b09cf1ae592dee9"},
                {"signalTraceMissionPreview", "3bd46195ff2054779a27f436d52d496e"},
                {"supplyLineMissionPreview", "b99d871cee7b64c25a2a9970a28c409f"},
                {"m01MissionPreview", "140028563df49495c8ea79096c717ab4"},
                {"m02MissionPreview", "140028563df49495c8ea79096c717ab4"},
            };
            foreach(var entry in media)
            {
                var property=data.FindProperty(entry.Key);if(property==null)throw new MissingFieldException(entry.Key);
                var texture=AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(entry.Value));
                if(texture==null)throw new MissingReferenceException("Campaign media missing: "+entry.Key);
                property.objectReferenceValue=texture;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
