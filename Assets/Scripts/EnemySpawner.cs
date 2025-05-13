using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject rangedEnemyPrefab;
    [SerializeField] private GameObject suicideEnemyPrefab;
    [SerializeField] private int enemyCount = 10;
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private float spawnHeight = 0.5f;
    [SerializeField] private bool spawnOnStart = true;

    private GridManager gridManager;

    private void Awake()
    {
        gridManager = GridManager.Instance;
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnWave();
        }
    }

    public void SpawnWave()
    {
        StartCoroutine(SpawnWaveCoroutine());
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        // Spawn enemies one by one with delay
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(rangedEnemyPrefab);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    public void SpawnEnemy(GameObject enemyType)
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
        
        Instantiate(enemyType, position, Quaternion.identity);
    }
}