using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public int x;
    public int y;
    public int scale;

    public RoomStatus status;

    public GameObject roomObj;
    public Vector2 objDir;

    public Room(int newX, int newY, int newScale)
    {
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
    Corridor
}