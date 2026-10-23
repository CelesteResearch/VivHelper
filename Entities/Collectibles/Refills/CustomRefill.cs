// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.VivHelper.Spawn;
using Celeste.Mods.VivHelper.Runtime.RefillCancellation;

namespace Celeste.Mods.VivHelper.Entities.Collectibles.Refills;

[RuntimeTypeAlias("VivHelper.Entities.CustomRefill")]
[CustomEntity("VivHelper/CustomRefill = CR1", "VivHelper/CustomRefillString = CR2")]
public partial class CustomRefill : SceneEntityState
{
    private enum WobbleTypes
    {
        None = -1,
        YOnly,
        XOnly,
        Circle
    }

    public float iStamina;

    public bool iStamRef;

    public bool logic;

    public int iDashes;

    public short iDashRef = -3;

    public float oStamina;

    public bool oStamRef;

    public int oDashes;

    public bool oDashRef;

    public int count = 0;

    public int UseNumber = int.MaxValue;

    public bool usecustomoutline = false;

    public string directory;

    public float scale;

    public float imageScale;

    public ParticleTypePresentation P_Shatter;

    public ParticleTypePresentation P_Glow;

    public ParticleTypePresentation P_Regen;

    public ParticleTypePresentation particle;

    private Sprite sprite;

    private Sprite flash;

    private SpriteImage outline;

    private BloomPointState bloom;

    private VertexLightState light;

    private string[] CustomAudio;

    protected WigglerComponent wiggler;

    private WobbleTypes wobble;

    private SineWaveState[] sine;

    private OriginalLevelSceneRoot level;

    public float RespawnTime;

    private float respawnTimer;

    private int[] Depths = new int[2];

    public string flagToggle;

    public bool flagInvert;

    public float FreezeTime;

    internal string mapName;

    private readonly IOriginalRefillProcess engine;
    private readonly RefillCancelContext context;
    public bool UseCustomOutline => usecustomoutline || context.Settings.ShowOneUseOnCustomRefills;

    public static SceneEntityState CR1(OriginalLevelSceneRoot level,
        Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data) =>
        new CustomRefill(((ISourceEngineContextScene)level).EngineContext, data, offset, 0);

    public static SceneEntityState CR2(OriginalLevelSceneRoot level,
        Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data) =>
        new CustomRefill(((ISourceEngineContextScene)level).EngineContext, data, offset, 1);
}
