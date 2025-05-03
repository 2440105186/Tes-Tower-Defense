using System.Text;
using UnityEngine;

public class TowerPlacementSystem : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;
    [SerializeField] private float previewYOffset = 0.1f;
    [SerializeField] private LayerMask gridLayerMask;

    private GameObject previewTower;
    private bool canPlaceTower = false;
    private GridManager gridManager;

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        CreatePreviewTower();
        
        // Debug: Print all grid cells' occupied status
        if (gridManager != null)
        {
            Debug.Log("Checking all grid cells' occupation status:");
            StringBuilder statusReport = new StringBuilder();
            
            for (int x = 0; x < gridManager.GridSizeX; x++)
            {
                for (int y = 0; y < gridManager.GridSizeY; y++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    bool isOccupied = gridManager.IsCellOccupied(cellPos);
                    bool isPath = gridManager.IsCellPath(cellPos);
                    
                    statusReport.AppendLine($"Cell ({x},{y}) - Occupied: {isOccupied}, Is Path: {isPath}");
                }
            }
            
            Debug.Log(statusReport.ToString());
        }
        else
        {
            Debug.LogError("GridManager not found!");
        }
    }


    private void CreatePreviewTower()
    {
        if (towerPrefab == null) return;

        // Instantiate the preview tower based on the actual tower prefab
        previewTower = Instantiate(towerPrefab);
        
        // Remove any scripts that might affect gameplay
        foreach (var component in previewTower.GetComponents<MonoBehaviour>())
        {
            if (component != null && component.GetType() != typeof(Transform))
            {
                Destroy(component);
            }
        }

        // Get or add MeshRenderer components for all children to apply the preview material
        foreach (var renderer in previewTower.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = validPlacementMaterial;
            }
            renderer.materials = materials;
        }

        // Disable the preview tower initially
        previewTower.SetActive(false);
    }

    private void Update()
    {
        UpdateTowerPreview();
        HandleTowerPlacement();
    }

    private void UpdateTowerPreview()
    {
        if (gridManager == null) return;

        // Cast ray from mouse position to find grid cell
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, gridLayerMask))
        {
            // Get the cell coordinates
            string[] cellNameParts = hit.transform.name.Split('_');
            if (cellNameParts.Length >= 3)
            {
                int cellX = int.Parse(cellNameParts[1]);
                int cellY = int.Parse(cellNameParts[2]);
                
                // Get the cell coordinates
                Vector2Int cellPos = new Vector2Int(cellX, cellY);

                // Check if the cell is available for placement
                canPlaceTower = !gridManager.IsCellOccupied(cellPos);

                // Show preview at the cell position
                previewTower.SetActive(true);
                previewTower.transform.position = new Vector3(
                    hit.transform.position.x, 
                    hit.transform.position.y + previewYOffset, 
                    hit.transform.position.z
                );

                // Apply material based on placement validity
                Material previewMaterial = canPlaceTower ? validPlacementMaterial : invalidPlacementMaterial;
                foreach (var renderer in previewTower.GetComponentsInChildren<Renderer>())
                {
                    Material[] materials = new Material[renderer.materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = previewMaterial;
                    }
                    renderer.materials = materials;
                }
            }
        }
        else
        {
            // Hide preview if not over any grid cell
            previewTower.SetActive(false);
            canPlaceTower = false;
        }
    }

    private void HandleTowerPlacement()
    {
        if (Input.GetMouseButtonDown(0) && canPlaceTower && gridManager != null)
        {
            // Get the current position from the preview
            Vector3 placementPosition = previewTower.transform.position;
            
            // Calculate grid coordinates
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, gridLayerMask))
            {
                string[] cellNameParts = hit.transform.name.Split('_');
                if (cellNameParts.Length >= 3)
                {
                    int cellX = int.Parse(cellNameParts[1]);
                    int cellY = int.Parse(cellNameParts[2]);
                    Vector2Int cellPos = new Vector2Int(cellX, cellY);
                    
                    // Create actual tower at position
                    GameObject placedTower = Instantiate(towerPrefab, placementPosition, Quaternion.identity);
                    placedTower.name = $"Tower_{cellX}_{cellY}";
                    
                    // Mark cell as occupied in the grid manager
                    gridManager.SetCellOccupied(cellPos, true);
                }
            }
        }
    }
}