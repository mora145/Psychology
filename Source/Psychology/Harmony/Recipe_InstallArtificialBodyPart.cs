﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using Harmony;

namespace Psychology.Harmony
{
    [HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), nameof(Recipe_InstallArtificialBodyPart.ApplyOnPawn))]
    public static class Recipe_InstallArtificialBodyPart_ApplyPatch
    {
        [HarmonyPrefix]
        public static void BleedingHeartThought(Pawn pawn, Pawn billDoer)
        {
            //TODO: Account for surgery failure
            if(billDoer != null && billDoer.needs.mood != null)
                billDoer.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.ReplacedPartBleedingHeart, pawn);
        }
    }
}
