using System;
using System.Reflection;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.VivHelper.Entities.Enemies.Seekers;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using VanillaController = Celeste.Game.Gameplay.Enemies.Seekers.SeekerEffectsControllerState;

namespace Celeste.Mods.VivHelper.Runtime.Enemies.Seekers
{
    public sealed class CustomSeekerEffectsHooks : IDisposable
    {
        private delegate void AppendDistances(RoomPlayerSession player, ref float nearest, ref float attacking);

        private readonly ISourceEngineContext engine;
        private readonly string seekerRegistryKey;
        private readonly ILHook hook;

        private CustomSeekerEffectsHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            seekerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(CustomSeeker))
                .RegistryKey;
            hook = new ILHook(typeof(VanillaController).GetMethod(nameof(VanillaController.Update)), Patch);
        }

        public static IDisposable Install(ISourceEngineContext engine) => new CustomSeekerEffectsHooks(engine);
        public void Dispose() => hook.Dispose();

        private void Patch(ILContext il)
        {
            var cursor = new ILCursor(il);
            VariableDefinition player = null, nearest = null, attacking = null;
            foreach (Instruction instruction in il.Body.Instructions)
            {
                if (instruction.MatchLdcR4(-1f) && instruction.Next != null &&
                    instruction.Next.MatchStloc(out int localIndex))
                {
                    if (nearest == null) nearest = il.Body.Variables[localIndex];
                    else if (attacking == null) attacking = il.Body.Variables[localIndex];
                }

                if (instruction.Operand is GenericInstanceMethod method && method.Name == "GetTrackedEntity" &&
                    method.GenericArguments.Count == 1 &&
                    method.GenericArguments[0].FullName == typeof(RoomPlayerSession).FullName &&
                    instruction.Next != null &&
                    instruction.Next.MatchStloc(out int playerIndex)) player = il.Body.Variables[playerIndex];
            }

            if (player == null || nearest == null || attacking == null)
                throw new InvalidOperationException("Seeker effects locals are not present.");
            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdloc(attacking.Index), i => i.MatchLdcR4(0f),
                    i => i.OpCode.Code == Code.Blt_Un || i.OpCode.Code == Code.Blt_Un_S))
                throw new InvalidOperationException("Seeker effects distance branch is not present.");
            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Ldloc, player);
            cursor.Emit(OpCodes.Ldloca, nearest);
            cursor.Emit(OpCodes.Ldloca, attacking);
            cursor.EmitDelegate<AppendDistances>(AddSeekerCheck);
            int counts = 0;
            cursor.Index = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       i => i.MatchLdstr("Celeste.Seeker"),
                       i => i.Operand is MethodReference m && m.Name == "GetTrackedEntities",
                       i => i.Operand is MethodReference m && m.Name == "get_Count"))
            {
                cursor.Emit(OpCodes.Ldloc, player);
                cursor.EmitDelegate<Func<int, RoomPlayerSession, int>>(AddSeekerCount);
                counts++;
            }

            if (counts != 2) throw new InvalidOperationException("Seeker effects count sites are not present.");
        }

        private void AddSeekerCheck(RoomPlayerSession player, ref float nearest, ref float attacking)
        {
            if (player?.Scene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, engine)) return;
            foreach (CustomSeeker seeker in level.SourceScene.Entities.GetTrackedEntities(seekerRegistryKey))
            {
                float distance =
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.DistanceSquared(player.Center, seeker.Center);
                if (!seeker.Regenerating) nearest = !(nearest < 0f) ? Math.Min(nearest, distance) : distance;
                if (seeker.Attacking) attacking = !(attacking < 0f) ? Math.Min(attacking, distance) : distance;
            }
        }

        private int AddSeekerCount(int count, RoomPlayerSession player)
        {
            if (player?.Scene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, engine)) return count;
            return count + level.SourceScene.Entities.GetTrackedEntities(seekerRegistryKey).Count;
        }
    }
}
