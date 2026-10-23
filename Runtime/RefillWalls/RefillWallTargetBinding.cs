// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Graphics.Animation;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Collectibles.Refills;

namespace Celeste.Mods.VivHelper.Runtime.RefillWalls
{
    public sealed class RefillWallTargetBinding
    {
        private readonly SceneEntityState entity;
        private readonly Type sourceFields;
        private readonly IRefillTargetState state;
        private readonly Func<SceneEntityState, float> readTimer;
        private readonly Action<SceneEntityState, float> writeTimer;
        private readonly Func<SceneEntityState, bool> readOneUse;
        private readonly Action<SceneEntityState, bool> writeOneUse;
        private readonly Func<SceneEntityState, bool> readUpperOneUse;
        private readonly Action respawn;
        public bool HasTimer { get; }

        private RefillWallTargetBinding(SceneEntityState entity, string respawnName)
        {
            this.entity = entity;
            sourceFields = entity is OriginalRefillState ? typeof(OriginalRefillState) : entity.GetType();
            state = entity as IRefillTargetState;
            if (state != null)
            {
                HasTimer = true;
                respawn = respawnName == "Respawn" ? state.RespawnRefill : BindRespawn(respawnName);
                return;
            }

            var timer = Member("respawnTimer") ?? Member("_respawnTimeRemaining");
            HasTimer = timer != null;
            if (timer != null)
            {
                readTimer = Getter<float>(timer);
                writeTimer = Setter<float>(timer);
            }

            var oneUse = Member("oneUse") ?? Member("OneUse");
            if (oneUse != null)
            {
                readOneUse = Getter<bool>(oneUse);
                writeOneUse = Setter<bool>(oneUse);
            }

            if (Member("oneUse") != null && Member("OneUse") is MemberInfo upperOneUse)
                readUpperOneUse = Getter<bool>(upperOneUse);
            respawn = BindRespawn(respawnName);
        }

        public static RefillWallTargetBinding Create(SceneEntityState entity, string respawnName) =>
            new(entity, respawnName);

        public float Timer
        {
            get => state != null ? state.RefillRespawnTimer : readTimer(entity);
            set
            {
                if (state != null) state.RefillRespawnTimer = value;
                else writeTimer(entity, value);
            }
        }

        public bool OneUse => state != null
            ? state.RefillOneUse
            : (readOneUse != null && readOneUse(entity)) || (readUpperOneUse != null && readUpperOneUse(entity));

        public void SetOneUse(bool value)
        {
            if (state != null) state.RefillOneUse = value;
            else writeOneUse?.Invoke(entity, value);
        }

        public void Respawn() => respawn();

        public object ReadImage(string name) =>
            name == "sprite" && state != null ? state.RefillSprite : ReadMember(name);

        public static SpriteImage FirstImage(SceneEntityState entity)
        {
            foreach (var component in entity)
                if (component is SpriteImage image && component is not Sprite)
                    return image;
            return null;
        }

        private object ReadMember(string name) => Member(name) switch
        {
            FieldInfo field => field.GetValue(entity), PropertyInfo property => property.GetValue(entity), _ => null
        };

        private MemberInfo Member(string name)
        {
            for (Type type = sourceFields; type != null; type = type.BaseType)
            {
                var field = type.GetField(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                var property = type.GetProperty(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (property != null) return property;
            }

            return null;
        }

        private Action BindRespawn(string name)
        {
            for (Type type = sourceFields; type != null; type = type.BaseType)
            {
                var method = type.GetMethod(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null, Type.EmptyTypes, null);
                if (method != null) return (Action)method.CreateDelegate(typeof(Action), entity);
            }

            return () => throw new MissingMethodException(sourceFields.FullName, name);
        }

        private static Func<SceneEntityState, T> Getter<T>(MemberInfo field)
        {
            var method = new DynamicMethod("ReadRefill" + field.Name, typeof(T), new[] { typeof(SceneEntityState) },
                field.DeclaringType, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, field.DeclaringType);
            if (field is FieldInfo f) il.Emit(OpCodes.Ldfld, f);
            else il.Emit(OpCodes.Call, ((PropertyInfo)field).GetGetMethod(true));
            il.Emit(OpCodes.Ret);
            return (Func<SceneEntityState, T>)method.CreateDelegate(typeof(Func<SceneEntityState, T>));
        }

        private static Action<SceneEntityState, T> Setter<T>(MemberInfo field)
        {
            var method = new DynamicMethod("WriteRefill" + field.Name, typeof(void),
                new[] { typeof(SceneEntityState), typeof(T) }, field.DeclaringType, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, field.DeclaringType);
            il.Emit(OpCodes.Ldarg_1);
            if (field is FieldInfo f) il.Emit(OpCodes.Stfld, f);
            else il.Emit(OpCodes.Call, ((PropertyInfo)field).GetSetMethod(true));
            il.Emit(OpCodes.Ret);
            return (Action<SceneEntityState, T>)method.CreateDelegate(typeof(Action<SceneEntityState, T>));
        }
    }
}
