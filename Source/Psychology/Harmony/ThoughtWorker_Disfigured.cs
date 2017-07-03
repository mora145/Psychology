﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;

namespace Psychology.Harmony
{
    [HarmonyPatch(typeof(ThoughtWorker_Disfigured), "CurrentSocialStateInternal")]
    public static class ThoughtWorker_DisfiguredPatch
    {
        [HarmonyPostfix]
        public static void Disable(ref bool __result, Pawn pawn)
        {
            if (pawn is PsychologyPawn)
            {
                __result = false;
            }
        }
    }
}
