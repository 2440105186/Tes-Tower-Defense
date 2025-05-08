using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnvironmentalObject", menuName = "Tower Defense/Environmental Object Data")]
public class EnvironmentalObjectData : ScriptableObject
{
    [Header("Basic Properties")]
    [SerializeField] private string objectName = "Environmental Object";
    [TextArea(2, 4)]
    [SerializeField] private string description = "An object that affects the environment.";
    [SerializeField] private GameObject modelPrefab;
    
    [Header("Grid Properties")]
    [SerializeField] private Vector2Int size = Vector2Int.one;
    [SerializeField] private bool isDestructible = true;
    [SerializeField] private float maxHealth = 50f;
    
    [Header("Effects")]
    [SerializeField] private List<BaseEffectData> effects = new List<BaseEffectData>();
    
    public string ObjectName => objectName;
    public string Description => description;
    public GameObject ModelPrefab => modelPrefab;
    public Vector2Int Size => size;
    public bool IsDestructible => isDestructible;
    public float MaxHealth => maxHealth;
    public List<BaseEffectData> Effects => effects;
}