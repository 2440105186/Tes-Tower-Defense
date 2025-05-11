using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private GridManager gridManager;
    private bool isPainting = false;
    private CellType paintType = CellType.Path;
    private bool eraserMode = false;
    
    private Vector2Int lastPaintedCell = new Vector2Int(-1, -1);
    private Vector2Int lastProcessedCell = new Vector2Int(-1, -1);

    private const float GATE_SPAWN_OFFSET_Y = 1f;
    
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
            EditorGUILayout.HelpBox("Click on cells to paint/erase paths. Press Escape to exit paint mode.", MessageType.Info);
        }
    }
    
    // In the SpawnGateAtLastCell method:
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
            gate.transform.position = gatePosition + Vector3.up * GATE_SPAWN_OFFSET_Y;
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