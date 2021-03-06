﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    [SerializeField] private List<EnvironmentTile> AccessibleTiles;
    [SerializeField] private List<EnvironmentTile> InaccessibleTiles;

    [SerializeField] private GameObject pedistalPrefab;
    [SerializeField] private GameObject keyPrefab;
    
    [Range(15,50)]
    [SerializeField] private int SizeX;
    [Range(15, 50)]
    [SerializeField] private int SizeY;
    private Vector2Int Size;

    [SerializeField] private float AccessiblePercentage;
    [SerializeField] private float DoorChance;

    private bool TileRotaionFinish = true;

    private EnvironmentTile[][] mMap;
    private List<EnvironmentTile> mAll;
    private List<EnvironmentTile> mToBeTested;
    private List<EnvironmentTile> mLastSolution;

    private readonly Vector3 NodeSize = Vector3.one * 9.0f; 
    private const float TileSize = 10.0f;
    private const float TileHeight = 2.5f;

    private Vector3 directionLeft = new Vector3(10, 0, 0);
    private Vector3 directionRight = new Vector3(-10, 0, 0);
    private Vector3 directionForward = new Vector3(0, 0, -10);
    private Vector3 directionBack = new Vector3(0, 0, 10);

    private int[] puzzleNumbers;
    private int puzzleNumbersCount = 4;

    private int DoorCount = 0;
    private int MaxDoorCount = 0;

    public EnvironmentTile StartTile { get; private set; }

    private void Awake()
    {
       
        mAll = new List<EnvironmentTile>();
        mToBeTested = new List<EnvironmentTile>();

        Size = new Vector2Int(SizeX, SizeY);
       
    }

    private void Start()
    {
        puzzleNumbers = new int[4];
        MaxDoorCount = PlayerPrefs.GetInt("Challenge");
        
        for (int i = 0; i < puzzleNumbersCount; i++)
        {
            puzzleNumbers[i] = Random.Range(0, 10);
            PlayerPrefs.SetInt(i.ToString(), puzzleNumbers[i]);
           // Debug.LogError(i + " : " + puzzleNumbers[i]);
        }
    }

    private void GetMaxDoorCount()
    {
        int challenge = PlayerPrefs.GetInt("Challenge");

        switch (challenge)
        {
            case 0:
                MaxDoorCount = 5;
                break;
            case 1:
                MaxDoorCount = 3;
                break;
            case 2:
                MaxDoorCount = 1;
                break;
        }
    }

    private void SetAllDoors()
    {
        
        EnvironmentTile tempTile = GetRandomTile();
           
        if (tempTile.IsAccessible)
        {
            SetAllDoors();
        }
        else
        {
            tempTile.SetDoorOn();
           // Debug.LogError(tempTile.name);
            tempTile.SetTextVisibilty(false);
            DoorCount++;

            if (!(MaxDoorCount - DoorCount < 1)) SetAllDoors();


        }            
        
    }

    private void SetAllPedistals()
    {
        EnvironmentTile tempTile;
        for (int i = 0; i < puzzleNumbersCount; i++)
        {
             tempTile = GetRandomTile();
            //Debug.LogError(tempTile.name);

            GameObject obj = Instantiate(pedistalPrefab, tempTile.transform);
            TMPro.TextMeshPro text = obj.GetComponentInChildren<TMPro.TextMeshPro>();
            text.outlineWidth = 0.1f;
            text.text = puzzleNumbers[i].ToString();
        }

        tempTile = GetRandomTile();
       // Debug.LogError(tempTile.name);
        Instantiate(keyPrefab, tempTile.transform);

    }

    private void OnDrawGizmos()
    {
        // Draw the environment nodes and connections if we have them
        if (mMap != null)
        {
            for (int x = 0; x < Size.x; ++x)
            {
                for (int y = 0; y < Size.y; ++y)
                {
                    if (mMap[x][y].Connections != null)
                    {
                        for (int n = 0; n < mMap[x][y].Connections.Count; ++n)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawLine(mMap[x][y].Position, mMap[x][y].Connections[n].Position);
                        }
                    }

                    // Use different colours to represent the state of the nodes
                    Color c = Color.white;
                    if ( !mMap[x][y].IsAccessible )
                    {
                        c = Color.red;
                    }
                    else
                    {
                        if(mLastSolution != null && mLastSolution.Contains( mMap[x][y] ))
                        {
                            c = Color.green;
                        }
                        else if (mMap[x][y].Visited)
                        {
                            c = Color.yellow;
                        }
                    }

                    Gizmos.color = c;
                    Gizmos.DrawWireCube(mMap[x][y].Position, NodeSize);
                }
            }
        }
    }

    private void Generate()
    {
        // Setup the map of the environment tiles according to the specified width and height
        // Generate tiles from the list of accessible and inaccessible prefabs using a random
        // and the specified accessible percentage
        mMap = new EnvironmentTile[Size.x][];

        int halfWidth = Size.x / 2;
        int halfHeight = Size.y / 2;
        Vector3 position = new Vector3( -(halfWidth * TileSize), 0.0f, -(halfHeight * TileSize) );
        bool start = true;

        for ( int x = 0; x < Size.x; ++x)
        {
            mMap[x] = new EnvironmentTile[Size.y];
            for ( int y = 0; y < Size.y; ++y)
            {
                bool isAccessible = start || Random.value < AccessiblePercentage;
                List<EnvironmentTile> tiles = isAccessible ? AccessibleTiles : InaccessibleTiles;
                EnvironmentTile prefab = tiles[Random.Range(0, tiles.Count)];
                EnvironmentTile tile = Instantiate(prefab, position, Quaternion.identity, transform);
                tile.Position = new Vector3( position.x + (TileSize / 2), TileHeight, position.z + (TileSize / 2));
                tile.IsAccessible = isAccessible;

                if (!isAccessible)
                {
                    //if (Random.value < DoorChance) tile.SetDoorOn();
                    //rotate from center of tile
                    tile.transform.RotateAround(tile.GetRotationBlockPosition(), Vector3.up, GetRandomRotation());
                   // Debug.LogError(tile.transform.eulerAngles.y);
                    //tile.transform.rotation = Quaternion.Euler(new Vector3(0,(int)tile.transform.eulerAngles.y,0));
                }

                tile.gameObject.name = string.Format("Tile({0},{1})", x, y);
                mMap[x][y] = tile;
                mAll.Add(tile);

                if(start)
                {
                    StartTile = tile;
                }

                position.z += TileSize;
                start = false;
            }

            position.x += TileSize;
            position.z = -(halfHeight * TileSize);
        }
    }

    private int GetRandomRotation()
    {
        int RotationValue = 0;
        switch (Random.Range(0, 4))
        {
            case 0:
                RotationValue = 0;
                break;
            case 1:
                RotationValue = 90;
                break;
            case 2:
                RotationValue = 180;
                break;
            case 3:
                RotationValue = 270;
                break;
        }
        return RotationValue;
    }

    private void SetupConnections()
    {
        // Currently we are only setting up connections between adjacnt nodes
        for (int x = 0; x < Size.x; ++x)
        {
            for (int y = 0; y < Size.y; ++y)
            {
                EnvironmentTile tile = mMap[x][y];
                tile.Connections = new List<EnvironmentTile>();
                if (x > 0)
                {
                    tile.Connections.Add(mMap[x - 1][y]);
                }

                if (x < Size.x - 1)
                {
                    tile.Connections.Add(mMap[x + 1][y]);
                }

                if (y > 0)
                {
                    tile.Connections.Add(mMap[x][y - 1]);
                }

                if (y < Size.y - 1)
                {
                    tile.Connections.Add(mMap[x][y + 1]);
                }
            }
        }
    }

    private float Distance(EnvironmentTile a, EnvironmentTile b)
    {
        // Use the length of the connection between these two nodes to find the distance, this 
        // is used to calculate the local goal during the search for a path to a location
        float result = float.MaxValue;
        EnvironmentTile directConnection = a.Connections.Find(c => c == b);
        if (directConnection != null)
        {
            result = TileSize;
        }
        return result;
    }

    private float Heuristic(EnvironmentTile a, EnvironmentTile b)
    {
        // Use the locations of the node to estimate how close they are by line of sight
        // experiment here with better ways of estimating the distance. This is used  to
        // calculate the global goal and work out the best order to prossess nodes in
        return Vector3.Distance(a.Position, b.Position) * 10;
    }

    public void GenerateWorld()
    {
        Generate();
        SetupConnections();
        GetMaxDoorCount();
        SetAllDoors();
        SetAllPedistals();
    }
    
    public void CleanUpWorld()
    {
        if (mMap != null)
        {
            for (int x = 0; x < Size.x; ++x)
            {
                for (int y = 0; y < Size.y; ++y)
                {
                    Destroy(mMap[x][y].gameObject);
                }
            }
        }
    }

    public void SetTileVisibility(Vector2Int coor, bool visibility)
    {
        mMap[coor.x][coor.y].gameObject.SetActive(visibility);
    }

    public List<EnvironmentTile> Solve(EnvironmentTile begin, EnvironmentTile destination)
    {
        List<EnvironmentTile> result = null;
        if (begin != null && destination != null)
        {
            // Nothing to solve if there is a direct connection between these two locations
            EnvironmentTile directConnection = begin.Connections.Find(c => c == destination);
            // Vector3 direction = begin.transform.position - destination.transform.position;
            //Debug.LogError(direction);
            if (directConnection == null || !IsTileAccessible(begin, directConnection))
            {
                // Set all the state to its starting values
                mToBeTested.Clear();

                for (int count = 0; count < mAll.Count; ++count)
                {
                    mAll[count].Parent = null;
                    mAll[count].Global = float.MaxValue;
                    mAll[count].Local = float.MaxValue;
                    mAll[count].Visited = false;
                }

                // Setup the start node to be zero away from start and estimate distance to target
                EnvironmentTile currentNode = begin;
                currentNode.Local = 0.0f;
                currentNode.Global = Heuristic(begin, destination);

                // Maintain a list of nodes to be tested and begin with the start node, keep going
                // as long as we still have nodes to test and we haven't reached the destination
                mToBeTested.Add(currentNode);

                while (mToBeTested.Count > 0 && currentNode != destination)
                {
                    // Begin by sorting the list each time by the heuristic
                    mToBeTested.Sort((a, b) => (int)(a.Global - b.Global));

                    // Remove any tiles that have already been visited
                    mToBeTested.RemoveAll(n => n.Visited);

                    // Check that we still have locations to visit
                    if (mToBeTested.Count > 0)
                    {
                        // Mark this note visited and then process it
                        currentNode = mToBeTested[0];
                        currentNode.Visited = true;

                        // Check each neighbour, if it is accessible and hasn't already been 
                        // processed then add it to the list to be tested 
                        for (int count = 0; count < currentNode.Connections.Count; ++count)
                        {
                            EnvironmentTile neighbour = currentNode.Connections[count];

                            if (!neighbour.Visited && IsTileAccessible(currentNode, neighbour))
                            {
                                mToBeTested.Add(neighbour);

                                //if (!neighbour.Visited && neighbour.IsAccessible)
                                //{
                                //    mToBeTested.Add(neighbour);
                                //}

                                // Calculate the local goal of this location from our current location and 
                                // test if it is lower than the local goal it currently holds, if so then
                                // we can update it to be owned by the current node instead 

                                float possibleLocalGoal = currentNode.Local + Distance(currentNode, neighbour);
                                if (possibleLocalGoal < neighbour.Local)
                                {
                                    neighbour.Parent = currentNode;
                                    neighbour.Local = possibleLocalGoal;
                                    neighbour.Global = neighbour.Local + Heuristic(neighbour, destination);
                                }
                            }
                        }
                    }
                }

                // Build path if we found one, by checking if the destination was visited, if so then 
                // we have a solution, trace it back through the parents and return the reverse route
                if (destination.Visited)
                {
                    result = new List<EnvironmentTile>();
                    EnvironmentTile routeNode = destination;

                    while (routeNode.Parent != null)
                    {
                        //Debug.LogError(routeNode.gameObject.name);
                        result.Add(routeNode);
                        routeNode = routeNode.Parent;
                    }
                    result.Add(routeNode);
                    result.Reverse();

                    Debug.LogFormat("Path Found: {0} steps {1} long", result.Count, destination.Local);
                }
                else
                {
                    Debug.LogWarning("Path Not Found");
                }
            }
            else 
            {
                result = new List<EnvironmentTile>();
                result.Add(begin);
                result.Add(begin);
                result.Add(destination);
                Debug.LogFormat("Direct Connection: {0} <-> {1} {2} long", begin, destination, TileSize);
            }
        }
        else
        {
            Debug.LogWarning("Cannot find path for invalid nodes");
        }

        mLastSolution = result;

        return result;
    }

    public Vector2Int GetMaxTiles()
    {
        return Size;
    }

    // Is there a wall in the way
    private bool IsTileAccessible(EnvironmentTile currentTile, EnvironmentTile NeighbourTile)
    {
        if (NeighbourTile == null) return false;
        if (currentTile.IsAccessible && NeighbourTile.IsAccessible) return true;

        
        Vector3 direction = currentTile.Position - NeighbourTile.Position;  

        if(direction == directionLeft)
        {
           // Debug.LogError("LEFT");
            return !IsFacingWall(currentTile, NeighbourTile, 270, 90);
        }
        else if (direction == directionRight)
        {
           // Debug.LogError("right");
            return !IsFacingWall(currentTile, NeighbourTile, 90, 270);
        }
        else if (direction == directionForward)
        {
           // Debug.LogError("forward");
            if (currentTile.IsAccessible)
            {
                if (NeighbourTile.transform.eulerAngles.y == 180)
                {
                   // Debug.LogError("false");
                    return false;
                }
                else
                {
                    //Debug.LogError("true");
                }
            }
            else
            {
                return !IsFacingWall(currentTile, NeighbourTile, 0, 180);
            }           
        }
        else if (direction == directionBack)
        {
           // Debug.LogError("back");
            if (NeighbourTile.IsAccessible)
            {
                if (currentTile.transform.eulerAngles.y == 180)
                {
                   // Debug.LogError("false");
                    return false;
                }
                else
                {
                    //Debug.LogError("true");
                }
            }
            else
            {
                return !IsFacingWall(currentTile, NeighbourTile, 180, 0);
            }
            
        }
        
        return true;
    }

    //Checks if rotations match the wall position
    private bool IsFacingWall(EnvironmentTile currentTile, EnvironmentTile NeighbourTile, float CurrentRotation, float NeightbourRotation)
    {
        if (currentTile.transform.eulerAngles.y == CurrentRotation || NeighbourTile.transform.eulerAngles.y == NeightbourRotation)
        {
           // Debug.LogError(currentTile.transform.eulerAngles.y);   
            return true;
        }
        else
        {
            //Debug.LogError(currentTile.transform.eulerAngles.y);
            return false;
        }
    }

    public EnvironmentTile GetRandomTile()
    {        
        return mMap[Random.Range(0,Size.x)][Random.Range(0, Size.y)]; 
    }

    public void RotateAllTiles()
    {
        
        for (int x = 0; x < Size.x; ++x)
        {
            for (int y = 0; y < Size.y; ++y)
            {
                EnvironmentTile currentTile = mMap[x][y];
                if (!currentTile.IsAccessible & Random.Range(0,4) < 3)
                {
                    //Uses hand made vector for rotation position
                    // currentTile.transform.RotateAround(currentTile.transform.position + GetRotationPosition(currentTile.transform.eulerAngles.y), Vector3.up, GetRandomRotation());

                    //Uses gameobject - Instant
                    //currentTile.transform.RotateAround(currentTile.GetRotationBlockPosition(), Vector3.up, GetRandomRotation());

                    //Uses gameobject - OverTime //Random or Set
                    StartCoroutine(RotateOverTime(currentTile, (int)currentTile.transform.eulerAngles.y + 90));

                }
            }
        }
    }

    //Test Function
    //[SerializeField] private bool RotateTiles;
    //private void Update()
    //{
    //    if (RotateTiles)
    //    {
    //        RotateAllTiles();
    //        RotateTiles = false;
    //    }
    //}

    
    //private Vector3 GetRotationPosition(float EulerAngle)
    //{
    //    switch (EulerAngle)
    //    {
    //        case 0:
    //            return new Vector3(5, 0, 5);
    //        case 90:
    //            return new Vector3(5, 0, -5);
    //        case 180:
    //            return new Vector3(-5, 0, -5);
    //        case 270:
    //            return new Vector3(-5, 0, 5);
    //    }

    //    return new Vector3(5,0,5);
    //}

    private IEnumerator RotateOverTime(EnvironmentTile tile, int rotation)
    {
        
        float roationInterval = 20f;
        if (rotation >= 360) rotation = 0;
        
        while (Mathf.RoundToInt(tile.transform.eulerAngles.y) != rotation)
        {
            TileRotaionFinish = false;
            tile.transform.RotateAround(tile.GetRotationBlockPosition(), Vector3.up, roationInterval * Time.deltaTime);
            yield return new WaitForEndOfFrame(); 
        }
        tile.transform.eulerAngles = new Vector3(0, Mathf.RoundToInt(rotation), 0);
        tile.transform.localPosition = new Vector3(Mathf.RoundToInt(tile.transform.localPosition.x), Mathf.RoundToInt(tile.transform.localPosition.y), Mathf.RoundToInt(tile.transform.localPosition.z));
        TileRotaionFinish = true;
    }

    public bool GetTileRotationStatus()
    {
    
        return TileRotaionFinish;
    }



}
