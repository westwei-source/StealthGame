﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : Agent {

    [Space]
    [Space]
    public Sprite portrait;

    static CameraController m_cameraController = null;

    private PlayerUI m_playerUI = null;
    private PlayerActions m_playerActions = null;
    private AgentAnimationController m_agentAnimationController = null;

    protected override void Start()
    {
        base.Start();
        m_cameraController = GameObject.FindGameObjectWithTag("CamPivot").GetComponent<CameraController>();

        m_playerUI = GetComponent<PlayerUI>();
        m_playerActions = GetComponent<PlayerActions>();
        m_agentAnimationController = GetComponent<AgentAnimationController>();
    }

    //Start of turn, only runs once per turn
    public override void AgentTurnInit()
    {
        base.AgentTurnInit();
        m_playerUI.InitUI();
    }

    //Runs every time a agent is selected, this can be at end of an action is completed
    public override void AgentSelected()
    {
        m_playerActions.InitActions();
        m_playerUI.StartUI();
        m_cameraController.Focus(transform);
    }

    //Constant update while agent is selected
    public override void AgentTurnUpdate()
    {
        m_playerActions.UpdateActions();
        m_playerUI.UpdateUI();

        if (m_currentActionPoints <= 0 && m_agentAnimationController.m_animationSteps.Count == 0)
        {
            m_playerActions.ActionEnd();
            m_turnManager.EndUnitTurn(this);
            AgentTurnEnd();
            return;
        }
    }

    //Runs when agent is removed from team list, end of turn
    public override void AgentTurnEnd()
    {
        base.AgentTurnEnd();
    }
}
