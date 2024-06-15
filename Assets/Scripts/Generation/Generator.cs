using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class Generator : MonoBehaviour
{
    public struct StaircaseSpawnCoords
    {
        public Vector2Int pos;
        public int layer;
    }
    public struct RoomOrientationData
    {
        public int rotation;
        public RoomType type;
    }

    public static Generator generator;

    //REMOVE FOR RELEASE
    public bool DEBUGMODEACTIVE = false;

    [Header("Initialization Settings")]
    //Board length.
    public int boardLength = 4;
    public int boardScale = 1;

    [Tooltip("Begin generation from a random point on the board")]
    public bool useRandomOriginPos = true;
    

    [Header("Generation Settings")]
    public int branchLengthMax = 10;
    public int branchLengthMin = 6;

    [Tooltip("Probability that a branch will occur frm each room. Has a 1 in (this value) chance.")]
    public int branchOffProbability = 4;
    private int currentBranchOffProbability;
    [Tooltip("Probability that a branch will occur frm each room. Has a 1 in (this value) chance.")]
    public int mergeProbability = 10;

    [Tooltip("The probability of branching decreases for each new branch.")]
    public bool diminishingBranchingProbability = false;
    [Tooltip("Effectively how much the probability will decrease per new branch.")]
    public int branchProbabilityDiminishAmount = 1;

    public bool limitNumberBranches = false;
    [Tooltip("Maximum number of times generation can branch (only active if the limit is true)")]
    public int maximumNumBranches = 5;

    [Tooltip("Force the generator to generate # of floors other than the first")]
    public int forceNumberExtraFloors = 1;

    [Tooltip("1 in this number probability to generate a staircase and new floor at the end of each branch")]
    public int generateFloorProbability = 2;
    public int maximumNumFloors = 2;


    [Header("Refs")]
    public bool generateUnderParent;
    public GameObject[] levelParent;

    public GameObject RoomPrefab;
    public GameObject HallwayPrefab;

    public GameObject[] SingleRooms;
    public GameObject[] ElbowRooms;
    public GameObject[] CrossRooms;
    public GameObject[] StraightRooms;
    public GameObject[] ThreeWayRooms;

    public GameObject[] Corridoors;

    public NavMeshSurface surface;


    [Header("Trackers")]
    public float timer = 30;

    public int numberBranches = 0;
    public int numberStaircases = 0;

    private List<Room[,]> boards;

    public List<List<GameObject>> allObjects;

    //Possible directions.
    List<Vector2Int> possibleDirs;

    private Vector2Int startPlace;
    private Vector2Int nullDirReference;

    private List<Vector2Int> orderedSpawns;

    public List<StaircaseSpawnCoords> staircaseSpawnPoints;

    [Header("Performance Optimization")]

    public List<Transform> activePlayers;
    public List<List<GameObject>> AllCurrentActiveObjects;

    private void Start()
    {
        if (generator == null) generator = this;

        //keep the null direction reference.
        nullDirReference = new Vector2Int(0, 0);

        orderedSpawns = new List<Vector2Int>();

        possibleDirs = new List<Vector2Int>
        {
            //Add the possible directions: Up, Down, Right, Left (Except by 2 b/c there are cooridoors in between.
            new Vector2Int(0, 2),  //North
            new Vector2Int(2, 0),  //East
            new Vector2Int(0, -2),  //South
            new Vector2Int(-2, 0) //West
        };

        //InitBoard();
        //GenerateNewStartPoint();
        //InitGeneration(0, startPlace);

        //GenerateObjects();

        AllCurrentActiveObjects = new List<List<GameObject>>();

        //This marks the start of the Generation
        {
            InitBoard();
            GenerateNewStartPoint();
            InitGeneration(0, startPlace, false);
            MergeBranches(0);

            Debug.Log("end first gen");

            foreach (StaircaseSpawnCoords coord in staircaseSpawnPoints)
            {
                Debug.Log("New layer from pt");
                InitGeneration(coord.layer, coord.pos, false, true);
            }

            //MergeBranches(1);

            staircaseSpawnPoints.Clear();

            Debug.Log("end board gen, onto object gen");

            PrepRoomGeneration();

            ShowAllRooms(true);
            surface?.BuildNavMesh();
            ShowAllRooms(false);

            Debug.Log("End of loop");
        }

        //REMOVE
        //REMOVE
        //REMOVE
        //REMOVE THIS CODE FOR RELEASE PLEASE
        #region Debug
        if (DEBUGMODEACTIVE)
        {
            AllCurrentActiveObjects.Add(new List<GameObject>());
            ShowRoomsCloseToAllPlayerss();
        } 
        #endregion
    }

    private void Update()
    {
        
    }

    public void InitBoard()
    {
        boards = new List<Room[,]>();
        staircaseSpawnPoints = new List<StaircaseSpawnCoords>();

        CreateBoardLayer();
    }

    public void CreateBoardLayer()
    {
        int layer = boards.Count; //Index of new layer

        boards.Add(new Room[boardLength * 2 - 1, boardLength * 2 - 1]);

        for (int i = 0; i < boardLength * 2 - 1; i++)
        {
            for (int j = 0; j < boardLength * 2 - 1; j++)
            {
                boards[layer][i, j] = new Room(layer, i, j, boardScale);
                boards[layer][i, j].status = 0;
            }
        }
    }

    public void InitGeneration(int layer, Vector2Int startPlace, bool generateStaircases = true, bool startingNewFloor = false)
    {
        List<Vector2Int> usedDirs = new List<Vector2Int>(); ;

        //Set the board up.

        currentBranchOffProbability = branchOffProbability; //Reset the branch off probability

        //Setting start room at end due to it possibly being reset

        List <Vector2Int> usedBranchDirs = new List<Vector2Int>();
        usedBranchDirs.Clear();

        //Generate Base Sctructure.
        GenerateBranchFromPoint(layer, startPlace, usedDirs, usedBranchDirs, branchLengthMax, branchLengthMin, 4, generateStaircases);

        //
        //
        //Now generate branches off of some of the already made rooms.
        usedBranchDirs.Clear();
        usedDirs.Clear();

        foreach (Room room in boards[layer])
        {
            if (room.status == RoomStatus.Room && Random.Range(0, currentBranchOffProbability + 1) == 0)
            {
                if (limitNumberBranches && numberBranches >= maximumNumBranches) break;
                //New Branch formed from this pos
                GenerateBranchFromPoint(layer, new Vector2Int(room.x, room.y), usedDirs, usedBranchDirs, 3, 2, 2, generateStaircases);

                //Check for diminishing branches and update counters
                numberBranches++;

                if (diminishingBranchingProbability) currentBranchOffProbability += branchProbabilityDiminishAmount; //Make it less probably to make new branches in the future

                if (limitNumberBranches && numberBranches >= maximumNumBranches) break; //If theres already the max branches, quit out of the loop
            }
        }


        boards[layer][startPlace.x, startPlace.y].status = startingNewFloor ? RoomStatus.StaircaseRoom : RoomStatus.Room;
        Debug.Log(startingNewFloor);
        if (startingNewFloor)
        {
            boards[layer][startPlace.x, startPlace.y].staircaseTop = true;
            boards[layer - 1][startPlace.x, startPlace.y].status = RoomStatus.StaircaseRoom;

        }
    }

    void GenerateBranchFromPoint(int layer, Vector2Int start, List<Vector2Int> usedDirs, List<Vector2Int> usedBranchDirs, int maxLen, int minLen, int branches = 4, bool generateStaircases = true)
    {
        Vector2Int curPos;
        Vector2Int lastDir;

        //Generates branches.
        for (int i = 0; i < branches; i++)
        {
            curPos = start;
            //Store the random dir.
            lastDir = GeneratePath(layer, curPos, usedBranchDirs);
            curPos += lastDir;

            //Generate room.
            boards[layer][curPos.x, curPos.y].status = RoomStatus.Room;
            //Generate coridor.
            if (lastDir != nullDirReference)
            {
                boards[layer][curPos.x - (lastDir.x / 2), curPos.y - (lastDir.y / 2)].status = RoomStatus.Corridor;
                boards[layer][curPos.x - (lastDir.x / 2), curPos.y - (lastDir.y / 2)].objDir = lastDir;
            }

            //Generates how long the branches are.
            for (int k = Random.Range(minLen, maxLen), j = 0; j < k; j++)
            {
                lastDir = GeneratePath(layer, curPos, usedDirs);
                curPos += lastDir;

                //Create the room.
                boards[layer][curPos.x, curPos.y].status = RoomStatus.Room;

                //Add to oreder list.
                orderedSpawns.Add(new Vector2Int(curPos.x, curPos.y));

                //Create the corridor.
                if (lastDir != nullDirReference)
                {
                    //Set the cell in between to a hallway.
                    boards[layer][curPos.x - (lastDir.x / 2), curPos.y - (lastDir.y / 2)].status = RoomStatus.Corridor;

                    //Set that hallway's direction
                    boards[layer][curPos.x - (lastDir.x / 2), curPos.y - (lastDir.y / 2)].objDir = lastDir;

                    //Add the next in order.
                    orderedSpawns.Add(new Vector2Int(curPos.x, curPos.y));
                }

                //Check for stairwell creation (after corridoors are generated bc this shit bouta get recursive)
                if (generateStaircases && j == k - 1)
                {
                    if (Random.Range(0, generateFloorProbability) == 0)
                    {
                        //Then make this a staircase area
                        boards[layer][curPos.x, curPos.y].status = RoomStatus.StaircaseRoom;
                        boards[layer][curPos.x, curPos.y].staircaseTop = false;

                        if (boards.Count <= layer + 1)
                        {
                            CreateBoardLayer();
                        }

                        StaircaseSpawnCoords temp = new()
                        {
                            pos = curPos,
                            layer = layer + 1
                        };

                        staircaseSpawnPoints.Add(temp);

                        Debug.Log($"new layer at {curPos} with a layer {layer} and branch {i}");

                        //InitGeneration(layer + 1, curPos, true);
                        //GenerateBranchFromPoint(layer + 1, curPos, new List<Vector2Int>(), new List<Vector2Int>(), branchLengthMax - 1, branchLengthMin - 1, 3);
                    }
                }

                usedDirs.Clear();
                usedDirs.Add(lastDir * -1);
            }
        }
    }

    public Vector2Int GeneratePath(int layer, Vector2Int start, List<Vector2Int> dirList)
    {
        Vector2Int pos = start;

        Vector2Int currentDir = new Vector2Int();

        while (true)
        {
            //Check for a direction to go in.
            currentDir = LookForDir(currentDir, dirList);

            if (currentDir == nullDirReference) break;

            if (!BoardSpaceDoesntExistOrIsOccupied(layer, pos + currentDir))
            {
                break;
            }
        }

        return currentDir;
    }

    bool BoardSpaceDoesntExistOrIsOccupied(int layer, Vector2Int pos)
    {
        //Check if it is out of bounds.
        if (!BoardSpaceExists(pos)) return true;

        if (BoardSpaceOccupied(layer, pos)) return true;

        return false;
    }

    private bool BoardSpaceExists(Vector2Int pos)
    {
        //Check if it is out of bounds.
        if (pos.x < 0 || pos.x > boardLength * 2 - 2) return false;
        if (pos.y < 0 || pos.y > boardLength * 2 - 2) return false;

        //Then its gotta be in the bounds
        return true;
    }

    private bool BoardSpaceOccupied(int layer, Vector2Int pos)
    {
        ///Check if the status of the desired board space is taken
        if (boards[layer][pos.x, pos.y].status == RoomStatus.Room || boards[layer][pos.x, pos.y].status == RoomStatus.StaircaseRoom) return true;

        return false;
    }

    Vector2Int LookForDir(Vector2Int currentDir, List<Vector2Int> usedDirs)
    {
        Vector2Int[] checkedDirs = new Vector2Int[possibleDirs.Count];

        //Set the array to some imposible values.
        for (int i = 0; i < checkedDirs.Length; i++)
        {
            checkedDirs[i] = nullDirReference;
        }

        int checkingPlace = 0;

        while (true)
        {
            currentDir = possibleDirs[Random.Range(0, 4)];

            if (!usedDirs.Contains(currentDir))
            {
                usedDirs.Add(currentDir);
                break;
            }

            checkedDirs[checkingPlace] = currentDir;
            checkingPlace++;

            //Check if all of the possibilities have been wasted by going through and looking to see if there are no default Vector2s
            int usedCount = 0;
            for (int i = 0; i < checkedDirs.Length; i++)
            {
                if (checkedDirs[i] != nullDirReference) usedCount++;
            }

            if (usedCount == checkedDirs.Length)
            {
                currentDir = nullDirReference;
                break;
            }
        }

        return currentDir;
    }

    void MergeBranches(int layer)
    {
        foreach (Room room in boards[layer])
        {
            if (room.status != RoomStatus.Room && Random.Range(0, mergeProbability) != Mathf.RoundToInt(mergeProbability / 2)) continue;
            
            //HEY FUTURE TY CAN U MAKE THESE VECTOR2INTS PLS THX

            Vector2Int mergeDir;
            Vector2Int pos;

            List<Vector2> usedDirs = new List<Vector2>();
            usedDirs.Clear();

            pos = new Vector2Int(room.x, room.y);

            while (true)
            {
                while (true)
                {
                    mergeDir = possibleDirs[Random.Range(0, possibleDirs.Count)];

                    if (!usedDirs.Contains(mergeDir))
                    {
                        break;
                    }
                }

                //Check the availabilit of this new merge pos
                if (BoardSpaceExists(pos + mergeDir))
                {
                    if (BoardSpaceOccupied(layer, pos + mergeDir))
                    {
                        //If the CORRIDOR between the rooms is empty, fill it
                        if (boards[layer][pos.x + mergeDir.x / 2, pos.y + mergeDir.y / 2].status == RoomStatus.EmptyRoom)
                        {
                            if(Random.Range(0, mergeProbability) == Mathf.RoundToInt(mergeProbability / 2))
                            {
                                boards[layer][Mathf.RoundToInt(pos.x + mergeDir.x / 2), Mathf.RoundToInt(pos.y + mergeDir.y / 2)].status = RoomStatus.Corridor;
                                boards[layer][Mathf.RoundToInt(pos.x + mergeDir.x / 2), Mathf.RoundToInt(pos.y + mergeDir.y / 2)].objDir = mergeDir;
                                boards[layer][Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)].status = RoomStatus.Room;
                            }
                        }
                        break;
                    }
                    else
                    {
                        //Not a usable merge direction
                        usedDirs.Add(mergeDir);
                    }
                }
                else
                {
                    //Not a usable merge Direction
                    usedDirs.Add(mergeDir);
                }

                //If the possibilities are all wasted then move on.
                if (usedDirs.Count == possibleDirs.Count) break;
            }
        }
    }

    public void GenerateNewStartPoint()
    {
        //Set a start place. (Either random [right hand operand] or in the center of teh board [left hand operand]
        //Make it -2 b/c the board len gth is allat -1 and we need another extra -1 to leave room for the clamping below
        //Otherwise the start point could be out of bounds.
        startPlace = useRandomOriginPos ? new Vector2Int(Random.Range(1, boardLength) * 2 - 1, Random.Range(1, boardLength) * 2 - 1) : new Vector2Int(boardLength / 2, boardLength / 2);

        //The following pretty much forces the start point to be even
        if (startPlace.x % 2 != 0) startPlace.x++;
        if (startPlace.y % 2 != 0) startPlace.y++;

        Debug.Log($"Start pos {startPlace}");
    }

    public void GenerateObjects()
    {
        if (boards == null)
        {
            return;
        }

        //Draw little spheres at the room places (just for testing).
        foreach (Room[,] layer in boards)
        {
            foreach (Room room in layer)
            {
                switch (room.status)
                {
                    case RoomStatus.Room:
                        Vector3 objPos;

                        objPos.x = room.x * boardScale;
                        objPos.y = 0;
                        objPos.z = room.y * boardScale;

                        GameObject instObj = Instantiate(RoomPrefab, objPos, Quaternion.identity);
                        if (generateUnderParent) instObj.transform.SetParent(levelParent[0].transform);
                        break;

                    case RoomStatus.Corridor:
                        Vector3 objPos2;

                        objPos2.x = room.x * boardScale;
                        objPos2.y = 0;
                        objPos2.z = room.y * boardScale;

                        //Determine the object direction.
                        float instYRot = 0;

                        //Check for a vertical hallway.
                        print(Mathf.Abs(room.objDir.y));

                        if (room.objDir.y != 0)
                        {
                            instYRot = 90;
                        }

                        GameObject instObj2 = Instantiate(HallwayPrefab, objPos2, Quaternion.Euler(0, instYRot, 0));
                        if (generateUnderParent) instObj2.transform.SetParent(levelParent[0].transform);
                        break;
                }

            }
        }
    }

    private void OnDrawGizmos()
    {
        if (boards == null)
        {
            return;
        }

        //Draw little spheres at the room places (just for testing).
        foreach (Room[,] layer in boards)
        {
            foreach (Room room in layer)
            {
                switch (room.status)
                {
                    case RoomStatus.Room:
                        Vector3 gizmoPos;

                        gizmoPos.x = room.x * boardScale;
                        gizmoPos.y = room.layer * 3;
                        gizmoPos.z = room.y * boardScale;

                        Gizmos.color = Color.white;
                        Gizmos.DrawSphere(gizmoPos, .5f);
                        break;

                    case RoomStatus.Corridor:
                        Vector3 gizmoPos2;

                        gizmoPos2.x = room.x * boardScale;
                        gizmoPos2.y = room.layer * 3;
                        gizmoPos2.z = room.y * boardScale;

                        Gizmos.color = Color.white;
                        Gizmos.DrawCube(gizmoPos2, new Vector3(.5f, .5f, .5f));
                        break;

                    case RoomStatus.StaircaseRoom:
                        Vector3 gizmoPos3;

                        gizmoPos3.x = room.x * boardScale;
                        gizmoPos3.y = room.layer * 3;
                        gizmoPos3.z = room.y * boardScale;

                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(gizmoPos3, .5f);
                        break;
                }
            }

        }
    }

    public void PrepRoomGeneration()
    {
        allObjects = new List<List<GameObject>>();

        //Layer by layer
        for (int i = 0; i < boards.Count; i++)
        {
            //Init this layer
            allObjects.Add(new List<GameObject>());

            //Room by room
            foreach (Room room in boards[i])
            {
                if (room.status == RoomStatus.EmptyRoom) continue; //Skip these non-rooms

                GameObject obj;

                if (room.status == RoomStatus.Room) //Then do default gen
                {
                    RoomOrientationData roomData = IdentifyRoomOrientation(i, new Vector2Int(room.x, room.y));

                    switch (roomData.type)
                    {
                        case RoomType.Single:
                            obj = Instantiate(SingleRooms[Random.Range(0, SingleRooms.Length)], levelParent[i].transform);
                            break;

                        case RoomType.Elbow:
                            obj = Instantiate(ElbowRooms[Random.Range(0, ElbowRooms.Length)], levelParent[i].transform);
                            break;

                        case RoomType.Straight:
                            obj = Instantiate(StraightRooms[Random.Range(0, StraightRooms.Length)], levelParent[i].transform);
                            break;

                        case RoomType.Threeway:
                            obj = Instantiate(ThreeWayRooms[Random.Range(0, ThreeWayRooms.Length)], levelParent[i].transform);
                            break;

                        case RoomType.Cross:
                            obj = Instantiate(CrossRooms[Random.Range(0, CrossRooms.Length)], levelParent[i].transform);
                            break;

                        default: //Throwaway just to make the compiler happy (bc cannot add unassigned "obj")
                            obj = new GameObject();
                            break;

                    }

                    obj.transform.SetPositionAndRotation(new Vector3(room.x * boardScale, i * .75f * boardScale, room.y * boardScale), Quaternion.Euler(0, roomData.rotation, 0));

                }
                else if (room.status == RoomStatus.StaircaseRoom)//Then its a staircase rooom, and do staircase gen
                {
                    obj = new GameObject();
                }
                else //Then it must be a corridoor
                {
                    obj = Instantiate(Corridoors[Random.Range(0, Corridoors.Length)], levelParent[i].transform);

                    obj.transform.SetPositionAndRotation(new Vector3(room.x * boardScale, i * .75f * boardScale, room.y * boardScale), Quaternion.Euler(0, room.objDir.y == 0 ? 90 : 0, 0));
                }

                obj.SetActive(false);

                boards[i][room.x, room.y].roomObj = obj;

                allObjects[i].Add(obj);
            }
        }
    }

    public RoomOrientationData IdentifyRoomOrientation(int layer, Vector2Int location)
    {
        List<Vector2Int> connectionDirections = new List<Vector2Int>();
        RoomOrientationData tmpData = new RoomOrientationData();
        tmpData.type = RoomType.None;

        for (int i = 0; i < 4; i++)
        {
            Vector2Int scanPos = location + possibleDirs[i];

            if (BoardSpaceExists(scanPos) && BoardSpaceOccupied(layer, scanPos))
            {
                //Make SURE theres a corridoor there, its not just generating between 2 side-by-side rooms without a connection
                if (boards[layer][location.x + (possibleDirs[i].x / 2), location.y + (possibleDirs[i].y / 2)].status != RoomStatus.Corridor) continue;

                switch (tmpData.type)
                {
                    case RoomType.None:
                        tmpData.type = RoomType.Single;
                        break;

                    case RoomType.Single:
                        //If the new connection is across from the existing single
                        if (connectionDirections[0] == possibleDirs[i] * -1)
                        {
                            tmpData.type = RoomType.Straight;
                            break;
                        }

                        //Then it must be an elbow
                        tmpData.type = RoomType.Elbow;
                        break;

                    case RoomType.Elbow:
                        tmpData.type = RoomType.Threeway;
                        break;

                    case RoomType.Straight:
                        tmpData.type = RoomType.Threeway;
                        break;

                    case RoomType.Threeway:
                        tmpData.type = RoomType.Cross;
                        break;
                }

                //There b a room there, so add this direction
                connectionDirections.Add(possibleDirs[i]);
            }
        }

        //Handle rotation (turns these "connectionDirections" into actual degrees of rotation for the base 5 room types)
        switch (tmpData.type)
        {
            case RoomType.Single:
                //The order of possible directions shows a clockwise order of directions, each being 90 degrees
                //Therefore multiplying the index with 90 deg will return the degrees rotation of that peice, with north being 0°
                tmpData.rotation = possibleDirs.IndexOf(connectionDirections[0]) * 90;
                break;

            case RoomType.Elbow:
                //Now for the elbow rotation. A default elbow enters from south and east
                if (connectionDirections.Contains(possibleDirs[2])) //Check if south
                {
                    if (connectionDirections.Contains(possibleDirs[1])) //Check if East (default orientation then)
                    {
                        tmpData.rotation = 0; //Defaulr rot
                    }
                    else //Has to be west then
                    {
                        tmpData.rotation = 90; //Becuase the elbow must be south and west, a 90° rotation because its clockwise
                    }
                }
                else //if (connectionDirections.Contains(possibleDirs[0])) //Contains North
                {
                    if (connectionDirections.Contains(possibleDirs[1])) //Check if East
                    {
                        tmpData.rotation = 270; // 3/4 of a full clockwise rotation
                    }
                    else
                    {
                        tmpData.rotation = 180; //Becuase the elbow must be north and west, a mirror of the default
                    }
                }

                break;

            case RoomType.Straight:
                //2 possible rotations
                if (connectionDirections.Contains(possibleDirs[0])) //If it runs North to south
                {
                    tmpData.rotation = (Random.Range(0, 2) == 0) ? 0 : 180; //Randomize the rotation for top to bottom
                }
                else
                {
                    tmpData.rotation = (Random.Range(0, 2) == 0) ? 90 : 270;
                }
                break;

            case RoomType.Threeway:
                //The default will be a straight north to south and an east connection too
                //The nub is how I refer to the non-straight connection.

                if (connectionDirections.Contains(possibleDirs[0])) //North
                {
                    if (connectionDirections.Contains(possibleDirs[2]))
                    {
                        //Then the straight is north-south, just need to check which side has the nub
                        if (connectionDirections.Contains(possibleDirs[1])) //East
                        {
                            //This is the default rotaion of a 3-way
                            tmpData.rotation = 0;
                        }
                        else
                        {
                            //This is a mirror of the default rot (180°)
                            tmpData.rotation = 180;
                        }
                    }
                    else
                    {
                        //Then the nub of the 3-way is up top (North)
                        tmpData.rotation = 270; // 3/4 clockwise rotation from default
                    }
                }
                else
                {
                    //Then the nub on the 3-way must be south
                    tmpData.rotation = 90; //Clockwise
                }

                break;

            case RoomType.Cross:
                tmpData.rotation = 90 * Random.Range(0, 5); //Crosses can be in any rotation of 90° and still line up
                break;
        }

        return tmpData;
    }

    public void ShowRoomsCloseToAllPlayerss()
    {
        for (int i = 0; i < activePlayers.Count; i++)
        {
            StartCoroutine(ShowRoomsCloseToPlayer(i));
        }
    }

    public IEnumerator ShowRoomsCloseToPlayer(int playerIndex)
    {
        //Clear all rooms
        List<GameObject> newActiveObjects = new List<GameObject>();

        if (AllCurrentActiveObjects[playerIndex] == null) AllCurrentActiveObjects[playerIndex] = new List<GameObject>();

        //activeObjects.Clear();

        //The following is per player
        //foreach (Transform player in activePlayers)
        Transform player = activePlayers[playerIndex];

        {
            //Identify the layer
            int layer = player.position.y > 8f ? 1 : 0;


            Vector2Int playerCoord = PlayerPositionToBoardCoordinate(player.position);

            newActiveObjects.Add(boards[layer][playerCoord.x, playerCoord.y].roomObj);

            for (int i = 0; i < 4; i++) //Do all directions
            {
                Vector2Int currentCoord = playerCoord + possibleDirs[i];

                if (!BoardSpaceExists(currentCoord) || !BoardSpaceOccupied(layer, currentCoord)) continue;

                //Add the corridoor and room
                newActiveObjects.Add(boards[layer][currentCoord.x, currentCoord.y].roomObj);
                newActiveObjects.Add(boards[layer][playerCoord.x + (possibleDirs[i].x / 2), playerCoord.y + (possibleDirs[i].y / 2)].roomObj); //Add the cooridoor
            }
        }

        List<GameObject> activeObjectsBeforeWeed = newActiveObjects.GetRange(0, newActiveObjects.Count);

        for (int i = 0; i < AllCurrentActiveObjects[playerIndex].Count; i++)
        {
            if (newActiveObjects.Contains(AllCurrentActiveObjects[playerIndex][i]))
            {
                //Remove object from temp list (to prevent seting it to active twice)
                newActiveObjects.Remove(AllCurrentActiveObjects[playerIndex][i]);
            }
            else
            {
                bool objectIsInOtherPlayerVicinity = false;

                //Look for this object in other players lists
                for (int j = 0; j < AllCurrentActiveObjects.Count; j++)
                {
                    if (j == playerIndex) continue;

                    if (AllCurrentActiveObjects[j].Contains(AllCurrentActiveObjects[playerIndex][i]))
                    {
                        objectIsInOtherPlayerVicinity = true;
                    }
                }
                //Hide objects that arent in the current list of active
                if (!objectIsInOtherPlayerVicinity) AllCurrentActiveObjects[playerIndex][i]?.SetActive(false);
            }
        }

        //Show all other objects that arent already visible
        foreach (GameObject obj in newActiveObjects)
        {
            if (obj != null) obj.SetActive(true);
        }

        AllCurrentActiveObjects[playerIndex] = activeObjectsBeforeWeed.GetRange(0, activeObjectsBeforeWeed.Count);

        yield return null;
    }

    public Vector2Int PlayerPositionToBoardCoordinate(Vector3 playerPos)
    {
        Vector2Int coord = Vector2Int.RoundToInt(new Vector2(playerPos.x, playerPos.z) / boardScale);
        
        if (coord.x % 2 != 0) //Needs to be odd to be a room
        {
            if (playerPos.x / boardScale % 2 > 1)
            {
                coord.x++;
            }
            else
            {
                coord.x--;
            }
        }

        if (coord.y % 2 != 0) //Needs to be odd to be a room
        {
            if (playerPos.z / boardScale % 2 > 1)
            {
                coord.y++;
            }
            else
            {
                coord.y--;
            }
        }

        if (BoardSpaceExists(coord))
        {
            return coord;
        }
        else
        {
            //Maybe clamp to within the board
            coord.x = Mathf.Clamp(coord.x, 0, boardLength * 2 - 2);
            coord.y = Mathf.Clamp(coord.y, 0, boardLength * 2 - 2);

            return coord;
        }
    }

    int roundUp(int numToRound, int multiple)
    {
        if (multiple == 0)
            return numToRound;

        int remainder = Mathf.Abs(numToRound) % multiple;
        if (remainder == 0)
            return numToRound;

        if (numToRound < 0)
            return -(Mathf.Abs(numToRound) - remainder);
        else
            return numToRound + multiple - remainder;
    }

    private void ShowAllRooms(bool show)
    {
        //Iterate layer by layer
        for (int i = 0; i < boards.Count; i++)
        {
            //Room by room
            foreach (Room room in boards[i])
            {
                room.roomObj?.SetActive(show);
            }
        }
    }
}
