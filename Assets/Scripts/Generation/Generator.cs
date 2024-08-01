using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using JetBrains.Annotations;
using Unity.VisualScripting;
using static Generator;

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
    
    public struct EndOfBranchCoords
    {
        public Vector2Int roomCoord;
        public int layer;
        public int branchLength;
        public float distToOrigin;
    }

    [System.Serializable]
    public struct RoomPrefab
    {
        public RoomType type;
        public GameObject prefab;
        public float difficulty;

        //Saved to track the difference between this difficulty and the level's
        [System.NonSerialized] public float difficultyError;
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

    [Tooltip("How close a room prefab's difficulty must be to the desired difficulty in order for it to be considered in generation")]
    public float roomDifficultyErrorMargin = .4f;

    [Header("Refs")]
    public bool generateUnderParent;
    public GameObject[] levelParent;

    public GameObject[] SingleRooms;
    public GameObject[] ElbowRooms;
    public GameObject[] CrossRooms;
    public GameObject[] StraightRooms;
    public GameObject[] ThreeWayRooms;
    public GameObject[] ExtractRooms;

    public RoomPrefab[] AllRoomPrefabs;

    public GameObject[] Corridoors;

    public NavMeshSurface surface;


    [Header("Trackers")]
    public float currentRoomDifficulty;
    public float currentEnemyDifficulty;

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

    public List<EndOfBranchCoords> possibleExtractSpawnPoints;

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
            GetDifficultySettings();

            InitBoard();
            
            //Gerenerate the origin room
            GenerateNewStartPoint();
            
            //run the main chunk of generating branches
            InitGeneration(0, startPlace, false);

            //Before merging determine the extract room and any other rooms of importance
            EndOfBranchCoords extractRoom = ChoseExtractRoom();

            Debug.Log($"Extract Coord: {extractRoom.roomCoord}");

            boards[extractRoom.layer][extractRoom.roomCoord.x, extractRoom.roomCoord.y].status = RoomStatus.Extract;

            //Now attempt to merge the remaining branches (exclding the extract room)
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

            CalculateAllRoomPrefabsDifficultyError();

            PrepRoomGeneration();

            ShowAllRooms(true);
            surface?.BuildNavMesh();

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
        possibleExtractSpawnPoints = new List<EndOfBranchCoords>();

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

                //If it is the last room in the branch
                if (j == k-1)
                {
                    // For some reason it sometimes chooses the start room to spawn the extract so for now just
                    // don't let it set the start place to a possible extract room. :) cheeze fix
                    if (curPos != startPlace)
                    {
                        //Log this room and how long this branch was for generating extracts and secrets
                        EndOfBranchCoords thisRoomCoord = new EndOfBranchCoords();
                        thisRoomCoord.roomCoord = curPos;
                        thisRoomCoord.layer = layer;
                        thisRoomCoord.branchLength = k;
                        thisRoomCoord.distToOrigin = Vector2Int.Distance(curPos, startPlace);
                        possibleExtractSpawnPoints.Add(thisRoomCoord);
                    }

                    //Check for stairwell creation (after corridoors are generated bc this shit bouta get recursive)
                    if (generateStaircases)
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
        RoomStatus room = boards[layer][pos.x, pos.y].status;

        ///Check if the status of the desired board space is taken
        if (room == RoomStatus.Room || room == RoomStatus.StaircaseRoom || room == RoomStatus.Extract) return true;

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

    private void MergeBranches(int layer)
    {
        foreach (Room room in boards[layer])
        {
            if ((room.status != RoomStatus.Room || room.status == RoomStatus.Extract) 
                && Random.Range(0, mergeProbability) != Mathf.RoundToInt(mergeProbability / 2)) continue;
            
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
                        //We want to keep extract rooms as singles so don't try to connect to it
                        if (boards[layer][pos.x + mergeDir.x, pos.y + mergeDir.y].status == RoomStatus.Extract) break;
                        if (boards[layer][pos.x + mergeDir.x, pos.y + mergeDir.y].status == RoomStatus.Extract) break;

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

                switch (room.status)
                {
                    case RoomStatus.Room:
                        RoomOrientationData roomData = IdentifyRoomOrientation(i, new Vector2Int(room.x, room.y));

                        obj = Instantiate(GetRoomPrefabByDifficulty(roomData.type, roomDifficultyErrorMargin), levelParent[i].transform);

                        obj.transform.SetPositionAndRotation(new Vector3(room.x * boardScale, i * .75f * boardScale, room.y * boardScale), Quaternion.Euler(0, roomData.rotation, 0));

                        break;

                    //Extracts can only be singles
                    case RoomStatus.Extract:
                        RoomOrientationData roomOrientationData = IdentifyRoomOrientation(i, new Vector2Int(room.x, room.y));

                        obj = Instantiate(ExtractRooms[Random.Range(0, ExtractRooms.Length)], levelParent[i].transform);

                        obj.transform.SetPositionAndRotation(new Vector3(room.x * boardScale, i * .75f * boardScale, room.y * boardScale), Quaternion.Euler(0, roomOrientationData.rotation, 0));

                        break;

                    case RoomStatus.StaircaseRoom:
                        obj = new GameObject();
                        break;

                    case RoomStatus.Corridor:
                        obj = Instantiate(Corridoors[Random.Range(0, Corridoors.Length)], levelParent[i].transform);

                        obj.transform.SetPositionAndRotation(new Vector3(room.x * boardScale, i * .75f * boardScale, room.y * boardScale), Quaternion.Euler(0, room.objDir.y == 0 ? 90 : 0, 0));
                        break;

                    // This is just to make the compiler happy, should never actually get tripped.
                    // Compiler wanted to make sure that obj gets set no matter what.
                    default:
                        obj = new GameObject();
                        break;
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

    public void ShowAllRooms(bool show)
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

    /// <summary>
    /// Returns the room at the end of the longest branch
    /// </summary>
    /// <returns>output</returns>
    public EndOfBranchCoords ChoseExtractRoom()
    {
        EndOfBranchCoords output = new EndOfBranchCoords();
        output.branchLength = -1;

        for (int i = 0; i < possibleExtractSpawnPoints.Count; i++)
        {
            //Compare the branch lengths
            if (possibleExtractSpawnPoints[i].branchLength > output.branchLength)
            {
                //If this possible extract is at the end of the longest branch, use it as the extract.
                output = possibleExtractSpawnPoints[i];
            }
            else if (possibleExtractSpawnPoints[i].branchLength == output.branchLength)
            {
                //if they are equal, choose the farther away one
                if (possibleExtractSpawnPoints[i].distToOrigin > output.distToOrigin)
                {
                    output = possibleExtractSpawnPoints[i];
                }
            }
        }

        return output;
    }

    /// <summary>
    /// Reads from level difficulty file to determine the current generation and enemy difficulty
    /// </summary>
    public void GetDifficultySettings()
    {
        // ALL of the integer values for generation will be stored as floats so that they can be increased incrementally
        // Since the actual variables are integers, we will just round down
        // TBH the implementation using a temp float IS NOT ELEGANT :( but performance here doesn't matter
        // Still room for improvement here though.
        float tmpFloat;

        //The 2nd line holds the overall room difficulty
        float.TryParse(GetSaveDataLine(2), out tmpFloat);
        currentRoomDifficulty = tmpFloat;

        //4th and 5th lines contain the min and max branch lengths
        float.TryParse(GetSaveDataLine(4), out tmpFloat);
        branchLengthMax = Mathf.FloorToInt(tmpFloat);

        float.TryParse(GetSaveDataLine(5), out tmpFloat);
        branchLengthMin = Mathf.FloorToInt(tmpFloat);

        //Line 7 and 8 represent the branch off and merge probability of the generation
        float.TryParse(GetSaveDataLine(7), out tmpFloat);
        branchOffProbability = Mathf.FloorToInt(tmpFloat);

        float.TryParse(GetSaveDataLine(8), out tmpFloat);
        mergeProbability = Mathf.FloorToInt(tmpFloat);

        //Lines 10 and 11 contain the maximum number of branches and floors
        float.TryParse(GetSaveDataLine(10), out tmpFloat);
        maximumNumBranches = Mathf.FloorToInt(tmpFloat);

        //THIS IS WHERE I START TO OVRELOAD THIS SCRIPT LOL, RLY SHOULD PUT ENEMY STUFF ELSEWHERE

        //Line 13 represents the current overall enemy spawn difficulty
        //This will get sent to the EventsManager which will release it to the graveyards and other
        float.TryParse(GetSaveDataLine(13), out tmpFloat);
        currentEnemyDifficulty = Mathf.FloorToInt(tmpFloat);

        EventsManager.SharedInstance.currentRoomDifficulty = currentRoomDifficulty;
        EventsManager.SharedInstance.currentEnemyDifficulty = currentEnemyDifficulty;
    }

    public void UpdateDifficultySettingsForExtract(float overallDiffIncrement, float branchLengthIncrement, float probabilityIncrement)
    {
        //Write to file
        var lines = File.ReadAllLines(GetFilePath("LevelDifficulty.txt"));

        //Update the room difficulty
        lines[1] = Mathf.Clamp(currentRoomDifficulty + overallDiffIncrement, .3f, 1.5f).ToString();

        //Update the branch length max and min
        lines[3] = Mathf.Clamp(float.Parse(lines[3]) + branchLengthIncrement, 1.0f, 3.0f).ToString();
        //lines[4] = (float.Parse(lines[4]) + branchLengthIncrement).ToString();

        //Upfate the branch and merge probability
        lines[6] = Mathf.Clamp(float.Parse(lines[6]) + probabilityIncrement, 2.5f, 5.5f).ToString();
        lines[7] = Mathf.Clamp(float.Parse(lines[7]) + probabilityIncrement, 1.5f, 4.5f).ToString();

        //Update the max number of branches and floors
        //FUTURE TY CAN DEAL WITH THIS LOL

        File.WriteAllLines(GetFilePath("LevelDifficulty.txt"), lines);
    }

    /// <summary>
    /// Gets a specific line of the desired text file.
    /// Getting used so much that it might as well just cache the whole file and then read from the cache instead of reading the file so much.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="line"></param>
    /// <returns></returns>
    string GetSaveDataLine(int line, string fileName = "Assets/Settings/SaveData/LevelDifficulty.txt")
    {
        using (var sr = new StreamReader(fileName))
        {
            for (int i = 1; i < line; i++)
                sr.ReadLine();
            return sr.ReadLine();
        }
    }

    /// <summary>
    /// A quick once over of the AllRoomPrefabs list to determine each room's error to the current room difficulty.
    /// Should only be run after getting the current room difficulty and before running any room prefab gen.
    /// </summary>
    public void CalculateAllRoomPrefabsDifficultyError()
    {
        for (int i = 0; i < AllRoomPrefabs.Length; i++)
        {
            AllRoomPrefabs[i].difficultyError = Mathf.Abs(currentRoomDifficulty - AllRoomPrefabs[i].difficulty);
        }
    }

    /// <summary>
    /// Somehow determines a good room based on the desired difficulty
    /// </summary>
    /// <param name="difficulty"></param>
    /// <returns></returns>
    public GameObject GetRoomPrefabByDifficulty(RoomType type, float errorMargin)
    {
        List<RoomPrefab> viableRoomPrefabs = new List<RoomPrefab>();
        List<RoomPrefab> viableRoomPrefabsByType = new List<RoomPrefab>();

        //Keep in mind that the actual room difficulty error should have been calculated prior to this.

        //Start by filtering for all rooms of the right type
        foreach (RoomPrefab roomPrefab in AllRoomPrefabs)
        {
            if (roomPrefab.type == type)
            {
                //Now filter for difficulty by an error margin
                if (roomPrefab.difficultyError <= errorMargin)
                {
                    //Then the room is a viable candidate
                    viableRoomPrefabs.Add(roomPrefab);
                }
                
                //Put the room into a bin of rooms that passed the type filter
                viableRoomPrefabsByType.Add(roomPrefab);
            }
        }

        //A quick check to make sure that there IS a room of such type even
        if (viableRoomPrefabsByType.Count == 0) return null;

        //A quick check to make sure that we even have a viable candidate
        if (viableRoomPrefabs.Count > 0)
        {
            //Return one of the room prefabs thats within the error margin.
            RoomPrefab roomPrefab = viableRoomPrefabs[Random.Range(0, viableRoomPrefabs.Count)];

            Debug.Log($"Rand. D: {currentRoomDifficulty}, Room D: {roomPrefab.difficulty}, Error: {roomPrefab.difficultyError}, Name: {roomPrefab.prefab.name}");

            return roomPrefab.prefab;
        }
        else
        {
            //Oop looks like there's no truly viable room.
            //Sooo we're just gonna choose the next best thing
            RoomPrefab bestRoomPrefabSoFar = viableRoomPrefabsByType[0];

            for (int i = 1; i < viableRoomPrefabsByType.Count; i++)
            {
                if (viableRoomPrefabsByType[i].difficultyError < bestRoomPrefabSoFar.difficultyError) bestRoomPrefabSoFar = viableRoomPrefabsByType[i];
            }

            Debug.Log($"Best. D: {currentRoomDifficulty}, Room D: {bestRoomPrefabSoFar.difficulty}, Error: {bestRoomPrefabSoFar.difficultyError}, Name: {bestRoomPrefabSoFar.prefab.name}");

            //Send it out baby
            return bestRoomPrefabSoFar.prefab;
        }
    }

    string GetFilePath(string fileName)
    {
        return Path.Combine(Application.streamingAssetsPath, fileName);
    }
}
