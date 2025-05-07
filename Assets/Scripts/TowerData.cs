using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Tower Identity")]
    [SerializeField] private string towerName = "Basic Tower";
    [TextArea(2, 4)]
    [SerializeField] private string description = "A basic tower that attacks enemies.";
    
    [Header("Tower Visuals")]
    [SerializeField] private GameObject modelPrefab;
    
    [Header("Tower Properties")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private Vector2Int size = Vector2Int.one; // Size in grid cells (x,y)
    
    // Public properties
    public string TowerName => towerName;
    public string Description => description;
    public GameObject ModelPrefab => modelPrefab;
    public float MaxHealth => maxHealth;
    public float RotationSpeed => rotationSpeed;
    public float AttackRange => attackRange;
    public Vector2Int Size => size;
}