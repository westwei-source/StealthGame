﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Kill Enemy", menuName = "AI Goals/Kill Enemy")]
public class KillEnemy : Goal {

	public override int DetermineGoalPriority(NPC NPCAgent) {
		int tempPriority = m_defualtPriority;
		return tempPriority;
	}
}