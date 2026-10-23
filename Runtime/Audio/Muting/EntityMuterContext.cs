using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Audio.Runtime;
using Celeste.Core.Audio;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Components.Audio;
using Celeste.Mods.VivHelper.Runtime.Audio.Changes;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;
using Celeste.Scene.Entities;

namespace Celeste.Mods.VivHelper.Runtime.Audio.Muting
{
    public sealed class EntityMuterContext
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, EntityMuterContext> contexts = new();
        private readonly RefillCancelContext session;
        public ISourceEngineContext Engine { get; }
        public OriginalGameAudioSession Audio { get; }
        public bool OverrideMute;
        public object ObjPlayingAudio;

        private EntityMuterContext(ISourceEngineContext engine)
        {
            Engine = engine;
            Audio = (OriginalGameAudioSession)((ISourceSoundSourceAudioEnvironment)engine).SoundSourceAudio;
            session = RefillCancelContext.For(engine);
        }

        public static EntityMuterContext For(ISourceEngineContext engine) =>
            contexts.GetValue(engine, static owner => new(owner));

        public string ResolvePath(string path, out AudioEvent changed)
        {
            if (OverrideMute) path = "event:/none";
            else if (ObjPlayingAudio is SceneEntityState entity)
            {
                var muter = entity.Get<EntityMuterComponent>();
                if (muter != null && (string.IsNullOrWhiteSpace(muter.flag) ||
                                      ((ObjPlayingAudio as ISourceEntityScene)?.Scene as OriginalLevelSceneRoot)
                                      ?.Session?.GetFlag(muter.flag) == true))
                    path = "event:/none";
            }

            changed = null;
            if (session.Session?.AudioChanges?.TryGetValue(path, out SoundChange value) == true)
                changed = value?.GrabEvent();
            return changed != null && changed.Name != "default" ? changed.Name : path;
        }
    }
}
