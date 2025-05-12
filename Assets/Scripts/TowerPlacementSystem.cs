using UnityEngine;
using System;
using System.Collections.Generic;

public class TowerPlacementSystem : MonoBehaviour
{
    [Header("Tower Data")]
    [SerializeField] private List<TowerData> availableTowerTypes = new();
    [SerializeField] private int selectedTowerIndex;
    [SerializeField] private GameObject towerPrefab;

    [Header("Config")]
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;
    [SerializeField] private float previewYOffset = 0.1f;
    [SerializeField] private LayerMask gridLayerMask;

    private GameObject previewObject;
    private bool canPlaceTower;
    private GridManager gridManager;
    private Vector2Int currentCellCoordinates;
    private List<Vector2Int> previewCells = new();

    public TowerData CurrentTowerData => availableTowerTypes.Count > selectedTowerIndex ? availableTowerTypes[selectedTowerIndex] : null;

    public event Action<int> OnTowerTypeChanged;

    private void Awake()
    {
        gridManager = GridManager.Instance;
    }

    private void Start()
    {
        if (availableTowerTypes.Count > 0) CreateTowerPreview();
    }

    public void SelectTowerType(int index)
    {
        if (index < 0 || index >= availableTowerTypes.Count || index == selectedTowerIndex) return;

        selectedTowerIndex = index;
        CreateTowerPreview();
        OnTowerTypeChanged?.Invoke(index);
    }

    private void Update()
    {
        UpdateTowerPreview();
        HandleTowerPlacement();

        if (Input.GetKeyDown(KeyCode.Tab) && availableTowerTypes.Count > 1)
        {
            SelectTowerType((selectedTowerIndex + 1) % availableTowerTypes.Count);
        }
    }

    private void CreateTowerPreview()
    {
        if (previewObject != null) Destroy(previewObject);

        var data = CurrentTowerData;
        if (data == null || data.ModelPrefab == null) return;

        previewObject = new GameObject("TowerPreview");
        var model = Instantiate(data.ModelPrefab, previewObject.transform);
        model.transform.localPosition = Vector3.zero;

        ApplyMaterialToAllRenderers(validPlacementMaterial);
        previewObject.SetActive(false);
    }

    private void UpdateTowerPreview()
    {
        if (gridManager == null || previewObject == null) return;

        GridCell cell = null;
        if (Camera.main != null)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 100f, gridLayerMask) || !hit.collider.TryGetComponent<GridCell>(out cell))
            {
                previewObject.SetActive(false);
                canPlaceTower = false;
                return;
            }
        }

        previewObject.SetActive(true);
        if (cell != null)
        {
            currentCellCoordinates = cell.coordinates;
            previewObject.transform.position = CalculatePreviewPosition(cell.transform.position, CurrentTowerData.Size);
        }

        canPlaceTower = CanPlaceTowerAt(currentCellCoordinates);
        ApplyMaterialToAllRenderers(canPlaceTower ? validPlacementMaterial : invalidPlacementMaterial);
    }

    private bool CanPlaceTowerAt(Vector2Int baseCell)
    {
        var data = CurrentTowerData;
        if (data == null)
            return false;

        previewCells.Clear();
        for (int x = 0; x < data.Size.x; x++)
        {
            for (int y = 0; y < data.Size.y; y++)
            {
                var cellPos = new Vector2Int(baseCell.x + x, baseCell.y + y);
                if (!gridManager.IsWithinBounds(cellPos)
                    || gridManager.IsCellOccupied(cellPos))
                    return false;
                previewCells.Add(cellPos);
            }
        }
        return true;
    }

    private Vector3 CalculatePreviewPosition(Vector3 cellWorldPos, Vector2Int towerSize)
    {
        float halfCell = gridManager.CellSize / 2f;
        if (towerSize == Vector2Int.one)
            return cellWorldPos + Vector3.up * previewYOffset;

        float offsetX = (towerSize.x * gridManager.CellSize) / 2f - halfCell;
        float offsetZ = (towerSize.y * gridManager.CellSize) / 2f - halfCell;
        return new Vector3(
            cellWorldPos.x + offsetX,
            previewYOffset,
            cellWorldPos.z + offsetZ);
    }

    private void ApplyMaterialToAllRenderers(Material mat)
    {
        if (previewObject == null) return;
        foreach (var rend in previewObject.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mat;
            rend.materials = mats;
        }
    }

    private void HandleTowerPlacement()
    {
        if (Input.GetMouseButtonDown(0) && canPlaceTower) PlaceTowerAtCurrentCell();
    }

    private void PlaceTowerAtCurrentCell()
    {
        var data = CurrentTowerData;
        if (data == null || towerPrefab == null)
            return;

        gridManager.TryGetCellObject(currentCellCoordinates, out var cellObj);
        var pos = CalculatePreviewPosition(cellObj.transform.position, data.Size);
        pos.y = 0;

        var towerObj = Instantiate(towerPrefab, pos, Quaternion.identity);
        towerObj.name = $"Tower_{currentCellCoordinates.x}_{currentCellCoordinates.y}";

        if (towerObj.TryGetComponent<Tower>(out var tower))
        {
            tower.Initialize(data, currentCellCoordinates);
        }
        else
        {
            Debug.LogError("Tower component missing on prefab");
            Destroy(towerObj);
        }
    }
}