using UnityEngine;

public enum CellType
{
    Default,
    Path
}

[System.Serializable]
public class GridCell : MonoBehaviour
{
    public Vector2Int coordinates;
    public CellType type = CellType.Default;
    public bool isOccupied = false;
}