using System;
using System.Collections.Generic;
using Game.Components;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingPlacementLifecycleCompositionSystemHelper
    {
        public bool Rotate(RotateContext context)
        {
            PlacementState placement = ActivePlacement;
            if (placement?.Definition == null)
                return false;

            placement.ManualRotation = true;
            placement.AutoRotateVertical = !placement.AutoRotateVertical;
            context.UpdatePlacementVisual?.Invoke(placement, false, default);
            return true;
        }
    }
}
