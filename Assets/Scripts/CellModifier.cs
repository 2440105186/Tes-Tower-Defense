using UnityEngine;

public class CellModifier : CellEffect
{
    [Header("Cell Visualizer")]
    [SerializeField] Material effectMaterial;
    
    protected void Start()
    {
        if (effectMaterial != null)
        {
            GetComponent<Renderer>().material = effectMaterial;
        }
    }
}