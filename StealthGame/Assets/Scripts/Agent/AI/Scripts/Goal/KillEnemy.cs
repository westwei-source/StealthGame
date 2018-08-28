﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Kill Enemy", menuName = "AI Goals/Kill Enemy")]
public class KillEnemy : Goal {

	public override void DetermineGoalPriority(NPC NPCAgent)
    {
        m_goalPriority = m_defualtPriority;
    }
}