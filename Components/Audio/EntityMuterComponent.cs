using System;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.VivHelper.Runtime.Audio.Muting;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.VivHelper.Components.Audio
{
    [RuntimeTypeAlias("VivHelper.Entities.EntityMuterComponent")]
    public class EntityMuterComponent : SourceComponentState
    {
        public string flag;
        private readonly EntityMuterContext context;

        public EntityMuterComponent(EntityMuterContext context, string flag = null) : base(true, false)
        {
            this.context = context;
            this.flag = flag;
        }

        public override void Added(object entity)
        {
            base.Added(entity);
            if (entity is SceneEntityState actual)
            {
                actual.PreUpdate += EntityPreUpdate;
                actual.PostUpdate += EntityPostUpdate;
            }

            foreach (SourceComponentState component in ((ISourceOwnedComponentList)entity).OwnedComponents)
                if (component is PlayerColliderComponentState collider)
                {
                    Action<RoomPlayerSession> original = collider.OnCollide;
                    collider.OnCollide = player =>
                    {
                        context.ObjPlayingAudio = entity;
                        original(player);
                        context.ObjPlayingAudio = null;
                    };
                }
        }

        private void EntityPreUpdate(SceneEntityState entity) => context.ObjPlayingAudio = entity;
        private void EntityPostUpdate(SceneEntityState entity) => context.ObjPlayingAudio = null;
    }
}
