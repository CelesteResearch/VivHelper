// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.VivHelper.Entities.Spinners
{
    public partial class CustomSpinner
    {
        protected override void CreateSprites()
        {
            if (expanded) return;
            ((ISourceRandomProcessEnvironment)engine).RandomProcess.PushRandom(randomSeed);
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            AtlasEntry texture = Random.Choose(atlas.GetAtlasSubtextures(fgdirectory + subdirectory));
            if (IsRainbow) tint = GetHue(Position);
            if (createConnectors)
            {
                foreach (object entry in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                             typeof(CustomSpinner)))
                {
                    var other = (CustomSpinner)entry;
                    if (!other.createConnectors || other.ID <= ID || other.AttachToSolid != AttachToSolid) continue;
                    if (!(VectorArithmetic.LengthSquared(other.Position - Position) <
                          (float)Math.Pow(12f * (scale + other.scale), 2.0)))
                        continue;
                    float angle = Vector2Geometry.Angle(other.Position, Position);
                    Vector2 first = Position + VectorArithmetic.RotateTowards(Vector2.UnitX, -angle, 6.3f);
                    Vector2 second = other.Position + VectorArithmetic.RotateTowards(Vector2.UnitX, angle, 6.3f);
                    var arithmetic = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
                    Vector2 at = new Vector2(arithmetic.Lerp(first.X, second.X, .5f),
                        arithmetic.Lerp(first.Y, second.Y, .5f)) - Position;
                    AddConnector(at, (other.scale + scale) / 2f, PackedColor.Lerp(other.tint, tint, .5f));
                }
            }

            int mask = 0;
            if (!SolidCheck(Position + new Vector2(-4f * scale, -4f * scale))) mask |= 1;
            if (!SolidCheck(Position + new Vector2(4f * scale, -4f * scale))) mask |= 2;
            if (!SolidCheck(Position + new Vector2(-4f * scale, 4f * scale))) mask |= 4;
            if (!SolidCheck(Position + new Vector2(4f * scale, 4f * scale))) mask |= 8;
            expanded = true;
            ((ISourceRandomProcessEnvironment)engine).RandomProcess.PopRandom();
            if (mask == 15)
            {
                var image = new SpriteImage(texture).CenterOrigin().SetColor(tint);
                image.Scale = Vector2.One * scale / imageScale;
                Add(image);
            }
            else
            {
                if ((mask & 1) != 0) AddQuadrant(texture, 0, 0, 12, 12);
                if ((mask & 2) != 0) AddQuadrant(texture, 10, 0, 2, 12);
                if ((mask & 8) != 0) AddQuadrant(texture, 10, 10, 2, 2);
                if ((mask & 4) != 0) AddQuadrant(texture, 0, 10, 12, 2);
            }

            if (borderColor != PackedColor.Transparent) CreateBorder(borderColor);
        }

        public override void UpdateHue()
        {
            foreach (var component in OwnedComponents)
                if (component is SpriteImage image)
                    image.Color = GetHue(Position + image.Position);

            if (filler == null) return;
            foreach (var component in filler.OwnedComponents)
                if (component is SpriteImage image)
                    image.Color = GetHue(Position + image.Position);
        }

        private void AddQuadrant(AtlasEntry texture, int x, int y, int originX, int originY)
        {
            var image = new SpriteImage(texture.GetSubtexture((int)(x * imageScale), (int)(y * imageScale),
                (int)(14f * imageScale),
                (int)(14f * imageScale))).SetOrigin(originX * imageScale, originY * imageScale);
            image.Color = tint;
            image.Scale = Vector2.One * scale / imageScale;
            Add(image);
        }

        private void AddConnector(Vector2 at, float connectorScale, PackedColor connectorColor)
        {
            if (filler == null)
            {
                filler = new SceneEntityState(((Celeste.Core.Components.ISourceEntitySourceDataEnvironment)engine)
                    .EntitySourceDataRegistration, Position) { Depth = Depth + 1, PresentationOwner = this };
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(filler);
            }

            List<AtlasEntry> textures = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas
                .GetAtlasSubtextures(bgdirectory + subdirectory);
            SpriteImage image;
            if (isSeeded)
            {
                int actualSeed = seed;
                if (actualSeed < 0)
                {
                    var levels = ((Celeste.Game.Content.Maps.OriginalChapterMapData)Level.Session.MapData).Levels;
                    actualSeed = levels.FindIndex(room => room.Name == Level.Session.LevelData.Name) + 977;
                    actualSeed = Mod(unchecked(actualSeed * actualSeed * actualSeed), 30977);
                }

                int x = (int)Position.X, y = (int)Position.Y;
                image = new SpriteImage(textures[ChooseIndex(x, y, actualSeed, textures.Count)]);
                image.Rotation = ChooseIndex(x, y, actualSeed, 4) * ((float)Math.PI / 2f);
            }
            else
            {
                image = new SpriteImage(Random.Choose(textures));
                image.Rotation = Random.Choose(0, 1, 2, 3) * ((float)Math.PI / 2f);
            }

            image.Position = at;
            image.CenterOrigin();
            image.Scale = Vector2.One * connectorScale / imageScale;
            image.Color = IsRainbow ? GetHue(Position + at) : connectorColor;
            filler.Add(image);
        }

        private static int Mod(int value, int modulus) => (value % modulus + modulus) % modulus;

        private static int ChooseIndex(int x, int y, int seed, int count)
        {
            int a = Mod(unchecked(171 * x), 30269),
                b = Mod(unchecked(172 * y), 30307),
                c = Mod(unchecked(170 * seed), 30323);
            double value = a / 30269.0 + b / 30307.0 + c / 30323.0;
            return (int)((value % 1.0 + 1.0) % 1.0 * count);
        }
    }
}
