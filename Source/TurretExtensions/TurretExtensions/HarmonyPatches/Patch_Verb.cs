﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TurretExtensions
{

    public static class Patch_Verb
    {

        [HarmonyPatch(typeof(Verb), nameof(Verb.DrawHighlight))]
        public static class DrawHighlight
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
            {
                #if DEBUG
                    Log.Message("Transpiler start: Verb.DrawHighlight (no matches)");
                #endif

                var instructionList = instructions.ToList();
                var drawRadiusRingInfo = AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.DrawRadiusRing));
                var tryDrawFiringConeInfo = AccessTools.Method(typeof(DrawHighlight), nameof(DrawHighlight.TryDrawFiringCone));

                var instructionToBranchTo = instructionList[instructionList.FirstIndexOf(i => i.OperandIs(drawRadiusRingInfo)) + 1];
                var branchLabel = ilGen.DefineLabel();
                instructionToBranchTo.labels.Add(branchLabel);


                yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                yield return new CodeInstruction(OpCodes.Call, tryDrawFiringConeInfo); // ShouldDrawFiringCone(this)
                yield return new CodeInstruction(OpCodes.Brtrue_S, branchLabel);

                /*

                if (!ShouldDrawFiringCone(this))
                    this.verbProps.DrawRadiusRing(this.caster.Position);

                */

                for (int i = 0; i < instructionList.Count; i++)
                    yield return instructionList[i];

            }

            private static bool TryDrawFiringCone(Verb instance)
            {
                if (instance.Caster is Building_Turret turret && TurretExtensionsUtility.FiringArcFor(turret) < 360)
                {
                    TurretExtensionsUtility.TryDrawFiringCone(turret, instance.verbProps.range);
                    return true;
                }
                return false;
            }

        }

        [HarmonyPatch(typeof(Verb), nameof(Verb.CanHitTargetFrom))]
        public static class CanHitTargetFrom
        {

            public static void Postfix(Verb __instance, LocalTargetInfo targ, ref bool __result)
            {
                // Also take firing arc into consideration if the caster is a turret
                if (__instance.Caster is Building_Turret && !targ.Cell.WithinFiringArcOf(__instance.caster))
                    __result = false;
            }

        }

    }

}
