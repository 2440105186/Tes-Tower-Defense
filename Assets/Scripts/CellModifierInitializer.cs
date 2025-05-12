using System.Collections.Generic;
using UnityEngine;

public class CellModifierInitializer : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayerMask;

    private void Start()
    {
        InitializeRegion<Mud>();
        InitializeRegion<TallGrass>();
    }

    private void InitializeRegion<T>() where T : CellModifier
    {
        var path = GridManager.Instance.GetPathCells();
        var segments = new List<List<Vector2Int>>();
        var currentSegment = new List<Vector2Int>();

        // Group contiguous mud cells into segments
        foreach (var cellPos in path)
        {
            if (GridManager.Instance.TryGetCellObject(cellPos, out var go) &&
                go.TryGetComponent<T>(out _))
            {
                // It's a mud cell
                currentSegment.Add(cellPos);
            }
            else
            {
                // End of a contiguous segment
                if (currentSegment.Count > 0)
                {
                    segments.Add(new List<Vector2Int>(currentSegment));
                    currentSegment.Clear();
                }
            }
        }
        // Add last segment if it ends on mud
        if (currentSegment.Count > 0)
            segments.Add(currentSegment);

        // Create colliders for each segment
        foreach (var segment in segments)
        {
            CreateSegmentCollider<T>(segment);
        }
    }

    private void CreateSegmentCollider<T>(List<Vector2Int> segment) where T : CellModifier
    {
        // Calculate bounds in grid coordinates
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var pos in segment)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        GridManager.Instance.TryGetCellObject(new Vector2Int(minX, minY), out var cellMin);
        GridManager.Instance.TryGetCellObject(new Vector2Int(maxX, maxY), out var cellMax);
        
        // Convert grid bounds to world coordinates (assuming each cell is 1 unit)
        Vector3 worldMin = cellMin.transform.position;
        Vector3 worldMax = cellMax.transform.position;

        // Center and size
        Vector3 centerXZ = (worldMin + worldMax) * 0.5f;
        float sizeX = Mathf.Abs(worldMax.x - worldMin.x) + GridManager.Instance.CellSize;
        float sizeZ = Mathf.Abs(worldMax.z - worldMin.z) + GridManager.Instance.CellSize;

        // Create GameObject
        var colliderGO = new GameObject($"{typeof(T).Name}RegionCollider");
        colliderGO.transform.position = new Vector3(centerXZ.x, 1f, centerXZ.z);
        
        // Add BoxCollider
        var box = colliderGO.AddComponent<BoxCollider>();
        box.size = new Vector3(sizeX, 1f, sizeZ);
        box.center = Vector3.zero;
        box.isTrigger = true;
        box.includeLayers = enemyLayerMask;
        
        // Add CellModifier component
        colliderGO.AddComponent<T>();
    }
}