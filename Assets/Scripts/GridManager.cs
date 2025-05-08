using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int x = 10;
    [SerializeField] private int y = 10;
    [SerializeField] private float cellSize = 2f;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject textCoordinatePrefab;
    [SerializeField] private GameObject gatePrefab;
    
    [Header("Materials")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material pathMaterial;
    
    [Header("Path Cells")]
    [SerializeField] private List<Vector2Int> pathCellCoordinates = new List<Vector2Int>();
    
    // Grid data storage
    [SerializeField, HideInInspector] private List<GridCell> gridCells = new List<GridCell>();
    
    // Dictionary for quick lookup
    private Dictionary<Vector2Int, GridCell> cellLookup = new Dictionary<Vector2Int, GridCell>();
    
    public int GridSizeX => x;
    public int GridSizeY => y;
    public float CellSize => cellSize;
    public Material DefaultMaterial => defaultMaterial;
    public Material PathMaterial => pathMaterial;
    public GameObject GatePrefab => gatePrefab;
    public GameObject SpawnedGate {get; private set;}
    
    public List<Vector2Int> GetPathCells() => pathCellCoordinates;
    
    public bool IsCellPath(Vector2Int coordinates) => pathCellCoordinates.Contains(coordinates);
    
    private void Awake()
    {
        // Initialize the cellLookup dictionary if it's empty
        if (cellLookup.Count == 0 && gridCells.Count > 0)
        {
            InitializeCellLookup();
        }
    }

    private void InitializeCellLookup()
    {
        cellLookup.Clear();
        
        foreach (var cell in gridCells)
        {
            if (cell != null && cell.cellObject != null)
            {
                cellLookup[cell.coordinates] = cell;
            }
        }
    }
    
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        // Clear any existing child objects and collections
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        
        gridCells.Clear();
        cellLookup.Clear();
        
        // Keep existing path data
        List<Vector2Int> existingPaths = new List<Vector2Int>(pathCellCoordinates);
        pathCellCoordinates.Clear();

        // Create grid of cells
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Vector2Int coordinates = new Vector2Int(i, j);
                
                // Create a new cell GameObject
                var cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cellObject.name = $"Cell_{i}_{j}";
                cellObject.transform.parent = transform;
                cellObject.layer = cellObject.transform.parent.gameObject.layer;
                
                // Position the cell
                float posX = i * cellSize;
                float posZ = j * cellSize;
                cellObject.transform.position = new Vector3(posX, 0, posZ);
                
                // Set the scale of the cell based on cellSize
                cellObject.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);

                var text = Instantiate(textCoordinatePrefab, cellObject.transform);
                text.GetComponentInChildren<TMP_Text>().text = $"({i},{j})";
                text.transform.localPosition = new Vector3(0, 0.6f, 0);
                
                // Create and store cell data
                GridCell cell = new GridCell
                {
                    coordinates = coordinates,
                    cellObject = cellObject,
                    type = CellType.Default,
                    isOccupied = false,
                };
                
                // Check if this was a path cell before and restore it
                if (existingPaths.Contains(coordinates))
                {
                    SetCellType(coordinates, CellType.Path);
                }
                
                gridCells.Add(cell);
                cellLookup[coordinates] = cell;
                
                // Apply default material right after creation
                Renderer renderer = cellObject.GetComponent<Renderer>();
                if (renderer != null && defaultMaterial != null)
                {
                    renderer.material = defaultMaterial;
                }
            }
        }
    }
    
    [ContextMenu("Delete Grid")]
    public void DeleteGrid()
    {
        if (SpawnedGate != null)
        {
            DestroyImmediate(SpawnedGate);
            SpawnedGate = null;
        }
        
        // Clear any existing child objects and dictionary
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        gridCells.Clear();
        cellLookup.Clear();
        pathCellCoordinates.Clear();
    }
    
    public bool TryGetCellAtPosition(Vector3 worldPosition, out Vector2Int cellCoordinates)
    {
        int cellX = Mathf.FloorToInt(worldPosition.x / cellSize);
        int cellZ = Mathf.FloorToInt(worldPosition.z / cellSize);
        
        cellCoordinates = new Vector2Int(cellX, cellZ);
        
        return cellX >= 0 && cellX < x && cellZ >= 0 && cellZ < y;
    }
    
    public void SetCellType(Vector2Int coordinates, CellType type)
    {
        // Make sure coordinates are valid
        if (coordinates.x < 0 || coordinates.x >= x || coordinates.y < 0 || coordinates.y >= y)
            return;
            
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            cell.type = type;
            
            // Update the material based on cell type
            Renderer renderer = cell.cellObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                switch (type)
                {
                    case CellType.Default:
                        renderer.material = defaultMaterial;
                        // Remove from path list if present
                        pathCellCoordinates.Remove(coordinates);
                        break;
                    case CellType.Path:
                        renderer.material = pathMaterial;
                        // Always add to the path list, even if it already exists
                        // This allows creating loops by painting over existing path cells
                        pathCellCoordinates.Add(coordinates);
                        break;

                }
            }
        }
    }
    
    public bool TryGetCellObject(Vector2Int coordinates, out GameObject cellObject)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell) && cell.cellObject != null)
        {
            cellObject = cell.cellObject;
            return true;
        }
    
        cellObject = null;
        return false;
    }
    
    public CellType GetCellType(Vector2Int coordinates)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            return cell.type;
        }
        return CellType.Default;
    }
    
    public void SetCellOccupied(Vector2Int coordinates, bool occupied)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            cell.isOccupied = occupied;
        }
    }

    public bool IsCellOccupied(Vector2Int coordinates)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            return cell.isOccupied || cell.type == CellType.Path;
        }
        return true; // Default to NOT occupied for invalid cells
    }
    
    public void SetSpawnedGate(GameObject gate)
    {
        SpawnedGate = gate;
    }
}

#if UNITY_EDITOR
#endif