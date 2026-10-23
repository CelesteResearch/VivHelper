using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Formats.Graphics.Atlas;
using Celeste.Graphics.Content;

namespace Celeste.Mods.VivHelper.Runtime.Graphics
{
    public sealed class VivHelperGraphicsResources
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, VivHelperGraphicsResources>
            Resources = new();

        private readonly ISourceEngineContext engine;
        private SpriteBank spriteBank;
        private VivHelperGraphicsResources(ISourceEngineContext engine) => this.engine = engine;

        public static VivHelperGraphicsResources For(ISourceEngineContext engine)
            => Resources.GetValue(engine, owner => new VivHelperGraphicsResources(owner));

        public SpriteBank SpriteBank =>
            spriteBank ?? throw new InvalidOperationException("VivHelper content has not been loaded.");

        public void LoadContent()
        {
            var graphics = ((ISourceGameGraphicsEnvironment)engine).GameGraphics;
            var xml = ((ISourceAtlasLoaderEnvironment)engine).LoadContentXml("Graphics/VivHelper/Sprites.xml");
            spriteBank = new SpriteBank(engine, graphics.GameAtlas, xml);
        }
    }
}
