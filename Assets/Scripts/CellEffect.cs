using UnityEngine;

public class CellEffect : MonoBehaviour
{
    public GridCell GridCell { get; private set; }
    
    private void Awake()
    {
        GridCell = GetComponent<GridCell>();
    }
}
