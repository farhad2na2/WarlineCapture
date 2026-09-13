using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class SelectionUiReadModelLookup
    {
        public bool HasFocusedUnit(EntityManager entityManager, Entity focusedUnit)
        {
            return focusedUnit != Entity.Null &&
                   entityManager.Exists(focusedUnit) &&
                   entityManager.HasComponent<Faction>(focusedUnit) &&
                   (!entityManager.HasComponent<UnitHealth>(focusedUnit) || entityManager.GetComponentData<UnitHealth>(focusedUnit).Current>0);
        }
    }
}
