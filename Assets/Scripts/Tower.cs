using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tower : DamageableStructure
{
    public enum TargetingMode
    {
        First,
        Last,
        Closest
    }
    
    [Header("Tower Settings")]
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private TargetingMode targetingMode = TargetingMode.First;
    
    [Header("Target Reevaluation")]
    [SerializeField] private float targetReevaluationInterval = 0.5f;
    
    [Header("Tower Components")]
    [SerializeField] private Transform rotatablePart;
    [SerializeField] private TowerDetector detector;
    
    private List<Enemy> enemiesInRange = new List<Enemy>();
    private Enemy currentTarget;
    private Coroutine targetEvaluationCoroutine;
    private Vector3 lastTargetPosition;
    private float lastRotationUpdateTime;
    
    // Add this to track target rotation
    private Quaternion currentTargetRotation;
    private bool isRotating = false;
    
    // Define whether the targeting mode needs frequent reevaluation
    private bool needsFrequentReevaluation = false;

    protected override void Awake()
    {
        base.Awake();
        ExtractGridPositionFromName();
        
        // Determine if the current targeting mode needs frequent reevaluation
        UpdateTargetingBehavior();
        
        // Initialize target rotation to the current rotation
        if (rotatablePart != null)
        {
            currentTargetRotation = rotatablePart.rotation;
        }
    }
    
    private void Start()
    {
        if (detector != null)
        {
            detector.OnEnemyEntered += OnEnemyEntered;
            detector.OnEnemyExited += OnEnemyExited;
        }
        else
        {
            Debug.LogWarning("Tower has no detector assigned!", this);
        }
        
        lastRotationUpdateTime = Time.time;
    }
    
    private void Update()
    {
        // Update target rotation if we have a valid target
        if (currentTarget != null && rotatablePart != null && currentTarget.isActiveAndEnabled)
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
    
    private void ExtractGridPositionFromName()
    {
        string[] nameParts = gameObject.name.Split('_');
        
        if (nameParts.Length >= 3 && nameParts[0] == "Tower")
        {
            if (int.TryParse(nameParts[1], out int x) && int.TryParse(nameParts[2], out int y))
            {
                coordinate = new Vector2Int(x, y);
                Debug.Log($"Tower initialized at grid position {coordinate}");
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
    
    private void SelectEnemyFirst()
    {
        int maxPathIndex = -1;
        Enemy furthestEnemy = null;
        
        foreach (Enemy enemy in enemiesInRange)
        {
            int pathIndex = enemy.CurrentPathIndex;
            
            if (pathIndex > maxPathIndex)
            {
                maxPathIndex = pathIndex;
                furthestEnemy = enemy;
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
                minPathIndex = pathIndex;
                lastEnemy = enemy;
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
                minDistance = distance;
                closestEnemy = enemy;
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
                needsFrequentReevaluation = false;
                break;
                
            case TargetingMode.Last:
                // Last needs reevaluation as enemies progress along the path
                needsFrequentReevaluation = false;
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
        
        if (gridManager)
        {
            gridManager.SetCellOccupied(coordinate, false);
        }
        
        base.DestroyStructure();
    }
}