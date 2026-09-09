using System;
using Game.Missions.Contracts;
using UnityEngine;

namespace Game.Configs
{
    [Serializable]
    public struct MissionGuidanceStepConfig
    {
        [SerializeField] private string stepId, titleKey, bodyKey;
        [SerializeField] private MissionGuidanceActionKind action;
        [SerializeField] private MissionGuidanceCompletionKind completion;
        [SerializeField] private bool optional;
        public string StepId=>stepId;
        public string TitleKey=>titleKey;
        public string BodyKey=>bodyKey;
        public MissionGuidanceActionKind Action=>action;
        public MissionGuidanceCompletionKind Completion=>completion;
        public bool Optional=>optional;
    }
}
