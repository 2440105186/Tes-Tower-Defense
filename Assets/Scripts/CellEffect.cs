public enum CellEffectType
{
    None,
    LineOfSightBlocker,
    MovementBlocker,
    MovementModifier,
    ThermalVisionBlocker
}

public class CellEffect
{
    public CellEffectType EffectType { get; private set; }
    public float Intensity { get; private set; }
    public EnvironmentalObject Source { get; private set; }
    
    public CellEffect(CellEffectType type, float intensity, EnvironmentalObject source)
    {
        EffectType = type;
        Intensity = intensity;
        Source = source;
    }
}