﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;
using UnityEngine;

namespace Psychology
{
    class LordJob_HangOut : LordJob
    {
        public LordJob_HangOut()
        { }

        public LordJob_HangOut(Pawn initiator, Pawn recipient)
        {
            this.initiator = initiator;
            this.recipient = recipient;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_HangOut lordToil_HangOut = new LordToil_HangOut(new Pawn[] { initiator, recipient });
            stateGraph.AddToil(lordToil_HangOut);
            LordToil_End lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);
            Transition transition = new Transition(lordToil_HangOut, lordToil_End);
            transition.AddTrigger(new Trigger_TickCondition(() => this.ShouldBeCalledOff()));
            transition.AddTrigger(new Trigger_TickCondition(() => this.initiator.health.summaryHealth.SummaryHealthPercent < 1f || this.recipient.health.summaryHealth.SummaryHealthPercent < 1f));
            transition.AddTrigger(new Trigger_TickCondition(() => this.initiator.Drafted || this.recipient.Drafted));
            transition.AddTrigger(new Trigger_TickCondition(() => this.initiator.Map == null || this.recipient.Map == null));
            transition.AddTrigger(new Trigger_PawnLost());
            stateGraph.AddTransition(transition);
            this.timeoutTrigger = new Trigger_TicksPassed(Rand.RangeInclusive(GenDate.TicksPerHour*3, GenDate.TicksPerHour*5));
            Transition transition2 = new Transition(lordToil_HangOut, lordToil_End);
            transition2.AddTrigger(this.timeoutTrigger);
            transition2.AddPreAction(new TransitionAction_Custom((Action)delegate
            {
                this.Finished();
            }));
            stateGraph.AddTransition(transition2);
            return stateGraph;
        }

        public void Finished()
        {
            this.initiator.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.HungOut, this.recipient);
            this.recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOfPsychology.HungOut, this.initiator);
        }
        
        public override void ExposeData()
        {
            Scribe_References.Look(ref this.initiator, "initiator");
            Scribe_References.Look(ref this.recipient, "recipient");
        }
        
        public override string GetReport()
        {
            return "LordReportHangingOut".Translate();
        }
        
        private bool ShouldBeCalledOff()
        {
            return !PartyUtility.AcceptableGameConditionsToContinueParty(base.Map) || this.initiator.GetTimeAssignment() == TimeAssignmentDefOf.Work || this.recipient.GetTimeAssignment() == TimeAssignmentDefOf.Work || this.initiator.needs.rest.CurLevel < 0.3f || this.recipient.needs.rest.CurLevel < 0.3f;
        }
        
        private Trigger_TicksPassed timeoutTrigger;
        public Pawn initiator;
        public Pawn recipient;
    }
}
