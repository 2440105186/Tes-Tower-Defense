using UnityEngine;

public class CellModifier : MonoBehaviour
{
    [Header("Cell Visualizer")]
    [SerializeField] Material effectMaterial;
    
    public GridCell GridCell { get; private set; }

    protected virtual void Awake()
    {
        GridCell = GetComponent<GridCell>();
        GridCell.isOccupied = true;
        
        if (effectMaterial != null)
        {
            GetComponent<Renderer>().material = effectMaterial;
        }
    }
}