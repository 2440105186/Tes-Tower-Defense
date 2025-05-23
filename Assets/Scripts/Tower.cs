﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TargetingMode
{
    First,
    Last,
    Closest
}

public class Tower : DamageableStructure
{
    [Header("Tower Type")]
    [SerializeField] private TowerData towerData;
    
    [Header("Tower Settings")]
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private TargetingMode targetingMode = TargetingMode.First;
    [SerializeField] private VisionModes visionModes = VisionModes.Visual;
    
    [Header("Target Reevaluation")]
    [SerializeField] private float targetReevaluationInterval = 0.5f;
    
    [Header("Tower Components")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Transform rotatablePart;
    [SerializeField] private TowerDetector detector;
    [SerializeField] private  LayerMask EnemyLayerMask;
    [SerializeField] private  LayerMask lineOfSightLayerMask;
    
    private BoxCollider boxCollider;
    private List<Enemy> enemiesInRange = new List<Enemy>();
    private Enemy currentTarget;
    private Coroutine targetEvaluationCoroutine;
    private Vector3 lastTargetPosition;
    private float lastRotationUpdateTime;
    
    private Quaternion currentTargetRotation;
    private bool isRotating = false;
    
    // Define whether the targeting mode needs frequent reevaluation
    private bool needsFrequentReevaluation = false;
    
    // List of cells this tower occupies
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();
    
    public TowerData TowerData => towerData;
    
    protected override void Awake()
    {
        // Initialize the base structure
        base.Awake();
        
        boxCollider = GetComponent<BoxCollider>();
    }
    
    public void Initialize(TowerData towerDataInput, Vector2Int originCoordinate)
    {
        if (towerDataInput != null)
        {
            // Store the tower data
            towerData = towerDataInput;
            coordinate = originCoordinate;
        
            // Set tower properties from data
            currentHealth = towerDataInput.MaxHealth;
            rotationSpeed = towerDataInput.RotationSpeed;
            boxCollider.size = new Vector3(towerDataInput.Size.x, boxCollider.size.y, towerDataInput.Size.y);
            visionModes = towerDataInput.VisionModes;
        
            // Create the visual representation
            if (visualTransform == null)
            {
                visualTransform = transform;
            }
        
            if (towerDataInput.ModelPrefab != null)
            {
                GameObject modelInstance = Instantiate(towerDataInput.ModelPrefab, visualTransform);
                modelInstance.layer = gameObject.layer;
            
                // Try to find a rotatable part if none is assigned
                if (rotatablePart == null)
                {
                    // Try to find a suitable rotatable part in the model
                    Transform possibleRotatablePart = modelInstance.transform.GetChild(0);  // Assume first child is rotatable
                    if (possibleRotatablePart != null)
                    {
                        rotatablePart = possibleRotatablePart;
                    }
                }
            }
        }
    
        // Set up the targeting behavior
        UpdateTargetingBehavior();
    
        // Initialize target rotation
        if (rotatablePart != null)
        {
            currentTargetRotation = rotatablePart.rotation;
        }
    
        // Initialize the detector if it exists
        if (detector != null)
        {
            detector.SetRange(towerData?.AttackRange ?? 5f);
            detector.OnEnemyEntered += OnEnemyEntered;
            detector.OnEnemyExited += OnEnemyExited;
        }
    }
    
    private void Start()
    {
        lastRotationUpdateTime = Time.time;
        
        // Occupy all cells this tower covers
        if (towerData != null)
        {
            OccupyCells(true);
        }
    }
    
    // Occupy or release all cells that this tower covers
    private void OccupyCells(bool occupy)
    {
        if (gridManager == null || towerData == null) return;
        
        // Clear previous list of occupied cells
        occupiedCells.Clear();
        
        // Occupy all cells within the tower's size
        for (int x = 0; x < towerData.Size.x; x++)
        {
            for (int y = 0; y < towerData.Size.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(coordinate.x + x, coordinate.y + y);
                
                // Mark cell as occupied/unoccupied in grid manager
                gridManager.SetCellOccupied(cellPos, occupy);
                
                // Add to our list of occupied cells
                if (occupy)
                {
                    occupiedCells.Add(cellPos);
                }
            }
        }
    }
    
    private void Update()
    {
        if (currentTarget != null && rotatablePart != null && currentTarget.isActiveAndEnabled && CheckTargetingValidity(currentTarget))
        {
            // Get the current position of the target
            Vector3 targetPosition = currentTarget.transform.position;
            
            // Calculate direction to target
            Vector3 targetDirection = targetPosition - rotatablePart.position;
            targetDirection.y = 0; // Keep rotation horizontal
            
            // Only update target rotation if we have a meaningful direction
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                // Update the target rotation
                currentTargetRotation = Quaternion.LookRotation(targetDirection);
                isRotating = true;
            }
            
            // Store the last known position of our target
            lastTargetPosition = targetPosition;
        }
        else
        {
            isRotating = false;
        }
        
        // Always smoothly rotate towards the current target rotation
        if (isRotating && rotatablePart != null)
        {
            // Smoothly rotate towards the target
            rotatablePart.rotation = Quaternion.RotateTowards(
                rotatablePart.rotation,
                currentTargetRotation,
                rotationSpeed * Time.deltaTime * 90f // Convert to degrees
            );
            
            // If we're very close to the target rotation, mark as not rotating
            if (Quaternion.Angle(rotatablePart.rotation, currentTargetRotation) < 0.1f)
            {
                rotatablePart.rotation = currentTargetRotation;
                isRotating = (currentTarget != null); // Keep rotating only if we still have a target
            }
        }
    }

    private void OnEnemyEntered(Enemy enemy)
    {
        if (!enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
            enemy.OnDestroyed += OnEnemyDestroyed;
            
            // If this is our first enemy and targeting coroutine isn't running, start it
            if (enemiesInRange.Count == 1)
            {
                StartTargetEvaluationIfNeeded();
            }
            
            // Always select a target if we don't have one
            if (currentTarget == null)
            {
                SelectTargetBasedOnMode();
            }
            // For dynamic targeting modes, reevaluate the target when a new enemy enters
            else if (needsFrequentReevaluation)
            {
                SelectTargetBasedOnMode();
            }
        }
    }
    
    private void OnEnemyExited(Enemy enemy)
    {
        if (enemiesInRange.Contains(enemy))
        {
            HandleEnemyExit(enemy);
        }
    }
    
    private void OnEnemyDestroyed(IDamageable damageable)
    {
        Enemy enemy = damageable as Enemy;
        if (enemy != null)
        {
            HandleEnemyExit(enemy);
        }
    }

    private void HandleEnemyExit(Enemy enemy)
    {
        enemiesInRange.Remove(enemy);
        enemy.OnDestroyed -= OnEnemyDestroyed;
            
        // If our current target left the range, select a new one
        if (enemy == currentTarget)
        {
            currentTarget = null;
            SelectTargetBasedOnMode();
        }
            
        // If no enemies left, stop target evaluation coroutine
        if (enemiesInRange.Count == 0 && targetEvaluationCoroutine != null)
        {
            StopCoroutine(targetEvaluationCoroutine);
            targetEvaluationCoroutine = null;
        }
    }
    
    private void StartTargetEvaluationIfNeeded()
    {
        // Only start the periodic reevaluation if the current targeting mode needs it
        // and we don't already have a coroutine running
        if (needsFrequentReevaluation && targetEvaluationCoroutine == null && enemiesInRange.Count > 0)
        {
            targetEvaluationCoroutine = StartCoroutine(EvaluateTargetsRepeatedly());
        }
    }
    
    private IEnumerator EvaluateTargetsRepeatedly()
    {
        while (enemiesInRange.Count > 0)
        {
            // Clean up the list to remove any null references
            enemiesInRange.RemoveAll(e => e == null || !e.isActiveAndEnabled);
            
            // Select target based on the current mode
            SelectTargetBasedOnMode();
            
            // Wait before reevaluating
            yield return new WaitForSeconds(targetReevaluationInterval);
        }
        
        // Reset coroutine reference when done
        targetEvaluationCoroutine = null;
    }
    
    private void SelectTargetBasedOnMode()
    {
        if (enemiesInRange.Count == 0)
        {
            currentTarget = null;
            return;
        }
        
        // Clean the list before selecting
        enemiesInRange.RemoveAll(e => e == null || !e.isActiveAndEnabled);
        
        if (enemiesInRange.Count == 0)
        {
            currentTarget = null;
            return;
        }
        
        // Store the current target to check if it changes
        Enemy previousTarget = currentTarget;
        
        // Select new target based on mode
        switch (targetingMode)
        {
            case TargetingMode.First:
                SelectEnemyFirst();
                break;
                
            case TargetingMode.Last:
                SelectEnemyLast();
                break;
                
            case TargetingMode.Closest:
                SelectEnemyClose();
                break;
        }
        
        // If the target changed, update the target rotation
        if (previousTarget != currentTarget && currentTarget != null && rotatablePart != null)
        {
            // Don't immediately snap the rotation - the Update method will handle smooth rotation
            // Just mark that we need to rotate
            isRotating = true;
        }
    }
    
    private bool CheckTargetingValidity(Enemy enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled)
        {
            return false;
        }

        // Check vision mode
        if ((visionModes & enemy.CurrentDetectedBy) == VisionModes.None)
        {
            return false;
        }

        // Calculate origin and direction for LOS check
        Vector3 origin = transform.position + Vector3.up * 0.5f; // slightly above ground
        Vector3 targetPos = enemy.transform.position;
        float distanceToEnemy = detector.Range;
        Vector3 direction = (targetPos - origin).normalized;

        // Raycast for LOS: if anything in lineOfSightLayerMask is hit before the enemy, there's no LOS
        Ray ray = new Ray(origin, direction);

        if (
            Physics.Raycast(ray, out RaycastHit hit, distanceToEnemy, lineOfSightLayerMask | EnemyLayerMask)
        )
        {
            // If we hit the enemy first, LOS is clear
            if (hit.collider.gameObject == enemy.gameObject)
            {
                return true;
            }
            // If we hit something else, LOS is blocked
            return false;
        }
        // Nothing hit at all -- unlikely, but no LOS
        return false;
    }
    
    private void SelectEnemyFirst()
    {
        int maxPathIndex = -1;
        Enemy furthestEnemy = null;
    
        foreach (Enemy enemy in enemiesInRange)
        {
            int pathIndex = enemy.CurrentPathIndex;
        
            if (pathIndex > maxPathIndex)
            {
                if (CheckTargetingValidity(enemy))
                {
                    maxPathIndex = pathIndex;
                    furthestEnemy = enemy;
                }
            }
        }
    
        currentTarget = furthestEnemy;
    }
    
    private void SelectEnemyLast()
    {
        int minPathIndex = int.MaxValue;
        Enemy lastEnemy = null;
        
        foreach (Enemy enemy in enemiesInRange)
        {
            int pathIndex = enemy.CurrentPathIndex;
            
            if (pathIndex < minPathIndex)
            {
                if (CheckTargetingValidity(enemy))
                {
                    minPathIndex = pathIndex;
                    lastEnemy = enemy;
                }
            }
        }
        
        currentTarget = lastEnemy;
    }
    
    private void SelectEnemyClose()
    {
        float minDistance = float.MaxValue;
        Enemy closestEnemy = null;
        
        foreach (Enemy enemy in enemiesInRange)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            
            if (distance < minDistance)
            {
                if (CheckTargetingValidity(enemy))
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        
        currentTarget = closestEnemy;
    }
    
    public void ChangeTargetingMode(TargetingMode mode)
    {
        targetingMode = mode;
        
        // Update targeting behavior for the new mode
        UpdateTargetingBehavior();
        
        // Immediately select a new target with the new targeting mode
        SelectTargetBasedOnMode();
        
        // Start or stop target evaluation coroutine based on the new mode
        if (needsFrequentReevaluation)
        {
            StartTargetEvaluationIfNeeded();
        }
        else if (targetEvaluationCoroutine != null)
        {
            StopCoroutine(targetEvaluationCoroutine);
            targetEvaluationCoroutine = null;
        }
    }
    
    private void UpdateTargetingBehavior()
    {
        // Some targeting modes need frequent reevaluation (like Closest)
        // because targets can change priority without entering/exiting/dying
        switch (targetingMode)
        {
            case TargetingMode.First:
                // First needs reevaluation as enemies progress along the path
                needsFrequentReevaluation = true;
                break;
                
            case TargetingMode.Last:
                // Last needs reevaluation as enemies progress along the path
                needsFrequentReevaluation = true;
                break;
                
            case TargetingMode.Closest:
                // Closest definitely needs reevaluation as enemy positions change
                needsFrequentReevaluation = true;
                break;
        }
    }
    
    protected override void DestroyStructure()
    {
        foreach (Enemy enemy in enemiesInRange)
        {
            if (enemy != null)
            {
                enemy.OnDestroyed -= OnEnemyDestroyed;
            }
        }
        
        if (detector != null)
        {
            detector.OnEnemyEntered -= OnEnemyEntered;
            detector.OnEnemyExited -= OnEnemyExited;
        }
        
        // Stop all coroutines
        if (targetEvaluationCoroutine != null)
        {
            StopCoroutine(targetEvaluationCoroutine);
            targetEvaluationCoroutine = null;
        }
        
        // Release all occupied cells
        if (towerData != null && (towerData.Size.x > 1 || towerData.Size.y > 1))
        {
            OccupyCells(false);
        }
        else if (gridManager)
        {
            // For single-cell towers, just release the base cell
            gridManager.SetCellOccupied(coordinate, false);
        }
        
        base.DestroyStructure();
    }
}