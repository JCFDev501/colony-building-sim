using UnityEngine;

[System.Serializable]
public class GridTile
{
    public Vector2Int Coordinates;
    public Vector3 WorldPosition;
    public bool IsOccupied;

    public GridTile(Vector2Int coordinates, Vector3 worldPosition)
    {
        Coordinates = coordinates;
        WorldPosition = worldPosition;
        IsOccupied = false;
    }
}