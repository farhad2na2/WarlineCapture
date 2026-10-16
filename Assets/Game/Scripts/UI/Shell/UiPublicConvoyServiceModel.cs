using System;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class UiSkirmishPublicObjectiveModel
    {
        public UiPublicConvoyService Service;
    }
    public sealed class UiPublicConvoyService
    {
        public string PublicId,FacilityPublicId;
        public Vector3 World;
        public bool Available;
        public int Materials;
        public float EligibilityRadiusMetres;
        public UiPublicServiceDestination[] Destinations=Array.Empty<UiPublicServiceDestination>();
        public UiPublicConvoyRepairTruck[] Trucks=Array.Empty<UiPublicConvoyRepairTruck>();
    }
    public struct UiPublicServiceDestination
    {
        public Vector3 World;
        public bool GroundAvailable;
        public int PresentedGroundId;
    }
    public sealed class UiPublicConvoyRepairTruck
    {
        public string OriginalPublicId;
        public uint GroupId;
        public int Health,MaximumHealth,Materials;
        public long DurationTicks;
        public bool Affordable;
    }
}
