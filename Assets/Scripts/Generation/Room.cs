using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public int x;
    public int y;
    public int layer;
    public int scale;

    public RoomStatus status;

    public GameObject roomObj;
    public Vector2 objDir;

    public Vector4 roomConnections; //Just gonna b used as a boolean for North East South West
    public bool staircaseTop = false; //If this room is the top of a staircase (so it shouldn't be spawned)

    public Room(int layer, int newX, int newY, int newScale)
    {
        this.layer = layer;

        x = newX;
        y = newY;
        scale = newScale;

        status = 0;

        objDir = new Vector2(0, 0);
    }
}

public enum RoomStatus
{
    EmptyRoom,
    Room,
    Corridor,
    StaircaseRoom
}

public enum RoomType
{
    None,
    Cross,
    Elbow,
    Straight,
    Threeway,
    Single
}