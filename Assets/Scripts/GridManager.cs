using System;
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
    public bool isOccupied = false;
}

[System.Serializable]
public class SerializableCellEffect
{
    public Vector2Int cellPosition;
    public CellEffectType effectType;
    public float intensity;
    public Vector3 sourcePosition; // Store position to find the source later
}

public class GridManager : MonoBehaviour
{
    #region Constants
    private const float CELL_HEIGHT = 0.1f;
    private const float TEXT_HEIGHT = 0.6f;
    #endregion

    #region Grid Settings
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
    #endregion

    #region Private Fields
    // Grid data storage
    [SerializeField, HideInInspector] private List<GridCell> gridCells = new List<GridCell>();
    
    // Dictionary for quick lookup
    private Dictionary<Vector2Int, GridCell> cellLookup = new Dictionary<Vector2Int, GridCell>();
    
    // Cell effects manager
    // Change from private field to property with lazy initialization
    private CellEffectsManager _effectsManager;
    private CellEffectsManager effectsManager 
    {
        get 
        {
            if (_effectsManager == null)
            {
                _effectsManager = new CellEffectsManager(this);
            }
            return _effectsManager;
        }
    }
    
    // Add a serialized list to store effects
    [SerializeField, HideInInspector] 
    private List<SerializableCellEffect> serializedCellEffects = new List<SerializableCellEffect>();
    #endregion

    #region Public Properties
    public int GridSizeX => x;
    public int GridSizeY => y;
    public float CellSize => cellSize;
    public Material DefaultMaterial => defaultMaterial;
    public Material PathMaterial => pathMaterial;
    public GameObject GatePrefab => gatePrefab;
    public GameObject SpawnedGate { get; private set; }
    #endregion

    #region Initialization
    private void Awake()
    {
        // Initialize the cellLookup dictionary if it's empty
        if (cellLookup.Count == 0 && gridCells.Count > 0)
        {
            InitializeCellLookup();
        }
        
        // Restore serialized effects
        RestoreSerializedEffects();
    }

    // NANTI HAPUS AJA CUMAN BUAT TES
    private void Start()
    {
        foreach (var p in pathCellCoordinates)
        {
            print($"Cell ({p.x}, {p.y}) has path effect: ");
            var fx = GetCellEffects(p);
            foreach (var e in fx)
            {
                print($"effect: {e.EffectType}");
            }
            print($"------------------------------------------------------------------");
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
    
    private void RestoreSerializedEffects()
    {
        if (serializedCellEffects == null || serializedCellEffects.Count == 0)
        {
            return;
        }

        foreach (var effect in serializedCellEffects)
        {
            // Try to find the source object based on stored position
            EnvironmentalObject source = null;
            EnvironmentalObject[] envObjects = FindObjectsByType<EnvironmentalObject>(FindObjectsSortMode.None);
            foreach (var obj in envObjects)
            {
                if (Vector3.Distance(obj.transform.position, effect.sourcePosition) < 0.1f)
                {
                    source = obj;
                    break;
                }
            }
            
            // Create a new cell effect and add it
            CellEffect cellEffect = new CellEffect(effect.effectType, effect.intensity, source);
            effectsManager.AddEffect(effect.cellPosition, cellEffect);
        }
    }
    
    public void SaveEffectsToSerializedData()
    {
        // Clear existing serialized effects
        serializedCellEffects.Clear();
    
        // If we have no effects manager or no effects, just return
        if (_effectsManager == null || _effectsManager.GetAllEffects() == null)
        {
            return;
        }
    
        // Serialize the effects
        foreach (var pair in _effectsManager.GetAllEffects())
        {
            foreach (var effect in pair.Value)
            {
                SerializableCellEffect serializedEffect = new SerializableCellEffect
                {
                    cellPosition = pair.Key,
                    effectType = effect.EffectType,
                    intensity = effect.Intensity,
                    sourcePosition = effect.Source != null ? effect.Source.transform.position : Vector3.zero
                };
            
                serializedCellEffects.Add(serializedEffect);
            }
        }
    }
    #endregion

    #region Grid Management
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearGrid();
        
        // Keep existing path data
        List<Vector2Int> existingPaths = new List<Vector2Int>(pathCellCoordinates);
        pathCellCoordinates.Clear();
        
        // Create grid of cells
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Vector2Int coordinates = new Vector2Int(i, j);
                CreateCell(i, j, coordinates, existingPaths);
            }
        }
    }

    private void CreateCell(int i, int j, Vector2Int coordinates, List<Vector2Int> existingPaths)
    {
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
        cellObject.transform.localScale = new Vector3(cellSize, CELL_HEIGHT, cellSize);
        
        // Add coordinate text
        var text = Instantiate(textCoordinatePrefab, cellObject.transform);
        text.GetComponentInChildren<TMP_Text>().text = $"({i},{j})";
        text.transform.localPosition = new Vector3(0, TEXT_HEIGHT, 0);
        
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

    [ContextMenu("Delete Grid")]
    public void DeleteGrid()
    {
        if (SpawnedGate != null)
        {
            DestroyImmediate(SpawnedGate);
            SpawnedGate = null;
        }
        
        ClearGrid();
    }

    private void ClearGrid()
    {
        // Clear any existing child objects and collections
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        
        gridCells.Clear();
        cellLookup.Clear();
        pathCellCoordinates.Clear();
        
        if (effectsManager != null)
        {
            effectsManager.ClearAllEffects();
        }
    }
    #endregion

    #region Cell Operations
    public List<Vector2Int> GetPathCells() => pathCellCoordinates;
    
    public bool IsCellPath(Vector2Int coordinates) => pathCellCoordinates.Contains(coordinates);

    public bool TryGetCellAtPosition(Vector3 worldPosition, out Vector2Int cellCoordinates)
    {
        int cellX = Mathf.FloorToInt(worldPosition.x / cellSize);
        int cellZ = Mathf.FloorToInt(worldPosition.z / cellSize);
        cellCoordinates = new Vector2Int(cellX, cellZ);
        return IsCellValid(cellCoordinates);
    }

    public void SetCellType(Vector2Int coordinates, CellType type)
    {
        // Make sure coordinates are valid
        if (!IsCellValid(coordinates))
            return;
            
        if (cellLookup.TryGetValue(coordinates, out GridCell cell))
        {
            cell.type = type;
            UpdateCellMaterial(coordinates, cell, type);
        }
    }

    private void UpdateCellMaterial(Vector2Int coordinates, GridCell cell, CellType type)
    {
        Renderer renderer = cell.cellObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            switch (type)
            {
                case CellType.Default:
                    renderer.material = defaultMaterial;
                    pathCellCoordinates.Remove(coordinates);
                    break;
                case CellType.Path:
                    renderer.material = pathMaterial;
                    if (!pathCellCoordinates.Contains(coordinates))
                    {
                        pathCellCoordinates.Add(coordinates);
                    }
                    break;
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
        return true; // Default to NOT available for invalid cells
    }
    
    private bool IsCellValid(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.x < x && coordinates.y >= 0 && coordinates.y < y;
    }
    #endregion

    #region Gate Management
    public void SetSpawnedGate(GameObject gate)
    {
        SpawnedGate = gate;
    }
    #endregion

    #region Line of Sight
    public bool HasLineOfSight(Vector3 fromPosition, Vector3 toPosition, bool hasThermalVision = false)
    {
        EnvironmentalObject[] envObjects = FindObjectsByType<EnvironmentalObject>(FindObjectsSortMode.None);
        foreach (var obj in envObjects)
        {
            if (!obj.CheckLineOfSight(fromPosition, toPosition, hasThermalVision))
            {
                return false;
            }
        }
        return true;
    }
    #endregion

    #region Cell Effects API
    public void AddCellEffect(Vector2Int cellPos, CellEffect effect)
    {
        effectsManager.AddEffect(cellPos, effect);
    }

    public void RemoveCellEffectsFromSource(Vector2Int cellPos, EnvironmentalObject source)
    {
        effectsManager.RemoveEffectsFromSource(cellPos, source);
    }

    public List<CellEffect> GetCellEffects(Vector2Int cellPos)
    {
        return effectsManager.GetEffects(cellPos);
    }

    public bool CellHasEffectType(Vector2Int cellPos, CellEffectType effectType)
    {
        return effectsManager.HasEffectType(cellPos, effectType);
    }

    public float GetCellMovementModifier(Vector2Int cellPos)
    {
        return effectsManager.GetMovementModifier(cellPos);
    }

    public void UpdateCellVisual(Vector2Int cellPos)
    {
        effectsManager.UpdateCellVisual(cellPos);
    }
    #endregion
}

public class CellEffectsManager
{
    private readonly GridManager gridManager;
    private Dictionary<Vector2Int, List<CellEffect>> cellEffects = new Dictionary<Vector2Int, List<CellEffect>>();

    public CellEffectsManager(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }
    
    public Dictionary<Vector2Int, List<CellEffect>> GetAllEffects()
    {
        return cellEffects;
    }

    public void AddEffect(Vector2Int cellPos, CellEffect effect)
    {
        if (!cellEffects.ContainsKey(cellPos))
        {
            cellEffects[cellPos] = new List<CellEffect>();
        }
        cellEffects[cellPos].Add(effect);
        UpdateCellVisual(cellPos);
    }

    public void RemoveEffectsFromSource(Vector2Int cellPos, EnvironmentalObject source)
    {
        if (cellEffects.ContainsKey(cellPos))
        {
            cellEffects[cellPos].RemoveAll(e => e.Source == source);
            if (cellEffects[cellPos].Count == 0)
            {
                cellEffects.Remove(cellPos);
            }
            UpdateCellVisual(cellPos);
        }
    }

    public List<CellEffect> GetEffects(Vector2Int cellPos)
    {
        if (cellEffects.ContainsKey(cellPos))
        {
            return new List<CellEffect>(cellEffects[cellPos]);
        }
        return new List<CellEffect>();
    }

    public bool HasEffectType(Vector2Int cellPos, CellEffectType effectType)
    {
        if (cellEffects.ContainsKey(cellPos))
        {
            return cellEffects[cellPos].Exists(e => e.EffectType == effectType);
        }
        return false;
    }

    public float GetMovementModifier(Vector2Int cellPos)
    {
        float modifier = 1.0f;
        if (cellEffects.ContainsKey(cellPos))
        {
            if (cellEffects[cellPos].Exists(e => e.EffectType == CellEffectType.MovementBlocker))
            {
                return 0.0f;
            }
            foreach (var effect in cellEffects[cellPos])
            {
                if (effect.EffectType == CellEffectType.MovementModifier)
                {
                    modifier *= effect.Intensity;
                }
            }
        }
        return modifier;
    }

    public void UpdateCellVisual(Vector2Int cellPos)
    {
        if (gridManager.TryGetCellObject(cellPos, out GameObject cellObject))
        {
            Renderer renderer = cellObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (gridManager.GetCellType(cellPos) == CellType.Path)
                {
                    // Path cells maintain their path material
                    renderer.material = gridManager.PathMaterial;
                }
                else
                {
                    ApplyEffectVisual(cellPos, renderer);
                }
            }
        }
    }

    private void ApplyEffectVisual(Vector2Int cellPos, Renderer renderer)
    {
        if (HasEffectType(cellPos, CellEffectType.MovementBlocker))
        {
            renderer.material.color = Color.red;
        }
        else if (HasEffectType(cellPos, CellEffectType.MovementModifier))
        {
            renderer.material.color = Color.blue;
        }
        else if (HasEffectType(cellPos, CellEffectType.LineOfSightBlocker))
        {
            renderer.material.color = Color.gray;
        }
        else if (HasEffectType(cellPos, CellEffectType.ThermalVisionBlocker))
        {
            renderer.material.color = new Color(1.0f, 0.5f, 0f); // Orange
        }
        else
        {
            renderer.material = gridManager.DefaultMaterial;
            renderer.material.color = Color.white;
        }
    }

    public void ClearAllEffects()
    {
        cellEffects.Clear();
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
    private Vector2Int lastPaintedCell = new Vector2Int(-1, -1);
    private Vector2Int lastProcessedCell = new Vector2Int(-1, -1);
    
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
            lastPaintedCell = new Vector2Int(-1, -1);
            SceneView.duringSceneGui += OnSceneGUI;
        }
        if (GUILayout.Button("Stop Painting"))
        {
            isPainting = false;
            SceneView.duringSceneGui -= OnSceneGUI;
            // Spawn gate at the last painted cell position
            if (lastPaintedCell.x != -1 && gridManager.GatePrefab != null)
            {
                SpawnGateAtLastCell();
            }
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
            EditorGUILayout.HelpBox("Click on cells to paint/erase paths. Press Escape to exit paint mode.",
                MessageType.Info);
        }
    }
    
    private void SpawnGateAtLastCell()
    {
        // Get the gate prefab from the GridManager
        GameObject gatePrefab = gridManager.GatePrefab;
        if (gatePrefab == null)
        {
            Debug.LogError("Gate prefab is not set in the Grid Manager!");
            return;
        }
        // Delete any existing gate
        if (gridManager.SpawnedGate != null)
        {
            Undo.DestroyObjectImmediate(gridManager.SpawnedGate);
        }
        // Get the world position of the last painted cell (centered)
        gridManager.TryGetCellObject(lastPaintedCell, out GameObject cellObject);
        Vector3 gatePosition = cellObject.transform.position;
        // Create the gate prefab
        GameObject gate = PrefabUtility.InstantiatePrefab(gatePrefab) as GameObject;
        if (gate != null)
        {
            Undo.RegisterCreatedObjectUndo(gate, "Create Gate");
            // Position the gate
            gate.transform.position = gatePosition;
            gate.name = "Gate";
            // Make the gate a child of the grid manager
            gate.transform.SetParent(gridManager.transform);
            // Store the reference to the gate
            Undo.RecordObject(gridManager, "Set Gate Reference");
            gridManager.SetSpawnedGate(gate);
            EditorUtility.SetDirty(gridManager);
            // Add a special end marker to the path
            List<Vector2Int> pathCells = gridManager.GetPathCells();
            if (!pathCells.Contains(new Vector2Int(-1, -1)))
            {
                SerializedObject so = new SerializedObject(gridManager);
                SerializedProperty pathCellsProp = so.FindProperty("pathCellCoordinates");
                int endIndex = pathCellsProp.arraySize;
                pathCellsProp.InsertArrayElementAtIndex(endIndex);
                SerializedProperty endElement = pathCellsProp.GetArrayElementAtIndex(endIndex);
                // Set the end marker position (-1, -1)
                var xProp = endElement.FindPropertyRelative("x");
                var yProp = endElement.FindPropertyRelative("y");
                if (xProp != null && yProp != null)
                {
                    xProp.intValue = -1;
                    yProp.intValue = -1;
                }
                so.ApplyModifiedProperties();
            }
            Debug.Log($"Gate spawned at cell ({lastPaintedCell.x}, {lastPaintedCell.y})");
        }
        else
        {
            Debug.LogError("Failed to instantiate Gate prefab!");
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
                        // Only process the cell if it's different from the last processed cell
                        // This prevents adding the same cell multiple times during a drag operation
                        if (!cellCoords.Equals(lastProcessedCell))
                        {
                            // Only make an Undo record for the first change in a drag sequence
                            if (e.type == EventType.MouseDown)
                            {
                                Undo.RecordObject(gridManager, "Paint Cell");
                            }

                            // Only update if not erasing - we want to track the last painted path cell
                            if (!eraserMode)
                            {
                                lastPaintedCell = cellCoords;
                            }

                            // Call SetCellType to update the cell's type and appearance
                            gridManager.SetCellType(cellCoords, paintType);
                            EditorUtility.SetDirty(gridManager);

                            // Update the last processed cell
                            lastProcessedCell = cellCoords;
                        }
                    }
                }
            }

            e.Use(); // Consume the event
        }

        // Reset lastProcessedCell when mouse is released
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            lastProcessedCell = new Vector2Int(-1, -1);
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
        // Delete any existing gate
        if (gridManager.SpawnedGate != null)
        {
            Undo.RecordObject(gridManager, "Clear Gate Reference");
            Undo.DestroyObjectImmediate(gridManager.SpawnedGate);
            gridManager.SetSpawnedGate(null);
        }

        // First, collect all cells that are currently paths
        List<Vector2Int> currentPathCells = new List<Vector2Int>(gridManager.GetPathCells());

        // Clear the path cell coordinates array
        var pathCellsProperty = serializedObject.FindProperty("pathCellCoordinates");
        pathCellsProperty.ClearArray();
        serializedObject.ApplyModifiedProperties();

        // Now update the visuals for each cell that was previously a path
        foreach (Vector2Int pathCell in currentPathCells)
        {
            // Skip the special end marker (-1, -1)
            if (pathCell.x >= 0 && pathCell.y >= 0)
            {
                // Get the cell and reset its visual
                if (gridManager.TryGetCellObject(pathCell, out GameObject cellObject))
                {
                    Renderer renderer = cellObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = gridManager.DefaultMaterial;
                    }
                }
            }
        }

        // Update cell type data for all cells
        for (int i = 0; i < gridManager.GridSizeX; i++)
        {
            for (int j = 0; j < gridManager.GridSizeY; j++)
            {
                Vector2Int coordinates = new Vector2Int(i, j);
                // Update the cell type in the data structure
                gridManager.SetCellType(coordinates, CellType.Default);
            }
        }

        EditorUtility.SetDirty(gridManager);
    }
}
#endif