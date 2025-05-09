using UnityEngine;

public class CellEffect : MonoBehaviour
{
    private GridCell gridCell;
    
    private void Awake()
    {
        gridCell = GetComponent<GridCell>();
    }

    private void Start()
    {
        
    }
}
