using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection;
using Celeste.Core.Reflection.Assemblies;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Components.Audio;
using Celeste.Mods.VivHelper.Runtime.Audio.Muting;
using Celeste.Mods.VivHelper.Runtime.Entities.Awakening;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Components;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Entities.Audio
{
    [CustomEntity("VivHelper/EntityMuter")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("VivHelper.Entities.EntityMuter")]
    public class EntityMuter : SceneEntityState, PostAwakeHolder
    {
        public List<Type> Types;
        public List<Type> assignableTypes;
        public bool all;
        public string flag;
        private readonly EntityMuterContext context;

        public static EntityMuter Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public EntityMuter(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            context = EntityMuterContext.For(engine);
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            all = data.Bool("all", false);
            string names = data.Attr("Types", "");
            assignableTypes = new List<Type>();
            Types = new List<Type>();
            Depth = int.MinValue;
            if (!string.IsNullOrEmpty(names))
                AppendTypes(names, ((ISourceEverestReflectionEnvironment)engine).Reflection.GetFakeEntryAssembly());
        }

        private void AppendTypes(string names, SourceLogicalAggregateAssembly discovery)
        {
            if (string.IsNullOrWhiteSpace(names))
            {
                assignableTypes.Add(typeof(SceneEntityState));
                return;
            }

            foreach (string name in names.Split(','))
            {
                string text = name.Trim();
                bool assignable = text.StartsWith("*");
                Type type = discovery.GetType(assignable ? text.Substring(1) : text, false);
                if (type != null && typeof(SceneEntityState).IsAssignableFrom(type))
                    (assignable ? assignableTypes : Types).Add(type);
            }
        }

        private bool Matches(Type type)
        {
            if (type == null) return false;
            foreach (Type exact in Types)
                if (type == exact)
                    return true;
            foreach (Type assignable in assignableTypes)
                if (assignable != null && assignable.IsAssignableFrom(type))
                    return true;
            return false;
        }

        public void PostAwake(object scene)
        {
            Collidable = true;
            foreach (object item in ((ISourceScene)scene).SourceScene.Entities.Entities)
            {
                if (!(item is ISourceEntityCollider collider) || !SourceEntityCollision.Check(this, collider)) continue;
                bool collidable = collider.Collidable;
                ((ISourceMutableEntityState)item).Collidable = true;
                if (SourceEntityCollision.Check(this, collider) && Matches(item.GetType()))
                {
                    ((ISourceOwnedComponentList)item).OwnedComponents.Add(new EntityMuterComponent(context));
                    if (!all)
                    {
                        ((ISourceMutableEntityState)item).Collidable = collidable;
                        break;
                    }
                }

                ((ISourceMutableEntityState)item).Collidable = collidable;
            }

            RemoveSelf();
        }
    }
}
