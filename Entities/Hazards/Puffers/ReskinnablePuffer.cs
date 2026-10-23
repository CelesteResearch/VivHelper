// SPDX-License-Identifier: AGPL-3.0-only

using System.Linq;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.VivHelper.Entities.Hazards.Puffers;

[CustomEntity("VivHelper/ReskinnablePuffer")]
[RuntimeTypeAlias("VivHelper.Entities.ReskinnablePuffer")]
public class ReskinnablePuffer : OriginalPufferState
{
    private static readonly FieldInfo spriteField =
        typeof(OriginalPufferState).GetField("sprite", BindingFlags.Instance | BindingFlags.NonPublic);

    public static ReskinnablePuffer Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
        OriginalLevelEntityData data) => new(level, data, offset);

    public ReskinnablePuffer(OriginalLevelSceneRoot level, EntityData data, Vector2 offset) : base(
        new PixelActor(((ISourcePlatformCollisionScene)level).PlatformWorld, data.Position + offset),
        data.Position + offset, data.Bool("right"), level.EngineContext,
        ((ISourcePlatformTargetMotionEnvironment)level.EngineContext).TargetMotionPolicy)
    {
        var engine = level.EngineContext;
        var atlas = ((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
        var replacement = new Sprite(engine, atlas, data.Attr("Directory", "").TrimEnd('/') + "/");
        replacement.AddLoop("idle", "idle", .08f);
        replacement.AddLoop("alerted", "alerted", .08f);
        replacement.AddLoop("hidden", "hidden", .08f);
        replacement.Add("alert", "alert", .08f, "alerted");
        replacement.Add("explode", "explode", .08f, "hidden");
        replacement.Add("unalert", .08f, "idle", replacement.Animations["alert"].Frames.Reverse().ToArray());
        replacement.Add("recover", "recover", .05f, "idle");
        replacement.CenterOrigin();
// Match native DynData once at construction; all inherited hot paths read the real private sprite.
        Remove(Get<Sprite>());
        spriteField.SetValue(this, replacement);
        Add(replacement);
        replacement.Play("idle");
    }
}
