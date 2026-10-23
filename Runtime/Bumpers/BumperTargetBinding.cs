// SPDX-License-Identifier: AGPL-3.0-only

using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Gameplay.Hazards.Bumpers;
using Celeste.Scene.Entities;

namespace Celeste.Mods.VivHelper.Runtime.Bumpers
{
    public sealed class BumperTargetBinding
    {
        private readonly SceneEntityState entity;
        private readonly Func<SceneEntityState, Vector2> readAnchor;
        private readonly Action<SceneEntityState, Vector2> writeAnchor;
        private readonly Func<SceneEntityState, float> readTimer;
        private readonly Action<SceneEntityState, float> writeTimer;

        public BumperTargetBinding(SceneEntityState entity, Type sourceFields, string anchorName)
        {
            this.entity = entity;
            var anchor = Find(sourceFields, anchorName);
            if (MemberType(anchor) != typeof(Vector2))
                throw new MissingFieldException("The bumper has no Vector2 " + anchorName + " field.");
            var timer = Find(sourceFields, "respawnTimer");
            if (MemberType(timer) != typeof(float))
                throw new MissingFieldException("The bumper has no respawnTimer field.");
            readAnchor = Getter<Vector2>(anchor);
            writeAnchor = Setter<Vector2>(anchor);
            readTimer = Getter<float>(timer);
            writeTimer = Setter<float>(timer);
        }

        public Vector2 Anchor
        {
            get => readAnchor(entity);
            set => writeAnchor(entity, value);
        }

        public float Timer
        {
            get => readTimer(entity);
            set => writeTimer(entity, value);
        }

        private static Type MemberType(MemberInfo member) =>
            member is FieldInfo field ? field.FieldType : (member as PropertyInfo)?.PropertyType;

        private static MemberInfo Find(Type source, string name)
        {
            for (var type = source; type != null; type = type.BaseType)
            {
                var field = type.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
                var property = type.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null) return property;
            }

            return null;
        }

        private static Func<SceneEntityState, T> Getter<T>(MemberInfo field)
        {
            var method = new DynamicMethod("ReadBumper" + field.Name, typeof(T), new[] { typeof(SceneEntityState) },
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
            var method = new DynamicMethod("WriteBumper" + field.Name, typeof(void),
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
