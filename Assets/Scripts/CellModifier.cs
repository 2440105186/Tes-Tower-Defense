using UnityEngine;

public class CellModifier : MonoBehaviour
{
    [Header("Cell Visualizer")]
    [SerializeField] Material effectMaterial;
    
    public GridCell GridCell { get; private set; }

    protected virtual void Awake()
    {
        if (TryGetComponent<GridCell>(out var gc))
        {
            GridCell = gc;
            GridCell.isOccupied = true;
        }
        
        if (effectMaterial != null)
        {
            GetComponent<Renderer>().material = effectMaterial;
        }
    }

    public virtual void ApplyModifier(Enemy enemy)
    {
        
    }

    public virtual void RemoveModifier(Enemy enemy)
    {
        
    }
}