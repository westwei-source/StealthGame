﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigation : MonoBehaviour
{
    private int m_maxLevelSize = 50;
    private int m_minYPos = -100;

    [Tooltip("How big a tile is in world units")]
    public Vector3 m_tileSize = new Vector3(2.0f, 1.0f, 2.0f);

    private int m_navNodeLayer = 0;

    //Caching of offsets for efficeincy
    private static Vector3 m_forwardOffset;
    private static Vector3 m_backwardOffset;
    private static Vector3 m_rightOffset;
    private static Vector3 m_leftOffset;

    //Storage of grid extents
    private int m_maxX = 0;
    private int m_minX = 0;
    private int m_maxY = 0;
    private int m_minY = 0;
    private int m_maxZ = 0;
    private int m_minZ = 0;


    public NavNode[,,] m_navGrid;
    public int m_navGridWidth;
    public int m_navGridDepth;
    public int m_navGridHeight;

    private void Awake()
    {
        //Setup stuff
        m_navNodeLayer = LayerMask.GetMask("NavNode");

        m_forwardOffset = new Vector3(0, 0, m_tileSize.z);
        m_backwardOffset = new Vector3(0, 0, -m_tileSize.z);
        m_rightOffset = new Vector3(m_tileSize.x, 0, 0);
        m_leftOffset = new Vector3(-m_tileSize.x, 0, 0);

        Vector3 orginatingPos = new Vector3(0.0f, m_minYPos, 0.0f);

        NavNode[,,] tempNavNodeGrid = new NavNode[m_maxLevelSize, m_maxLevelSize, m_maxLevelSize];
        Vector3Int gridCenter = new Vector3Int(m_maxLevelSize / 2, m_maxLevelSize / 2, m_maxLevelSize / 2);

        m_maxX = m_minX = gridCenter.x;
        m_maxY = m_minY = gridCenter.y;
        m_maxZ = m_minZ = gridCenter.z;


        List<NavNode> newNodes = GetNavNode(orginatingPos, gridCenter, tempNavNodeGrid);

        foreach (NavNode navNode in newNodes)
        {
            UpdateNeighbourNodes(navNode, tempNavNodeGrid);
        }

        //Create new grid clipping excess null filled sides
        m_navGridWidth = m_maxX - m_minX + 1;
        m_navGridHeight = m_maxY - m_minY + 1;
        m_navGridDepth = m_maxZ - m_minZ + 1;

        m_navGrid = new NavNode[m_navGridWidth, m_navGridHeight, m_navGridDepth];

        for (int i = 0; i < m_navGridWidth; i++)
        {
            for (int j = 0; j < m_navGridHeight; j++)
            {
                for (int k = 0; k < m_navGridDepth; k++)
                {
                    m_navGrid[i, j, k] = tempNavNodeGrid[m_minX + i, m_minY + j, m_minZ + k];
                    if (m_navGrid[i, j, k] != null)
                        m_navGrid[i, j, k].m_gridPos = new Vector3Int(i, j, k);
                }
            }
        }

        //Setup Node neighbours
        for (int i = 0; i < m_navGridWidth; i++)
        {
            for (int j = 0; j < m_navGridHeight; j++)
            {
                for (int k = 0; k < m_navGridDepth; k++)
                {
                    BuildNodeBranches(new Vector3Int(i, j, k));
                }
            }
        }
    }
    
    private List<NavNode> GetNavNode(Vector3 pos, Vector3Int gridPos, NavNode[,,] navNodeGrid)
    {
        List<NavNode> newNodes = new List<NavNode>();
        if (!IsGridColumnEmpty(gridPos.x, gridPos.z, navNodeGrid))//Case of a node already exisitng
            return newNodes;

        RaycastHit[] hits = Physics.RaycastAll(pos, Vector3.up, Mathf.Infinity, m_navNodeLayer);
        if (hits.Length != 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                NavNode navNode = hits[i].collider.GetComponent<NavNode>();

                if (navNode != null)
                {
                    NavNode originNode = navNodeGrid[m_maxLevelSize / 2, m_maxLevelSize / 2, m_maxLevelSize / 2];
                    if (originNode != null)//Get offset height
                    {
                        int heightDifference = Mathf.FloorToInt(navNode.transform.position.y - originNode.transform.position.y);

                        Vector3Int tempGridPos = gridPos;
                        tempGridPos.y = originNode.m_gridPos.y + heightDifference;
                        navNode.m_gridPos = tempGridPos;
                    }
                    else
                    {
                        navNode.m_gridPos = gridPos;
                    }

                    navNodeGrid[navNode.m_gridPos.x, navNode.m_gridPos.y, navNode.m_gridPos.z] = navNode;

                    m_minX = navNode.m_gridPos.x < m_minX ? navNode.m_gridPos.x : m_minX;
                    m_maxX = navNode.m_gridPos.x > m_maxX ? navNode.m_gridPos.x : m_maxX;
                    m_minY = navNode.m_gridPos.y < m_minY ? navNode.m_gridPos.y : m_minY;
                    m_maxY = navNode.m_gridPos.y > m_maxY ? navNode.m_gridPos.y : m_maxY;
                    m_minZ = navNode.m_gridPos.z < m_minZ ? navNode.m_gridPos.z : m_minZ;
                    m_maxZ = navNode.m_gridPos.z > m_maxZ ? navNode.m_gridPos.z : m_maxZ;

                    newNodes.Add(navNode);
                }
            }
        }
        return newNodes;
    }

    private bool IsGridColumnEmpty(int x, int z, NavNode[,,] navNodeGrid)
    {
        for (int i = 0; i < m_maxLevelSize; i++)
        {
            if (navNodeGrid[x, i, z] != null)
                return false;
        }
        return true;
    }

    //----------------
    //Creation of Nav Grid
    //----------------
    private void UpdateNeighbourNodes(NavNode currentNode, NavNode[,,] navNodeGrid)
    {
        Vector3 initialRaycastPos = currentNode.transform.position;
        initialRaycastPos.y = m_minYPos;

        //Forward
        List<NavNode> newNodes = GetNavNode(initialRaycastPos + m_forwardOffset, currentNode.m_gridPos + new Vector3Int(0, 0, 1), navNodeGrid);

        //Backward
        newNodes.AddRange(GetNavNode(initialRaycastPos + m_backwardOffset, currentNode.m_gridPos + new Vector3Int(0, 0, -1), navNodeGrid));

        //Right
        newNodes.AddRange(GetNavNode(initialRaycastPos + m_rightOffset, currentNode.m_gridPos + Vector3Int.right, navNodeGrid));

        //Left
        newNodes.AddRange(GetNavNode(initialRaycastPos + m_leftOffset, currentNode.m_gridPos + Vector3Int.left, navNodeGrid));

        foreach (NavNode navNode in newNodes)
        {
            UpdateNeighbourNodes(navNode, navNodeGrid);
        }
    }

    private void BuildNodeBranches(Vector3Int currentGridPos)
    {
        NavNode currentNode = m_navGrid[currentGridPos.x, currentGridPos.y, currentGridPos.z];

        if(currentNode!=null)
        {
            //Forward
            if (currentGridPos.z + 1 < m_navGridDepth)
            {
                NavNode adjacentNode = GetAdjacentNode(currentGridPos + new Vector3Int(0, 0, 1));
                if(adjacentNode!= null)
                    currentNode.m_adjacentNodes.Add(adjacentNode);
            }
            //Backward
            if (currentGridPos.z - 1 >= 0)
            {
                NavNode adjacentNode = GetAdjacentNode(currentGridPos + new Vector3Int(0, 0, -1));
                if (adjacentNode != null)
                    currentNode.m_adjacentNodes.Add(adjacentNode);
            }
            //Right
            if (currentGridPos.x + 1 < m_navGridWidth)
            {
                NavNode adjacentNode = GetAdjacentNode(currentGridPos + new Vector3Int(1, 0, 0));
                if (adjacentNode != null)
                    currentNode.m_adjacentNodes.Add(adjacentNode);
            }
            //Left
            if (currentGridPos.x - 1 >= 0)
            {
                NavNode adjacentNode = GetAdjacentNode(currentGridPos + new Vector3Int(-1, 0, 0));
                if (adjacentNode != null)
                    currentNode.m_adjacentNodes.Add(adjacentNode);
            }
        }

        m_navGrid[currentGridPos.x, currentGridPos.y, currentGridPos.z] = currentNode;
    }

    private NavNode GetAdjacentNode(Vector3Int offsetGridPos)
    {
        //Mid
        if (m_navGrid[offsetGridPos.x, offsetGridPos.y, offsetGridPos.z] != null)
            return m_navGrid[offsetGridPos.x, offsetGridPos.y, offsetGridPos.z];
        //Top
        if (offsetGridPos.y + 1 < m_navGridHeight && m_navGrid[offsetGridPos.x, offsetGridPos.y+1, offsetGridPos.z] != null)
            return m_navGrid[offsetGridPos.x, offsetGridPos.y+1, offsetGridPos.z];
        //Lower
        if (offsetGridPos.y - 1 >= 0 && m_navGrid[offsetGridPos.x, offsetGridPos.y - 1, offsetGridPos.z] != null)
            return m_navGrid[offsetGridPos.x, offsetGridPos.y - 1, offsetGridPos.z];

        return null;
    }

    //----------------
    //End of Nav Grid Creation
    //----------------

    //----------------
    //A* stuff
    //----------------
    public List<NavNode> GetNavPath(NavNode startingNode, NavNode goalNode)
    {
        if (startingNode == goalNode)//Already at position
            return null;

        List<NavNode> openNodes = new List<NavNode>();
        List<NavNode> closedNodes = new List<NavNode>();

        //Get starting node
        openNodes.Add(startingNode);

        NavNode currentNode = startingNode;
        currentNode.m_gScore = 0;//Reset starting node

        //Loop till no more options
        while (openNodes.Count > 0)
        {
            //Break early when at end
            if (currentNode == goalNode)
                return GetPath(currentNode, startingNode);

            AddNextNodes(currentNode, goalNode, openNodes, closedNodes);

            currentNode = GetLowestFScore(openNodes);
        }
        return null;
    }

    public List<NavNode> GetNavPath(Vector3Int startingIndexPos, NavNode goalNode)
    {
        NavNode startingNode = m_navGrid[startingIndexPos.x, startingIndexPos.y, startingIndexPos.z];
        return GetNavPath(startingNode, goalNode);
    }

    private void AddNextNodes(NavNode currentNode, NavNode goalNode, List<NavNode> openNodes, List<NavNode> closedNodes)
    {
        openNodes.Remove(currentNode);
        closedNodes.Add(currentNode);

        foreach (NavNode nextNode in currentNode.m_adjacentNodes)
        {
            if (!openNodes.Contains(nextNode) && !closedNodes.Contains(nextNode))
            {
                openNodes.Add(nextNode);
                nextNode.Setup(openNodes, closedNodes, goalNode);
            }
        }
    }

    private NavNode GetLowestFScore(List<NavNode> openNodes)
    {
        float fScore = Mathf.Infinity;
        NavNode highestFNode = null;
        foreach (NavNode node in openNodes)
        {
            if (node.m_fScore < fScore)
            {
                highestFNode = node;
                fScore = node.m_fScore;
            }
        }
        return highestFNode;
    }

    private List<NavNode> GetPath(NavNode currentNode, NavNode startingNode)
    {
        List<NavNode> path = new List<NavNode>();
        while(currentNode != startingNode)
        {
            path.Insert(0, currentNode);
            currentNode = currentNode.m_previousNode;
        }
        path.Add(currentNode);
        return path;
    }
}
