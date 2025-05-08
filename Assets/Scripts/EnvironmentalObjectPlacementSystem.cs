using UnityEngine;
using System.Collections.Generic;

public class EnvironmentalObjectPlacementSystem : GridTargetingSystem
{
    [Header("Environmental Object Data")]
    [SerializeField] private List<EnvironmentalObjectData> availableObjectTypes = new List<EnvironmentalObjectData>();
    [SerializeField] private int selectedObjectIndex = 0;
    [SerializeField] private EnvironmentalObjectFactory objectFactory;
    [SerializeField] private KeyCode toggleKey = KeyCode.P; // 'P' for Painting
    
    private bool isPlacementActive = false;
    
    public EnvironmentalObjectData CurrentObjectData => 
        availableObjectTypes.Count > selectedObjectIndex ? availableObjectTypes[selectedObjectIndex] : null;
    
    protected override void Start()
    {
        base.Start();
        
        if (objectFactory == null)
        {
            objectFactory = FindFirstObjectByType<EnvironmentalObjectFactory>();
            if (objectFactory == null)
            {
                Debug.LogError("No EnvironmentalObjectFactory found in scene! Creating environmental objects will not work.");
            }
        }
        
        // Start with placement inactive
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }
    
    protected override void CreatePreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        
        EnvironmentalObjectData objectData = CurrentObjectData;
        if (objectData == null || objectData.ModelPrefab == null) return;
        
        previewObject = new GameObject("EnvironmentalObjectPreview");
        
        GameObject modelInstance = Instantiate(objectData.ModelPrefab, previewObject.transform);
        modelInstance.transform.localPosition = Vector3.zero;
        
        // Apply preview material to all renderers
        foreach (var renderer in previewObject.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = validPlacementMaterial;
            }
            renderer.materials = materials;
        }
        
        previewObject.SetActive(false);
    }
    
    public void SelectObjectType(int index)
    {
        if (index >= 0 && index < availableObjectTypes.Count)
        {
            selectedObjectIndex = index;
            CreatePreview();
            
            if (isPlacementActive && previewObject != null)
            {
                previewObject.SetActive(true);
            }
        }
    }
    
    protected override void Update()
    {
        // Toggle placement mode with the toggle key
        if (Input.GetKeyDown(toggleKey))
        {
            isPlacementActive = !isPlacementActive;
            if (previewObject != null)
            {
                previewObject.SetActive(isPlacementActive);
            }
        }
        
        // Only handle placement when active
        if (isPlacementActive)
        {
            base.Update();
            
            // Allow cycling through object types with Tab key
            if (Input.GetKeyDown(KeyCode.Tab) && availableObjectTypes.Count > 1)
            {
                selectedObjectIndex = (selectedObjectIndex + 1) % availableObjectTypes.Count;
                CreatePreview();
                
                if (previewObject != null)
                {
                    previewObject.SetActive(true);
                }
            }
        }
    }
    
    protected override void UpdatePreviewPosition(RaycastHit hit)
    {
        // Position the preview on top of the cell
        previewObject.transform.position = hit.transform.position + Vector3.up * previewYOffset;
    }
    
    protected override bool CanPlaceAtPosition(Vector2Int baseCell)
    {
        EnvironmentalObjectData objectData = CurrentObjectData;
        if (objectData == null || objectFactory == null) return false;
        
        // Use the factory to check if placement is valid
        return objectFactory.CanPlaceEnvironmentalObject(objectData, baseCell);
    }
    
    protected override void PlaceObjectAtCurrentCell()
    {
        EnvironmentalObjectData objectData = CurrentObjectData;
        if (objectData == null || objectFactory == null) return;
        
        // Get cell object for position
        GameObject cellObject;
        if (gridManager.TryGetCellObject(currentCellCoordinates, out cellObject))
        {
            Vector3 position = cellObject.transform.position;
            position.y = 0.05f; // Slightly above ground
            
            // Create environmental object using factory
            EnvironmentalObject newObject = objectFactory.CreateEnvironmentalObject(
                objectData, position, currentCellCoordinates);
                
            if (newObject != null)
            {
                Debug.Log($"Placed {objectData.ObjectName} at ({currentCellCoordinates.x}, {currentCellCoordinates.y})");
            }
        }
    }
}