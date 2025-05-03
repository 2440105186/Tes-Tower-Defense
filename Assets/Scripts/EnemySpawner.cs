using System.Collections;
using UnityEngine;

/// <summary>
/// Extremely basic enemy wave spawner - just spawns enemies at a position with delays
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemyCount = 10;
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private float spawnHeight = 0.5f;

    private GridManager gridManager;

    private void Awake()
    {
        // Find grid manager for path
        gridManager = FindFirstObjectByType<GridManager>();
    }

    private void Start()
    {
        SpawnWave();
    }

    /// <summary>
    /// Start spawning enemies
    /// </summary>
    public void SpawnWave()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        // Get spawn position from grid path if available
        Vector3 position = transform.position;
        position.y = spawnHeight;
        
        gridManager.TryGetCellObject(gridManager.GetPathCells()[0], out var spawnPoint);
        if (spawnPoint != null)
        {
            position = spawnPoint.transform.position;
            position.y = spawnHeight;
        }
        else if (gridManager != null)
        {
            // Use first path cell as spawn if available
            var pathCells = gridManager.GetPathCells();
            if (pathCells.Count > 0)
            {
                float cellSize = gridManager.CellSize;
                Vector2Int firstCell = pathCells[0];
                position = new Vector3(
                    firstCell.x * cellSize + cellSize * 0.5f,
                    spawnHeight,
                    firstCell.y * cellSize + cellSize * 0.5f
                );
            }
        }

        // Spawn enemies one by one with delay
        for (int i = 0; i < enemyCount; i++)
        {
            Instantiate(enemyPrefab, position, Quaternion.identity);
            yield return new WaitForSeconds(spawnDelay);
        }
    }
}