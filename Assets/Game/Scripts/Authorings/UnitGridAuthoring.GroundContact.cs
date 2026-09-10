using System.Collections.Generic;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Authoring
{
    public partial class UnitGridAuthoring
    {
        [SerializeField,HideInInspector] private float[] animationGroundOffsets;
        public IReadOnlyList<float> AnimationGroundOffsets=>animationGroundOffsets;
        private partial class UnitGridBaker
        {
            private void AddGroundContact(Entity entity,UnitGridAuthoring authoring)
            {
                AddComponent(entity,new UnitGroundOffsetComponent{Value=authoring.ConfiguredGroundOffset});
                if(authoring.animationGroundOffsets==null||authoring.animationGroundOffsets.Length==0)return;
                var offsets=AddBuffer<UnitAnimationGroundOffset>(entity);
                foreach(float offset in authoring.animationGroundOffsets)offsets.Add(new UnitAnimationGroundOffset{Value=offset});
            }
        }
    }
}
