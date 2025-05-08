using System.Collections.Generic;
using UnityEngine;

public class TowerPlacementSystem : GridTargetingSystem
{
    [Header("Tower Data")]
    [SerializeField] private List<TowerData> availableTowerTypes = new List<TowerData>();
    [SerializeField] private int selectedTowerIndex = 0;
    [SerializeField] private GameObject towerPrefab;
    
    public TowerData CurrentTowerData => 
        availableTowerTypes.Count > selectedTowerIndex ? availableTowerTypes[selectedTowerIndex] : null;
    
    protected override void CreatePreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        
        TowerData towerData = CurrentTowerData;
        if (towerData == null || towerData.ModelPrefab == null) return;
        
        previewObject = new GameObject("TowerPreview");
        
        GameObject modelInstance = Instantiate(towerData.ModelPrefab, previewObject.transform);
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
    
    public void SelectTowerType(int index)
    {
        if (index >= 0 && index < availableTowerTypes.Count)
        {
            selectedTowerIndex = index;
            CreatePreview();
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        // Allow cycling through tower types with Tab key
        if (Input.GetKeyDown(KeyCode.Tab) && availableTowerTypes.Count > 1)
        {
            selectedTowerIndex = (selectedTowerIndex + 1) % availableTowerTypes.Count;
            CreatePreview();
        }
    }
    
    protected override void UpdatePreviewPosition(RaycastHit hit)
    {
        if (availableTowerTypes[selectedTowerIndex].Size.x == 1 && availableTowerTypes[selectedTowerIndex].Size.y == 1)
        {
            // Position the preview on top of the cell
            previewObject.transform.position = hit.transform.position + Vector3.up * previewYOffset;
        }
        else
        {
            // We have the current cell
            gridManager.TryGetCellObject(currentCellCoordinates, out var currentCellObject);

            // For multi-cell tower, calculate the center position based on the current cell
            float offsetX = (availableTowerTypes[selectedTowerIndex].Size.x * gridManager.CellSize) / 2.0f;
            float offsetZ = (availableTowerTypes[selectedTowerIndex].Size.y * gridManager.CellSize) / 2.0f;

            // Position the preview at the center of the multi-cell area
            previewObject.transform.position = new Vector3(
                currentCellObject.transform.position.x + offsetX - (gridManager.CellSize / 2.0f),
                previewYOffset,
                currentCellObject.transform.position.z + offsetZ - (gridManager.CellSize / 2.0f)
            );
        }
    }
    
    protected override bool CanPlaceAtPosition(Vector2Int baseCell)
    {
        TowerData towerData = CurrentTowerData;
        if (towerData == null) return false;
        
        // Clear previous preview cells
        previewCells.Clear();
        
        // Check if all cells needed for this tower are valid and available
        for (int x = 0; x < towerData.Size.x; x++)
        {
            for (int y = 0; y < towerData.Size.y; y++)
            {
                Vector2Int cellToCheck = new Vector2Int(baseCell.x + x, baseCell.y + y);
                
                // Check if this cell is within grid bounds
                if (cellToCheck.x < 0 || cellToCheck.x >= gridManager.GridSizeX || 
                    cellToCheck.y < 0 || cellToCheck.y >= gridManager.GridSizeY)
                {
                    return false;
                }
                
                // Check if this cell is already occupied
                if (gridManager.IsCellOccupied(cellToCheck))
                {
                    return false;
                }
                
                // Add to preview cells
                previewCells.Add(cellToCheck);
            }
        }
        
        return true;
    }
    
    protected override void PlaceObjectAtCurrentCell()
    {
        TowerData towerData = CurrentTowerData;
        if (towerData == null || towerPrefab == null) return;
        
        // Calculate the center position for the tower based on its size
        Vector3 placementPosition;
        
        if (towerData.Size.x == 1 && towerData.Size.y == 1)
        {
            // For 1x1 towers, use the current cell's center position
            gridManager.TryGetCellObject(currentCellCoordinates, out var cellObject);
            placementPosition = cellObject.transform.position;
        }
        else
        {
            // For multi-cell towers, calculate the center position of all occupied cells
            gridManager.TryGetCellObject(currentCellCoordinates, out var bottomLeftCell);
            
            // Calculate offsets to center based on tower size and cell size
            float offsetX = (towerData.Size.x * gridManager.CellSize) / 2.0f;
            float offsetZ = (towerData.Size.y * gridManager.CellSize) / 2.0f;
            
            // Position at the center of all cells
            placementPosition = new Vector3(
                bottomLeftCell.transform.position.x + offsetX - (gridManager.CellSize / 2.0f),
                0,
                bottomLeftCell.transform.position.z + offsetZ - (gridManager.CellSize / 2.0f)
            );
        }
        
        // Instantiate the tower prefab at the calculated position
        GameObject towerObject = Instantiate(towerPrefab, placementPosition, Quaternion.identity);
        towerObject.name = $"Tower_{currentCellCoordinates.x}_{currentCellCoordinates.y}";
        
        // Get the Tower component from the prefab instance
        Tower tower = towerObject.GetComponent<Tower>();
        if (tower != null)
        {
            // Initialize the tower with the tower data
            tower.Initialize(towerData);
            
            // Mark the base cell as occupied
            gridManager.SetCellOccupied(currentCellCoordinates, true);
            
            Debug.Log($"Placed {towerData.TowerName} at ({currentCellCoordinates.x}, {currentCellCoordinates.y})");
        }
        else
        {
            Debug.LogError("Tower component not found on the instantiated tower prefab!");
            Destroy(towerObject);
        }
    }
}