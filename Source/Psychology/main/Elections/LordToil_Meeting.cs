﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;

namespace Psychology
{
    public class LordToil_Meeting : LordToil
    {
        public LordToil_Meeting(IntVec3 spot)
        {
            this.spot = spot;
        }

        public override void UpdateAllDuties()
        {
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                this.lord.ownedPawns[i].mindState.duty = new PawnDuty(DutyDefOfPsychology.Meeting, this.spot, -1f);
            }
        }

        [LogPerformance]
        public override void LordToilTick()
        {
            base.LordToilTick();
            LordJob_VisitMayor meeting = this.lord.LordJob as LordJob_VisitMayor;
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                if (this.lord.ownedPawns[i].Position == null || this.spot == null || this.lord.ownedPawns[i].Map == null || !PartyUtility.InPartyArea(this.lord.ownedPawns[i].Position, this.spot, this.lord.ownedPawns[i].Map))
                {
                    return;
                }
            }
            if (meeting != null)
            {
                meeting.ticksInSameRoom += 1;
                if(meeting.ticksInSameRoom % 200 == 0 && Rand.Value < 0.2f)
                {
                    meeting.mayor.skills.Learn(SkillDefOf.Social, 0.5f);
                    MoteMaker.MakeInteractionBubble(meeting.constituent, meeting.mayor, InteractionDefOf.DeepTalk.interactionMote, InteractionDefOf.DeepTalk.Symbol);
                }
            }
        }

        private IntVec3 spot;
    }
}
