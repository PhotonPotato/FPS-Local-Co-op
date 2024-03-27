using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    //Board length.
    public int boardLength = 4;
    public int boardScale = 1;

    public int branchLengthMax = 10;
    public int branchLengthMin = 6;

    [Tooltip("Probability that a branch will occur frm each room. Has a 1 in (this value) chance.")]
    public int brachOffProbability = 4;
    [Tooltip("Probability that a branch will occur frm each room. Has a 1 in (this value) chance.")]
    public int mergeProbability = 10;

    public GameObject levelParent;
    public bool generateUnderParent;

    public float timer = 30;

    private Room[,] board;

    //Possible directions.
    List<Vector2> possibleDirs;

    private Vector2 startPlace;
    private Vector2 nullDirReference;

    private List<Vector2Int> orderedSpawns;

    public GameObject RoomPrefab;
    public GameObject HallwayPrefab;

    private void Start()
    {
        //keep the null direction reference.
        nullDirReference = new Vector2(0, 0);

        orderedSpawns = new List<Vector2Int>();

        possibleDirs = new List<Vector2>
        {
            //Add the possible directions: Up, Down, Right, Left (Except by 2 b/c there are cooridoors in between.
            new Vector2(2, 0),
            new Vector2(-2, 0),
            new Vector2(0, 2),
            new Vector2(0, -2)
        };

        InitGeneration();

        GenerateObjects();
    }

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            InitGeneration();
            timer = 10;
        }
    }

    public void InitGeneration()
    {
        List<Vector2> usedDirs = new List<Vector2>(); ;

        //Set the board up.
        board = new Room[boardLength * 2 - 1, boardLength * 2 - 1];

        for (int i = 0; i < boardLength * 2 - 1; i++)
        {
            for (int j = 0; j < boardLength * 2 - 1; j++)
            {
                board[i, j] = new Room(i, j, boardScale);
                board[i, j].status = 0;
            }
        }

        //Set a start place.
        startPlace = new Vector2(Random.Range(1, boardLength) * 2 - 1, Random.Range(1, boardLength) * 2 - 1);

        board[(int)startPlace.x, (int)startPlace.y].status = RoomStatus.Room;

        List<Vector2> usedBranchDirs = new List<Vector2>();
        usedBranchDirs.Clear();

        //Generate Base Sctructure.
        GenerateBranchFrom(startPlace, usedDirs, usedBranchDirs, branchLengthMax, branchLengthMin);

        //Now generate branches off of some of the already made rooms.
        usedBranchDirs.Clear();
        usedDirs.Clear();

        foreach (Room room in board)
        {
            if (room.status == RoomStatus.Room && Random.Range(0, brachOffProbability + 1) == 0)
            {
                GenerateBranchFrom(new Vector2(room.x, room.y), usedDirs, usedBranchDirs, 6, 3);
            }
        }

        MergeBranches();
    }

    void GenerateBranchFrom(Vector2 start, List<Vector2> usedDirs, List<Vector2> usedBranchDirs, int maxLen, int minLen)
    {
        Vector2 curPos;
        Vector2 lastDir;

        //Generates branches.
        for (int l = 4, i = 0; i < l; i++)
        {
            curPos = start;
            //Store the random dir.
            lastDir = GeneratePath(curPos, usedBranchDirs);
            curPos += lastDir;

            //Generate room.
            board[(int)curPos.x, (int)curPos.y].status = RoomStatus.Room;
            //Generate coridor.
            if (lastDir != nullDirReference)
            {
                board[(int)(curPos.x - (lastDir.x / 2)), (int)(curPos.y - (lastDir.y / 2))].status = RoomStatus.Corridor;
                board[(int)(curPos.x - (lastDir.x / 2)), (int)(curPos.y - (lastDir.y / 2))].objDir = lastDir;
            }

            //Generates how long the branches are.
            for (int k = Random.Range(minLen, maxLen), j = 0; j < k; j++)
            {
                lastDir = GeneratePath(curPos, usedDirs);
                curPos += lastDir;

                //Create the room.
                board[(int)curPos.x, (int)curPos.y].status = RoomStatus.Room;

                //Add to oreder list.
                orderedSpawns.Add(new Vector2Int((int)curPos.x, (int)curPos.y));

                //Create the corridor.
                if (lastDir != nullDirReference)
                {
                    //Set the cell in between to a hallway.
                    board[(int)(curPos.x - (lastDir.x / 2)), (int)(curPos.y - (lastDir.y / 2))].status = RoomStatus.Corridor;

                    //Set that hallway's direction
                    board[(int)(curPos.x - (lastDir.x / 2)), (int)(curPos.y - (lastDir.y / 2))].objDir = lastDir;

                    //Add the next in order.
                    orderedSpawns.Add(new Vector2Int((int)curPos.x, (int)curPos.y));
                }

                usedDirs.Clear();
                usedDirs.Add(lastDir * -1);
            }
        }
    }

    public Vector2 GeneratePath(Vector2 start, List<Vector2> dirList)
    {
        Vector2 pos = start;

        Vector2 currentDir = new Vector2();

        while (true)
        {
            //Check for a direction to go in.
            currentDir = LookForDir(currentDir, dirList);

            if (currentDir == nullDirReference) break;

            if (!BoardSpaceDoesntExistOrIsOccupied(pos + currentDir))
            {
                break;
            }
        }

        return currentDir;
    }

    bool BoardSpaceDoesntExistOrIsOccupied(Vector2 pos)
    {
        //Check if it is out of bounds.
        if (!BoardSpaceExists(pos)) return true;

        if (BoardSpaceOccupied(pos)) return true;

        return false;
    }

    private bool BoardSpaceExists(Vector2 pos)
    {
        //Check if it is out of bounds.
        if (pos.x < 0 || pos.x > boardLength * 2 - 2) return false;
        if (pos.y < 0 || pos.y > boardLength * 2 - 2) return false;

        //Then its gotta be in the bounds
        return true;
    }

    private bool BoardSpaceOccupied(Vector2 pos)
    {
        ///Check if the status of the desired board space is taken
        if (board[(int)pos.x, (int)pos.y].status == RoomStatus.Room) return true;

        return false;
    }

    Vector2 LookForDir(Vector2 currentDir, List<Vector2> usedDirs)
    {
        Vector2[] checkedDirs = new Vector2[possibleDirs.Count];

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

    void MergeBranches()
    {
        foreach (Room room in board)
        {
            if (room.status != RoomStatus.Room && Random.Range(0, mergeProbability) != Mathf.RoundToInt(mergeProbability / 2)) continue;

            //HEY FUTURE TY CAN U MAKE THESE VECTOR2INTS PLS THX

            Vector2 mergeDir;
            Vector2 pos;

            List<Vector2> usedDirs = new List<Vector2>();
            usedDirs.Clear();

            pos = new Vector2(room.x, room.y);

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
                    if (BoardSpaceOccupied(pos + mergeDir))
                    {
                        //If the CORRIDOR between the rooms is empty, fill it
                        if (board[(int)(pos.x + mergeDir.x / 2), (int)(pos.y + mergeDir.y / 2)].status == RoomStatus.EmptyRoom)
                        {
                            if(Random.Range(0, mergeProbability) == Mathf.RoundToInt(mergeProbability / 2))
                            {
                                board[Mathf.RoundToInt(pos.x + mergeDir.x / 2), Mathf.RoundToInt(pos.y + mergeDir.y / 2)].status = RoomStatus.Corridor;
                                board[Mathf.RoundToInt(pos.x + mergeDir.x / 2), Mathf.RoundToInt(pos.y + mergeDir.y / 2)].objDir = mergeDir;
                                board[Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)].status = RoomStatus.Room;
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

    public void GenerateObjects()
    {
        if (board == null)
        {
            return;
        }

        //Draw little spheres at the room places (just for testing).
        foreach (Room room in board)
        {
            switch (room.status)
            {
                case RoomStatus.Room:
                    Vector3 objPos;

                    objPos.x = room.x * boardScale;
                    objPos.y = 0;
                    objPos.z = room.y * boardScale;

                    GameObject instObj = Instantiate(RoomPrefab, objPos, Quaternion.identity);
                    if (generateUnderParent) instObj.transform.SetParent(levelParent.transform);
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
                    if (generateUnderParent) instObj2.transform.SetParent(levelParent.transform);
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (board == null)
        {
            return;
        }

        //Draw little spheres at the room places (just for testing).
        foreach (Room room in board)
        {
            switch (room.status)
            {
                case RoomStatus.Room:
                    Vector3 gizmoPos;

                    gizmoPos.x = room.x * boardScale;
                    gizmoPos.y = 0;
                    gizmoPos.z = room.y * boardScale;

                    Gizmos.DrawSphere(gizmoPos, .5f);
                    break;

                case RoomStatus.Corridor:
                    Vector3 gizmoPos2;

                    gizmoPos2.x = room.x * boardScale;
                    gizmoPos2.y = 0;
                    gizmoPos2.z = room.y * boardScale;

                    Gizmos.DrawCube(gizmoPos2, new Vector3(.5f, .5f, .5f));
                    break;
            }
        }
    }
}
