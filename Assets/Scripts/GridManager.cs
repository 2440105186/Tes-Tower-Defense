using TMPro;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public bool isOccupied = false; // Track if a tower is placed on this cell
}

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int x = 10;
    [SerializeField] private int y = 10;
    [SerializeField] private float cellSize = 2f;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject textCoordinatePrefab;
    
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

    /// <summary>
    /// Populates the cellLookup dictionary from the existing gridCells list
    /// </summary>
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
        // Clear any existing child objects and dictionary
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        gridCells.Clear();
        cellLookup.Clear();
        
        // Note: We're keeping the pathCellCoordinates intact
    }
    
    // Helper method to convert world position to grid cell coordinates
    public bool TryGetCellAtPosition(Vector3 worldPosition, out Vector2Int cellCoordinates)
    {
        int cellX = Mathf.FloorToInt(worldPosition.x / cellSize);
        int cellZ = Mathf.FloorToInt(worldPosition.z / cellSize);
        
        cellCoordinates = new Vector2Int(cellX, cellZ);
        
        return cellX >= 0 && cellX < x && cellZ >= 0 && cellZ < y;
    }
    
    // Set the type of a cell and update its visual appearance
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
                        // Add to path list if not already there
                        if (!pathCellCoordinates.Contains(coordinates))
                        {
                            pathCellCoordinates.Add(coordinates);
                        }
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Try to get the GameObject for a specific cell
    /// </summary>
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
    
    // Get the type of a cell
    public CellType GetCellType(Vector2Int coordinates)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            return cell.type;
        }
        return CellType.Default;
    }
    
    /// <summary>
    /// Set the occupied state of a cell
    /// </summary>
    /// <param name="coordinates">The coordinates of the cell</param>
    /// <param name="occupied">Whether the cell is occupied</param>
    public void SetCellOccupied(Vector2Int coordinates, bool occupied)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            cell.isOccupied = occupied;
        }
    }

    /// <summary>
    /// Check if a cell is occupied
    /// </summary>
    /// <param name="coordinates">The coordinates of the cell</param>
    /// <returns>True if the cell is occupied or is a path, false otherwise</returns>
    public bool IsCellOccupied(Vector2Int coordinates)
    {
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            return cell.isOccupied || cell.type == CellType.Path;
        }
        return true; // Default to NOT occupied for invalid cells
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private GridManager gridManager;
    private bool isPainting = false;
    private CellType paintType = CellType.Path;
    private bool eraserMode = false;
    
    private void OnEnable()
    {
        gridManager = (GridManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Grid Tools", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Generate Grid"))
        {
            gridManager.GenerateGrid();
        }
        
        if (GUILayout.Button("Delete Grid"))
        {
            gridManager.DeleteGrid();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Path Painting Tools", EditorStyles.boldLabel);
        
        // Paint tools
        EditorGUILayout.BeginHorizontal();
        
        // Choose paint type or eraser
        eraserMode = EditorGUILayout.ToggleLeft("Eraser Mode", eraserMode, GUILayout.Width(120));
        paintType = eraserMode ? CellType.Default : CellType.Path;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Start Painting"))
        {
            isPainting = true;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        if (GUILayout.Button("Stop Painting"))
        {
            isPainting = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Clear all paths button
        if (GUILayout.Button("Clear All Paths"))
        {
            Undo.RecordObject(gridManager, "Clear All Paths");
            ClearAllPaths();
            EditorUtility.SetDirty(gridManager);
        }
        
        if (isPainting)
        {
            EditorGUILayout.HelpBox("Click on cells to paint/erase paths. Press Escape to exit paint mode.", MessageType.Info);
        }
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        // Exit paint mode if Escape is pressed
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            isPainting = false;
            SceneView.duringSceneGui -= OnSceneGUI;
            Repaint();
            return;
        }
        
        if (!isPainting) return;

        Event e = Event.current;
        
        // Track the last cell we painted to avoid repainting the same cell multiple times
        Vector2Int? lastPaintedCell = null;
        
        // Handle both MouseDown and MouseDrag events for continuous painting
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            // Handle painting on mouse click or drag
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check if this is a grid cell
                if (hit.transform.parent == gridManager.transform)
                {
                    Vector2Int cellCoords;
                    if (gridManager.TryGetCellAtPosition(hit.transform.position, out cellCoords))
                    {
                        // Skip if we just painted this cell (for smoother painting)
                        if (lastPaintedCell.HasValue && lastPaintedCell.Value == cellCoords)
                            return;
                            
                        // Only make an Undo record for the first change in a drag sequence
                        if (e.type == EventType.MouseDown)
                        {
                            Undo.RecordObject(gridManager, "Paint Cell");
                        }
                        
                        gridManager.SetCellType(cellCoords, paintType);
                        lastPaintedCell = cellCoords;
                        EditorUtility.SetDirty(gridManager);
                    }
                }
            }
            
            e.Use(); // Consume the event
        }
        
        // Make cursor painting feel responsive by updating on mouse move
        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
        {
            SceneView.RepaintAll();
        }
        
        // Force repaint to ensure smooth painting during mouse drag
        if (e.type == EventType.Layout)
        {
            HandleUtility.Repaint();
        }
    }
    
    private void ClearAllPaths()
    {
        var pathCellsProperty = serializedObject.FindProperty("pathCellCoordinates");
        pathCellsProperty.ClearArray();
        serializedObject.ApplyModifiedProperties();
        
        // Update visual state - set all cells to default
        for (int i = 0; i < gridManager.GridSizeX; i++)
        {
            for (int j = 0; j < gridManager.GridSizeY; j++)
            {
                gridManager.SetCellType(new Vector2Int(i, j), CellType.Default);
            }
        }
    }
}
#endif