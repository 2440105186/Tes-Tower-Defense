using UnityEngine;

public class LineOfSightBlocker : CellModifier
{
    [Header("Line of Sight Blocker Settings")]
    [SerializeField] float spawnOffsetY = 1f;
    [SerializeField] private GameObject blockerPrefab;
    [SerializeField] private LayerMask blockerLayerMask;
    
    protected override void Awake()
    {
        base.Awake();
        
        GridCell.isOccupied = true;

        if (blockerPrefab != null)
        {
            var blocker = Instantiate(blockerPrefab, transform.position + (Vector3.up * spawnOffsetY), Quaternion.identity);
            var cellSize = GridManager.Instance.CellSize;
            blocker.transform.parent = transform;
            int layerIndex = Mathf.RoundToInt(Mathf.Log(blockerLayerMask.value, 2));
            blocker.layer = layerIndex;
        }
    }
}