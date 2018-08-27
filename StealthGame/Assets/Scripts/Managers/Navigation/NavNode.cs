﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavNode : MonoBehaviour
{
    [Header("Tile UI")]
    public GameObject m_selectableUI;
    public GameObject m_selectedUI;
    public Sprite m_selectedSprite;
    public Sprite m_attackSprite;
    public Sprite m_defaultSprite;
    public SpriteRenderer m_spriteRenderer;

    public static float m_wallHideSelectionDeadZone = 0.5f;

    [System.Serializable]
    public struct WallHideIndicators
    {
        public SpriteRenderer m_wallHideSprite;
        public NODE_TYPE m_wallHideType;
        public bool m_selected;
    }

    [SerializeField]
    public WallHideIndicators[] m_wallHideIndicators = new WallHideIndicators[4];

    [HideInInspector]
    public Color spriteColor;
    
    //auto assigned
    [Space]
    [Space]
    //BFS vars
    public NavNode m_BFSPreviousNode = null;
    public int m_BFSDistance = 0;

    Renderer myRenderer;

    public enum NODE_STATE {SELECTED, SELECTABLE, UNSELECTED}
    public enum NODE_TYPE {NONE, WALKABLE, OBSTRUCTED, HIGH_OBSTACLE, LOW_OBSTACLE}

    public Agent m_obstructingAgent = null;

    public NODE_STATE m_nodeState = NODE_STATE.UNSELECTED;
    public NODE_TYPE m_nodeType = NODE_TYPE.NONE;

    public Vector3 m_nodeTop = Vector3.zero;
    public Vector3Int m_gridPos = Vector3Int.zero;

    public List<NavNode> m_adjacentNodes = new List<NavNode>();

    public float m_gScore, m_hScore, m_fScore = 0;

    public NavNode m_previousNode = null;

    void Start()
    {
        myRenderer = GetComponent<Renderer>();
        m_spriteRenderer = m_selectableUI.GetComponent<SpriteRenderer>();
        spriteColor = m_spriteRenderer.color;
        Vector3 colliderExtents = GetComponent<BoxCollider>().size;
        m_nodeTop = transform.position + Vector3.up * colliderExtents.y * transform.lossyScale.y;
    }

    public void Setup(List<NavNode> openNodes, List<NavNode> closedNodes, NavNode goalNode)
    {
        NavNode previousNode = null;
        float previousNodeCost = Mathf.Infinity;

        foreach (NavNode navNode in m_adjacentNodes)
        {
            if(navNode.m_gScore < previousNodeCost && (openNodes.Contains(navNode) || closedNodes.Contains(navNode)))
            {
                previousNode = navNode;
                previousNodeCost = navNode.m_gScore;
            }
        }

        m_previousNode = previousNode;
        m_gScore = previousNodeCost + 1;

        m_hScore = Vector3.Distance(transform.position, goalNode.transform.position);

        m_fScore = m_hScore + m_gScore;
    }

    public void UpdateNavNodeState(NODE_STATE nodeState, Agent agent)
    {
        m_nodeState = nodeState;

        switch (nodeState) {
            case NODE_STATE.SELECTABLE:

                if(m_nodeType == NODE_TYPE.WALKABLE)
                {
                    m_selectableUI.SetActive(true);
                    m_selectedUI.SetActive(false);
                    m_spriteRenderer.sprite = m_defaultSprite;

                    ToggleWallHideIndicators(false);
                }
                else if (m_obstructingAgent != null && m_obstructingAgent.m_team != agent.m_team)
                {
                    m_selectableUI.SetActive(true);
                    m_selectedUI.SetActive(false);
                    m_spriteRenderer.sprite = m_attackSprite;
                }

                break;
            case NODE_STATE.SELECTED:

                if (m_nodeType == NODE_TYPE.WALKABLE)
                {
                    m_selectedUI.SetActive(true);
                    m_spriteRenderer.sprite = m_selectedSprite;

                    ToggleWallHideIndicators(true);
                }
                else if (m_obstructingAgent != null && m_obstructingAgent.m_team != agent.m_team)
                {
                    m_selectableUI.SetActive(true);
                    m_selectedUI.SetActive(false);
                    m_spriteRenderer.sprite = m_attackSprite;
                }

                break;
            case NODE_STATE.UNSELECTED:
                m_selectableUI.SetActive(false);
                m_selectedUI.SetActive(false);
                m_spriteRenderer.sprite = m_defaultSprite;
                break;
        }              
    }

    public void SetupWallHideIndicators(Navigation navigation)
    {
        m_wallHideIndicators[0].m_wallHideType = navigation.GetAdjacentNodeType(m_gridPos, new Vector3Int(0, 0, 1));
        m_wallHideIndicators[1].m_wallHideType = navigation.GetAdjacentNodeType(m_gridPos, new Vector3Int(1, 0, 0));
        m_wallHideIndicators[2].m_wallHideType = navigation.GetAdjacentNodeType(m_gridPos, new Vector3Int(0, 0, -1));
        m_wallHideIndicators[3].m_wallHideType = navigation.GetAdjacentNodeType(m_gridPos, new Vector3Int(-1, 0, 0));
    }

    public void UpdateWallIndicators()
    {
        Color halfAlpha = new Color(1, 1, 1, 0.2f);
        for (int i = 0; i < 4; i++)
        {
            m_wallHideIndicators[i].m_wallHideSprite.color = halfAlpha;
        }

        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        RaycastHit hit; 

        if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out hit, Mathf.Infinity, LayerManager.m_navNodeLayer))
        {
            Vector3 relativeMousePos = hit.point - transform.position;

            if(Mathf.Abs(relativeMousePos.z) > Mathf.Abs(relativeMousePos.x))//North south Indicator
            {
                if(relativeMousePos.z > m_wallHideSelectionDeadZone)//North Indicator
                {
                    if (m_wallHideIndicators[0].m_wallHideType == NODE_TYPE.LOW_OBSTACLE || m_wallHideIndicators[0].m_wallHideType == NODE_TYPE.HIGH_OBSTACLE)
                    {
                        m_wallHideIndicators[0].m_wallHideSprite.color = new Color(1, 1, 1, 1);
                        m_wallHideIndicators[0].m_selected = true;
                    }
                }
                else if(relativeMousePos.z < -m_wallHideSelectionDeadZone) //South indicator
                {
                    if (m_wallHideIndicators[2].m_wallHideType == NODE_TYPE.LOW_OBSTACLE || m_wallHideIndicators[2].m_wallHideType == NODE_TYPE.HIGH_OBSTACLE)
                    {
                        m_wallHideIndicators[2].m_wallHideSprite.color = new Color(1, 1, 1, 1);
                        m_wallHideIndicators[2].m_selected = true;
                    }
                }
            }
            else
            {
                if (relativeMousePos.x > m_wallHideSelectionDeadZone)//East Indicator
                {
                    if (m_wallHideIndicators[1].m_wallHideType == NODE_TYPE.LOW_OBSTACLE || m_wallHideIndicators[1].m_wallHideType == NODE_TYPE.HIGH_OBSTACLE)
                    {
                        m_wallHideIndicators[1].m_wallHideSprite.color = new Color(1, 1, 1, 1);
                        m_wallHideIndicators[1].m_selected = true;
                    }
                }
                else if (relativeMousePos.x < -m_wallHideSelectionDeadZone) //West indicator
                {
                    if (m_wallHideIndicators[3].m_wallHideType == NODE_TYPE.LOW_OBSTACLE || m_wallHideIndicators[3].m_wallHideType == NODE_TYPE.HIGH_OBSTACLE)
                    {
                        m_wallHideIndicators[3].m_wallHideSprite.color = new Color(1, 1, 1, 1);
                        m_wallHideIndicators[3].m_selected = true;
                    }
                }
            }
        }
    }

    public void ToggleWallHideIndicators(bool toggleVal)
    {
        for (int i = 0; i < 4; i++) //Add wall hide icons
        {
            m_wallHideIndicators[i].m_selected = false;

            if (m_wallHideIndicators[i].m_wallHideType == NODE_TYPE.LOW_OBSTACLE || m_wallHideIndicators[i].m_wallHideType == NODE_TYPE.HIGH_OBSTACLE)
                m_wallHideIndicators[i].m_wallHideSprite.enabled = toggleVal;
            else
                m_wallHideIndicators[i].m_wallHideSprite.enabled = false;
        }
    }

    public AnimationManager.FACING_DIR GetWallHideDir()
    {
        for (int i = 0; i < 4; i++)
        {
            if(m_wallHideIndicators[i].m_selected == true)
            {
                return (AnimationManager.FACING_DIR)i;//Casting 'i' to direction
            }
        }
        return AnimationManager.FACING_DIR.NONE;
    }
}
