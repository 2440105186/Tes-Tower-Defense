using System;
using UnityEngine;
using System.Collections.Generic;

public abstract class GridTargetingSystem : MonoBehaviour
{
    [Header("Targeting Config")]
    [SerializeField] protected Material validPlacementMaterial;
    [SerializeField] protected Material invalidPlacementMaterial;
    [SerializeField] protected float previewYOffset = 0.1f;
    [SerializeField] protected LayerMask gridLayerMask;
    
    protected GameObject previewObject;
    protected bool canPlace = false;
    protected GridManager gridManager;
    protected Vector2Int currentCellCoordinates;
    protected List<Vector2Int> previewCells = new List<Vector2Int>();

    private void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
    }

    protected virtual void Start()
    {
        CreatePreview();
    }
    
    protected abstract void CreatePreview();
    
    protected virtual void Update()
    {
        UpdatePreview();
        HandlePlacement();
    }
    
    protected virtual void UpdatePreview()
    {
        if (gridManager == null || previewObject == null) return;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, gridLayerMask))
        {
            previewObject.SetActive(true);
            
            if (gridManager.TryGetCellAtPosition(hit.transform.position, out Vector2Int cellCoords))
            {
                currentCellCoordinates = cellCoords;
                UpdatePreviewPosition(hit);
                
                canPlace = CanPlaceAtPosition(cellCoords);
                UpdatePreviewMaterial(canPlace);
            }
        }
        else
        {
            previewObject.SetActive(false);
            canPlace = false;
        }
    }
    
    protected abstract void UpdatePreviewPosition(RaycastHit hit);
    
    protected abstract bool CanPlaceAtPosition(Vector2Int baseCell);
    
    protected virtual void UpdatePreviewMaterial(bool validPlacement)
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
    
    protected virtual void HandlePlacement()
    {
        if (Input.GetMouseButtonDown(0) && canPlace && gridManager != null)
        {
            PlaceObjectAtCurrentCell();
        }
    }
    
    protected abstract void PlaceObjectAtCurrentCell();
}