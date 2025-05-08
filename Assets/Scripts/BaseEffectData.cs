using UnityEngine;

[CreateAssetMenu(fileName = "NewEnvironmentalEffectData", menuName = "Tower Defense/Effects/Base Effect Data")]
public abstract class BaseEffectData : ScriptableObject
{
    [SerializeField] protected string effectName = "Base Effect";
    [TextArea(2, 4)]
    [SerializeField] protected string effectDescription = "A basic environmental effect.";
    
    public string EffectName => effectName;
    public string EffectDescription => effectDescription;
    
    public abstract IEnvironmentalEffect CreateEffect(EnvironmentalObject owner);
}