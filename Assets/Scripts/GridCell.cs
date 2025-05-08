using UnityEngine;

public enum CellType
{
    Default,
    Path
}

[System.Serializable]
public class GridCell
{
    public Vector2Int coordinates;
    public CellType type = CellType.Default;
    public GameObject cellObject;
    public bool isOccupied = false;
}