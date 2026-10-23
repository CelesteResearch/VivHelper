// SPDX-License-Identifier: AGPL-3.0-only

using System;
using Celeste.Core.Components;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.VivHelper.Entities.Enemies.Seekers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.CustomSeekerCollider")]
    public class CustomSeekerCollider : SourceComponentState
    {
        public Action<CustomSeeker> OnCollide;
        public ColliderGeometry Collider;

        public CustomSeekerCollider(Action<CustomSeeker> onCollide, ColliderGeometry collider = null) : base(false,
            false)
        {
            OnCollide = onCollide;
            Collider = null;
        }

        public void Check(CustomSeeker seeker)
        {
            if (OnCollide != null)
            {
                var entity = (ISourceEntityCollider)Entity;
                var collider = entity.Collider;
                if (Collider != null) entity.Collider = Collider;
                if (SourceEntityCollision.Check(seeker, entity)) OnCollide(seeker);
                entity.Collider = collider;
            }
        }
    }
}
