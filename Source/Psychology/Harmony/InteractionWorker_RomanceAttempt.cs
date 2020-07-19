﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;

namespace Psychology.Harmony
{
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "BreakLoverAndFianceRelations")]
    public static class InteractionWorker_RomanceAttempt_BreakRelationsPatch
    {
        [LogPerformance]
        [HarmonyPrefix]
        public static bool BreakRelations(Pawn pawn, ref List<Pawn> oldLoversAndFiances)
        {
            oldLoversAndFiances = new List<Pawn>();
            while (true)
            {
                Pawn firstDirectRelationPawn = pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, null);
                if (firstDirectRelationPawn != null && (!firstDirectRelationPawn.story.traits.HasTrait(TraitDefOfPsychology.Polygamous) || !pawn.story.traits.HasTrait(TraitDefOfPsychology.Polygamous)))
                {
                    pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, firstDirectRelationPawn);
                    Pawn recipient = firstDirectRelationPawn;
                    if (PsycheHelper.PsychologyEnabled(pawn) && PsycheHelper.PsychologyEnabled(recipient))
                    {
                        BreakupHelperMethods.AddExLover(pawn, recipient);
                        BreakupHelperMethods.AddExLover(recipient, pawn);
                        BreakupHelperMethods.AddBrokeUpOpinion(recipient, pawn);
                        BreakupHelperMethods.AddBrokeUpMood(recipient, pawn);
                        BreakupHelperMethods.AddBrokeUpMood(pawn, recipient);
                    }
                    else
                    {
                        pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, firstDirectRelationPawn);
                    }
                    oldLoversAndFiances.Add(firstDirectRelationPawn);
                }
                else
                {
                    Pawn firstDirectRelationPawn2 = pawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, null);
                    if (firstDirectRelationPawn2 == null)
                    {
                        break;
                    }
                    else if (!firstDirectRelationPawn2.story.traits.HasTrait(TraitDefOfPsychology.Polygamous) || !pawn.story.traits.HasTrait(TraitDefOfPsychology.Polygamous))
                    {
                        pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Fiance, firstDirectRelationPawn2);
                        Pawn recipient2 = firstDirectRelationPawn2;
                        if (PsycheHelper.PsychologyEnabled(pawn) && PsycheHelper.PsychologyEnabled(recipient2))
                        {
                            BreakupHelperMethods.AddExLover(pawn, recipient2);
                            BreakupHelperMethods.AddExLover(recipient2, pawn);
                            BreakupHelperMethods.AddBrokeUpOpinion(recipient2, pawn);
                            BreakupHelperMethods.AddBrokeUpMood(recipient2, pawn);
                            BreakupHelperMethods.AddBrokeUpMood(pawn, recipient2);
                        }
                        else
                        {
                            pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, firstDirectRelationPawn2);
                        }
                        oldLoversAndFiances.Add(firstDirectRelationPawn2);
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight))]
    public static class InteractionWorker_RomanceAttempt_SelectionWeightPatch
    {
        [LogPerformance]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        public static void PsychologyException(ref float __result, Pawn initiator, Pawn recipient)
        {
            //Don't hit on people in mental breaks... unless you're really freaky.
            if (recipient.InMentalState && PsycheHelper.PsychologyEnabled(initiator) && PsycheHelper.Comp(initiator).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Experimental) < 0.8f)
            {
                __result = 0f;
                return;
            }
            //Pawns won't hit on their spouses.
            if (LovePartnerRelationUtility.LovePartnerRelationExists(initiator, recipient))
            {
                __result = 0f;
                return;
            }
            //Codependents won't romance anyone if they are in a relationship
            if (LovePartnerRelationUtility.HasAnyLovePartner(initiator) && initiator.story.traits.HasTrait(TraitDefOfPsychology.Codependent))
            {
                __result = 0f;
                return;
            }
            //Only lechers will romance someone that has less than +5 opinion of them
            if (recipient.relations.OpinionOf(initiator) < 5 && !initiator.story.traits.HasTrait(TraitDefOfPsychology.Lecher))
            {
                __result = 0f;
                return;
            }
            float attractiveness = initiator.relations.SecondaryRomanceChanceFactor(recipient);
            int opinion = initiator.relations.OpinionOf(recipient);
            float romanceChance = 1.15f;
            if (!PsycheHelper.PsychologyEnabled(initiator))
            {
                //Vanilla: Straight women are 15% as likely to romance anyone.
                romanceChance = (!initiator.story.traits.HasTrait(TraitDefOf.Gay)) ? ((initiator.gender != Gender.Female) ? romanceChance : romanceChance * 0.15f) : romanceChance;
            }
            else
            {
                //Psychology: A pawn's likelihood to romance is based on how Aggressive and Romantic they are.
                float personalityFactor = Mathf.Pow(20f, PsycheHelper.Comp(initiator).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Aggressive)) * Mathf.Pow(12f, (1f - PsycheHelper.Comp(initiator).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Romantic)));
                romanceChance = personalityFactor * 0.02f;
            }
            //A pawn with +50 or more opinion of their lover will not hit on other pawns unless they are lecherous or polygamous (and their lover is also polygamous).
            float existingLovePartnerFactor = 1f;
            Pawn pawn = LovePartnerRelationUtility.ExistingMostLikedLovePartner(initiator, false);
            if (pawn != null && !initiator.story.traits.HasTrait(TraitDefOfPsychology.Lecher) && (!initiator.story.traits.HasTrait(TraitDefOfPsychology.Polygamous) && !pawn.story.traits.HasTrait(TraitDefOfPsychology.Polygamous)))
            {
                float value = (float)initiator.relations.OpinionOf(pawn);
                existingLovePartnerFactor = Mathf.InverseLerp(50f, -50f, value);
            }
            float attractivenessFactor = Mathf.InverseLerp(0.25f, 1f, attractiveness);
            float opinionFactor = Mathf.InverseLerp(-5f, 100f, (float)opinion)*2f;
            //People who have hit on someone in the past and been rejected because of their sexuality will rarely attempt to hit on them again.
            float knownSexualityFactor = (PsycheHelper.PsychologyEnabled(initiator) && PsychologyBase.ActivateKinsey() && PsycheHelper.Comp(initiator).Sexuality.IncompatibleSexualityKnown(recipient) && !initiator.story.traits.HasTrait(TraitDefOfPsychology.Lecher)) ? 0.05f : (PsycheHelper.PsychologyEnabled(initiator) ? (initiator.gender == recipient.gender ? (initiator.story.traits.HasTrait(TraitDefOf.Gay) && recipient.story.traits.HasTrait(TraitDefOf.Gay) ? 1f : 0.15f) : (!initiator.story.traits.HasTrait(TraitDefOf.Gay) && !recipient.story.traits.HasTrait(TraitDefOf.Gay) ? 1f : 0.15f)) : 1f);
            //Only lechers will try to romance someone in a stable relationship.
            float recipientLovePartnerFactor = 1f;
            Pawn pawn2 = LovePartnerRelationUtility.ExistingMostLikedLovePartner(recipient, false);
            if (pawn2 != null && !initiator.story.traits.HasTrait(TraitDefOfPsychology.Lecher))
            {
                int value = recipient.relations.OpinionOf(pawn2);
                recipientLovePartnerFactor = Mathf.InverseLerp(5f, -100f, (float)value);
            }
            __result = romanceChance * existingLovePartnerFactor * attractivenessFactor * opinionFactor * knownSexualityFactor * recipientLovePartnerFactor;
            return;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.Interacted))]
    public static class InteractionWorker_RomanceAttempt_InteractedLearnSexualityPatch
    {
        [LogPerformance]
        [HarmonyPriority(Priority.High)]
        [HarmonyPrefix]
        public static bool LearnSexuality(Pawn initiator, Pawn recipient)
        {
            if (PsycheHelper.PsychologyEnabled(initiator) && PsycheHelper.PsychologyEnabled(recipient) && PsychologyBase.ActivateKinsey())
            {
                PsycheHelper.Comp(initiator).Sexuality.LearnSexuality(recipient);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "Interacted")]
    public static class InteractionWorker_RomanceAttempt_InteractedHandleThoughtsPatch
    {
        [LogPerformance]
        [HarmonyPostfix]
        public static void HandleNewThoughts(InteractionWorker_RomanceAttempt __instance, Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, string letterText, string letterLabel, LetterDef letterDef)
        {
            if (extraSentencePacks.Contains(RulePackDefOf.Sentence_RomanceAttemptAccepted))
            {
                foreach (ThoughtDef d in (from tgt in initiator.needs.mood.thoughts.memories.Memories
                                          where tgt.def.defName.Contains("BrokeUpWithMe")
                                          select tgt.def))
                {
                    initiator.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(d, recipient);
                }
                foreach (ThoughtDef d in (from tgt in recipient.needs.mood.thoughts.memories.Memories
                                          where tgt.def.defName.Contains("BrokeUpWithMe")
                                          select tgt.def))
                {
                    recipient.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(d, initiator);
                }
                initiator.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOfPsychology.BrokeUpWithMeCodependent, recipient);
                recipient.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOfPsychology.BrokeUpWithMeCodependent, initiator);
            }
            else if(extraSentencePacks.Contains(RulePackDefOf.Sentence_RomanceAttemptRejected))
            {
                if (initiator.story.traits.HasTrait(TraitDefOfPsychology.Lecher))
                {
                    initiator.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.RebuffedMyRomanceAttemptLecher, recipient);
                }
            }
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.SuccessChance))]
    public static class InteractionWorker_RomanceAttempt_SuccessChancePatch
    {
        [LogPerformance]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        public static void NewSuccessChance(ref float __result, Pawn initiator, Pawn recipient)
        {
            /* Throw out the result and replace it with our own formula. */
            float successChance = 0.6f;
            if (PsycheHelper.PsychologyEnabled(recipient))
            {
                //The recipient is less likely to accept the more romantic they are, which means they will need to like the person more.
                successChance = 0.25f + (1f - PsycheHelper.Comp(recipient).Psyche.GetPersonalityRating(PersonalityNodeDefOf.Romantic));
            }
            successChance *= recipient.relations.SecondaryRomanceChanceFactor(initiator);
            successChance *= 2f * Mathf.InverseLerp(-5f, 100f, (float)recipient.relations.OpinionOf(initiator));
            float existingLovePartnerFactor = 1f;
            if (!recipient.story.traits.HasTrait(TraitDefOfPsychology.Polygamous))
            {
                Pawn pawn = null;
                if (recipient.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, (Pawn x) => !x.Dead) != null)
                {
                    pawn = recipient.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover, null);
                    existingLovePartnerFactor = (recipient.story.traits.HasTrait(TraitDefOfPsychology.Codependent)) ? 0f : 0.6f;
                }
                else if (recipient.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, (Pawn x) => !x.Dead) != null)
                {
                    pawn = recipient.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Fiance, null);
                    existingLovePartnerFactor = (recipient.story.traits.HasTrait(TraitDefOfPsychology.Codependent)) ? 0f : 0.1f;
                }
                else if (recipient.GetSpouse() != null && !recipient.GetSpouse().Dead)
                {
                    pawn = recipient.GetSpouse();
                    existingLovePartnerFactor = (recipient.story.traits.HasTrait(TraitDefOfPsychology.Codependent)) ? 0f : 0.3f;
                }
                if (pawn != null)
                {
                    existingLovePartnerFactor *= Mathf.InverseLerp(100f, 0f, (float)recipient.relations.OpinionOf(pawn));
                    existingLovePartnerFactor *= Mathf.Clamp01(1f - recipient.relations.SecondaryRomanceChanceFactor(pawn));
                }
            }
            if (recipient.story.traits.HasTrait(TraitDefOfPsychology.Lecher))
            {
                existingLovePartnerFactor = 1.916f;
            }
            successChance *= existingLovePartnerFactor;
            __result = Mathf.Clamp01(successChance);
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "TryAddCheaterThought")]
    public static class InteractionWorker_RomanceAttempt_CheaterThoughtPatch
    {
        [LogPerformance]
        [HarmonyPostfix]
        public static void AddCodependentThought(Pawn pawn, Pawn cheater)
        {
            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.CheatedOnMeCodependent, cheater);
        }
    }
}
