// SPDX-License-Identifier: AGPL-3.0-only

using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Rendering.Textures;

namespace Celeste.Mods.VivHelper.Entities.Solids.Growing
{
    public partial class GrowBlock
    {
        private VirtualRenderTarget buffer;
        private SourceRenderTarget2DResource bakedResource;

        public void BeforeRender()
        {
            if (tileGrid == null) return;
            if (buffer == null)
                buffer = ((ISourceVirtualContentEnvironment)engine).VirtualContent.CreateRenderTarget("viv-grow-block",
                    (int)width, (int)height, false, false, 0);
            if (ReferenceEquals(bakedResource, buffer.Target)) return;
            var context = ((ISourceRenderEnvironment)engine).CurrentContext;
            var device = ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice;
            var batch = ((ISourceDrawScope)context).Draw.SpriteBatch;
            device.SetRenderTarget(buffer.Target);
            device.Clear(PackedColor.Transparent);
            batch.Begin(new SourceRenderBatchState(SourceSpriteSortMode.Deferred, SourceBlendState.AlphaBlend,
                SourceSamplerState.LinearClamp, SourceDepthStencilState.None,
                SourceRasterizerState.CullCounterClockwise, null, Matrix4x4.Identity));
            tileGrid.RenderAt(Vector2.Zero, context);
            batch.End();
            bakedResource = buffer.Target;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (EverestCullHelper.IsRectangleVisible(engine, Hitbox.AbsoluteLeft, Hitbox.AbsoluteTop, Hitbox.Width,
                    Hitbox.Height, 4f) && buffer != null && alphaC > 0f)
                ((ISourceDrawScope)context).Draw.SpriteBatch.Draw(buffer.Target,
                    Position + .5f * new Vector2(width, height) + offset, buffer.Bounds,
                    (PackedColor.White * alphaC).PackedValue, (1f - alphaA) * .25f,
                    new Vector2(width, height) * .5f, alphaB, SourceSpriteEffects.None, 0f);
        }

        public override void Removed(object scene)
        {
            buffer?.Dispose();
            buffer = null;
            bakedResource = null;
            base.Removed(scene);
        }

        public override void SceneEnd(object scene)
        {
            buffer?.Dispose();
            buffer = null;
            bakedResource = null;
            base.SceneEnd(scene);
        }
    }
}
