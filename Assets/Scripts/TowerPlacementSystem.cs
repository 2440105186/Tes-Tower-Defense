using UnityEngine;
using System.Collections.Generic;

public class TowerPlacementSystem : MonoBehaviour
{
    [Header("Tower Data")]
    [SerializeField] private List<TowerData> availableTowerTypes = new List<TowerData>();
    [SerializeField] private int selectedTowerIndex = 0;
    [SerializeField] private GameObject towerPrefab;
    
    [Header("Config")]
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;
    [SerializeField] private float previewYOffset = 0.1f;
    [SerializeField] private LayerMask gridLayerMask;
    
    private GameObject previewObject;
    private bool canPlaceTower = false;
    private GridManager gridManager;
    private Vector2Int currentCellCoordinates;
    private List<Vector2Int> previewCells = new List<Vector2Int>();
    
    public TowerData CurrentTowerData => 
        availableTowerTypes.Count > selectedTowerIndex ? availableTowerTypes[selectedTowerIndex] : null;

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (availableTowerTypes.Count > 0)
        {
            CreateTowerPreview();
        }
    }
    
    private void CreateTowerPreview()
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
            CreateTowerPreview();
        }
    }
    
    private void Update()
    {
        UpdateTowerPreview();
        HandleTowerPlacement();
        
        // Allow cycling through tower types with Tab key
        if (Input.GetKeyDown(KeyCode.Tab) && availableTowerTypes.Count > 1)
        {
            selectedTowerIndex = (selectedTowerIndex + 1) % availableTowerTypes.Count;
            CreateTowerPreview();
        }
    }
    
    private void UpdateTowerPreview()
    {
        if (gridManager == null || previewObject == null) return;
        
        // Cast ray from mouse position to find grid cell
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, gridLayerMask))
        {
            // Make preview visible
            previewObject.SetActive(true);
            
            // Get the cell coordinates
            if (hit.collider.gameObject.TryGetComponent<GridCell>(out var cell))
            {
                currentCellCoordinates = cell.coordinates;
                
                if (availableTowerTypes[selectedTowerIndex].Size.x == 1 && availableTowerTypes[selectedTowerIndex].Size.y == 1)
                {
                    // Position the preview on top of the cell
                    previewObject.transform.position = hit.transform.position + Vector3.up * previewYOffset;
                }
                else
                {
                    // For multi-cell tower, calculate the center position based on the current cell
                    // The tower's origin should be at the bottom-left corner (the current cell)
                    float offsetX = (availableTowerTypes[selectedTowerIndex].Size.x * gridManager.CellSize) / 2.0f;
                    float offsetZ = (availableTowerTypes[selectedTowerIndex].Size.y * gridManager.CellSize) / 2.0f;
    
                    // Position the preview at the center of the multi-cell area
                    previewObject.transform.position = new Vector3(
                        cell.transform.position.x + offsetX - (gridManager.CellSize / 2.0f),
                        previewYOffset,
                        cell.transform.position.z + offsetZ - (gridManager.CellSize / 2.0f)
                    );
                }
                
                // Check if all cells needed for this tower are available
                canPlaceTower = CanPlaceTowerAt(currentCellCoordinates);
                
                // Update preview materials based on placement validity
                UpdatePreviewMaterial(canPlaceTower);
            }
        }
        else
        {
            // Hide preview if not over any grid cell
            previewObject.SetActive(false);
            canPlaceTower = false;
        }
    }
    
    private bool CanPlaceTowerAt(Vector2Int baseCell)
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
    
    private void UpdatePreviewMaterial(bool validPlacement)
    {
        if (previewObject == null) return;
        
        Material previewMaterial = validPlacement ? validPlacementMaterial : invalidPlacementMaterial;
        
        foreach (var renderer in previewObject.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = previewMaterial;
            }
            renderer.materials = materials;
        }
    }
    
    private void HandleTowerPlacement()
    {
        if (Input.GetMouseButtonDown(0) && canPlaceTower && gridManager != null)
        {
            PlaceTowerAtCurrentCell();
        }
    }
    
    
    private void PlaceTowerAtCurrentCell()
    {
        TowerData towerData = CurrentTowerData;
        if (towerData == null || towerPrefab == null) return;
        
        // Calculate the center position for the tower based on its size
        Vector3 placementPosition;
        
        gridManager.TryGetCellObject(currentCellCoordinates, out var originObject);
        if (towerData.Size.x == 1 && towerData.Size.y == 1)
        {
            // For 1x1 towers, use the current cell's center position
            placementPosition = originObject.transform.position;
        }
        else
        {
            // For multi-cell towers, calculate the center position of all occupied cells
            // Calculate offsets to center based on tower size and cell size
            float offsetX = (towerData.Size.x * gridManager.CellSize) / 2.0f;
            float offsetZ = (towerData.Size.y * gridManager.CellSize) / 2.0f;
            
            // Position at the center of all cells
            placementPosition = new Vector3(
                originObject.transform.position.x + offsetX - (gridManager.CellSize / 2.0f),
                0,
                originObject.transform.position.z + offsetZ - (gridManager.CellSize / 2.0f)
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
            tower.Initialize(towerData, currentCellCoordinates);
            
            Debug.Log($"Placed {towerData.TowerName} at ({currentCellCoordinates.x}, {currentCellCoordinates.y})");
        }
        else
        {
            Debug.LogError("Tower component not found on the instantiated tower prefab!");
            Destroy(towerObject);
        }
    }
}