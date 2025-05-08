using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnvironmentalObjectFactory : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject environmentalObjectPrefab;
    [SerializeField] private EnvironmentalObjectData[] availableObjects;
    
    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }
    
    // Using Factory pattern to create environmental objects
    public EnvironmentalObject CreateEnvironmentalObject(EnvironmentalObjectData data, Vector3 position, Vector2Int gridCoordinate)
    {
        if (environmentalObjectPrefab == null)
        {
            Debug.LogError("Environmental object prefab is not assigned!");
            return null;
        }
        
        GameObject objInstance = Instantiate(environmentalObjectPrefab, position, Quaternion.identity, transform);
        objInstance.name = $"{data.ObjectName}_{gridCoordinate.x}_{gridCoordinate.y}";
        
        EnvironmentalObject envObject = objInstance.GetComponent<EnvironmentalObject>();
        if (envObject != null)
        {
            // Initialize the environmental object
            envObject.Initialize(data);
            
            // Automatically set grid manager and coordinates via the DamageableStructure
            DamageableStructure structure = envObject as DamageableStructure;
            if (structure != null)
            {
                structure.SetCoordinate(gridCoordinate);
                structure.SetGridManager(gridManager);
            }
            
            // Mark the grid cell as occupied
            if (gridManager != null)
            {
                gridManager.SetCellOccupied(gridCoordinate, true);
            }
        }
        
        return envObject;
    }
    
    // Method to check if the object can be placed at the specified position
    public bool CanPlaceEnvironmentalObject(EnvironmentalObjectData data, Vector2Int gridCoordinate)
    {
        if (gridManager == null || data == null)
            return false;
            
        // For now we only support 1x1 objects on path cells
        if (data.Size.x != 1 || data.Size.y != 1)
            return false;
            
        // Check if this is a path cell (for mud objects)
        bool isPathCell = gridManager.GetCellType(gridCoordinate) == CellType.Path;
        if (!isPathCell)
            return false;
            
        // Check if the cell is already occupied
        return !gridManager.IsCellOccupied(gridCoordinate);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EnvironmentalObjectFactory))]
public class EnvironmentalObjectFactoryEditor : Editor
{
    private EnvironmentalObjectFactory factory;
    private int selectedObjectIndex = 0;
    private bool isPlacingObject = false;
    
    private void OnEnable()
    {
        factory = (EnvironmentalObjectFactory)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Object Placement", EditorStyles.boldLabel);
        
        // Get required properties
        SerializedProperty availableObjectsProp = serializedObject.FindProperty("availableObjects");
        SerializedProperty gridManagerProp = serializedObject.FindProperty("gridManager");
        SerializedProperty prefabProp = serializedObject.FindProperty("environmentalObjectPrefab");
        
        // Check for missing references
        if (gridManagerProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Grid Manager reference is missing!", MessageType.Error);
        }
        
        if (prefabProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Environmental Object Prefab is missing!", MessageType.Error);
        }
        
        if (availableObjectsProp != null && availableObjectsProp.arraySize > 0)
        {
            string[] objectNames = new string[availableObjectsProp.arraySize];
            for (int i = 0; i < availableObjectsProp.arraySize; i++)
            {
                SerializedProperty objectProp = availableObjectsProp.GetArrayElementAtIndex(i);
                if (objectProp != null && objectProp.objectReferenceValue != null)
                {
                    EnvironmentalObjectData objData = objectProp.objectReferenceValue as EnvironmentalObjectData;
                    objectNames[i] = objData != null ? objData.ObjectName : "Unknown Object";
                }
                else
                {
                    objectNames[i] = "Empty";
                }
            }
            
            selectedObjectIndex = EditorGUILayout.Popup("Select Object", 
                Mathf.Clamp(selectedObjectIndex, 0, objectNames.Length - 1), objectNames);
                
            if (GUILayout.Button(isPlacingObject ? "Stop Placing" : "Start Placing"))
            {
                isPlacingObject = !isPlacingObject;
                
                if (isPlacingObject)
                {
                    SceneView.duringSceneGui += OnSceneGUI;
                }
                else
                {
                    SceneView.duringSceneGui -= OnSceneGUI;
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Add some environmental objects to place.", MessageType.Info);
        }
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isPlacingObject = false;
            SceneView.duringSceneGui -= OnSceneGUI;
            Repaint();
            return;
        }
        
        SerializedProperty availableObjectsProp = serializedObject.FindProperty("availableObjects");
        SerializedProperty gridManagerProp = serializedObject.FindProperty("gridManager");
        
        if (!isPlacingObject || availableObjectsProp == null || 
            selectedObjectIndex >= availableObjectsProp.arraySize ||
            gridManagerProp.objectReferenceValue == null)
            return;
            
        SerializedProperty objectProp = availableObjectsProp.GetArrayElementAtIndex(selectedObjectIndex);
        if (objectProp == null || objectProp.objectReferenceValue == null)
            return;
            
        EnvironmentalObjectData selectedObject = objectProp.objectReferenceValue as EnvironmentalObjectData;
        if (selectedObject == null)
            return;
            
        GridManager gridManager = gridManagerProp.objectReferenceValue as GridManager;
        if (gridManager == null)
            return;
            
        // Draw preview of placement area
        DrawPlacementPreview(gridManager, selectedObject, e.mousePosition);
            
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Vector2Int cellCoords;
                gridManager = gridManagerProp.objectReferenceValue as GridManager;
                if (gridManager != null && gridManager.TryGetCellAtPosition(hit.point, out cellCoords))
                {
                    bool isPathCell = gridManager.GetCellType(cellCoords) == CellType.Path;
                    
                    // Special case for mud: allow placement on path for 1x1 objects
                    bool canPlace = isPathCell && selectedObject.Size.x == 1 && selectedObject.Size.y == 1;
                    
                    if (canPlace)
                    {
                        // Get center position for object placement
                        GameObject cellObject;
                        if (gridManager.TryGetCellObject(cellCoords, out cellObject))
                        {
                            Vector3 position = cellObject.transform.position;
                            position.y = 0.05f; // Slightly above ground
                            
                            // Get the factory component for creating the object
                            EnvironmentalObjectFactory factory = target as EnvironmentalObjectFactory;
                            if (factory != null)
                            {
                                // Register undo operations
                                Undo.IncrementCurrentGroup();
                                int group = Undo.GetCurrentGroup();
                                
                                // Create the environmental object
                                EnvironmentalObject newObject = factory.CreateEnvironmentalObject(
                                    selectedObject, position, cellCoords);
                                    
                                if (newObject != null)
                                {
                                    Undo.RegisterCreatedObjectUndo(newObject.gameObject, "Place Environmental Object");
                                    
                                    GridManager gm = gridManagerProp.objectReferenceValue as GridManager;
                                    if (gm != null)
                                    {
                                        Undo.RecordObject(gm, "Save Cell Effects");
                                        gm.SaveEffectsToSerializedData();
                                        EditorUtility.SetDirty(gm);
                                    }

                                }
                                
                                Undo.CollapseUndoOperations(group);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Cannot place object here. Mud objects must be placed on path cells.");
                    }
                }
            }
            
            e.Use();
        }
        
        // Update scene view while moving mouse
        if (e.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void DrawPlacementPreview(GridManager gridManager, EnvironmentalObjectData selectedObject, Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            Vector2Int cellCoords;
            if (gridManager.TryGetCellAtPosition(hit.point, out cellCoords))
            {
                bool isPathCell = gridManager.GetCellType(cellCoords) == CellType.Path;
                bool canPlace = isPathCell && selectedObject.Size.x == 1 && selectedObject.Size.y == 1;
                
                // Get cell position
                GameObject cellObject;
                if (gridManager.TryGetCellObject(cellCoords, out cellObject))
                {
                    // Draw preview wireframe
                    Handles.color = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
                    Vector3 cellPos = cellObject.transform.position;
                    float cellSize = gridManager.CellSize;
                    
                    // Draw box outline at cell position
                    Vector3 center = cellPos + new Vector3(0, 0.05f, 0);
                    Vector3 size = new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f);
                    Handles.DrawWireCube(center, size);
                    
                    // Draw text label
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = canPlace ? Color.green : Color.red;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 12;
                    style.fontStyle = FontStyle.Bold;
                    
                    Vector3 labelPos = cellPos + new Vector3(0, 1f, 0);
                    Handles.Label(labelPos, selectedObject.ObjectName, style);
                }
            }
        }
    }
    
    private void OnDisable()
    {
        if (isPlacingObject)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            isPlacingObject = false;
        }
    }
}
#endif