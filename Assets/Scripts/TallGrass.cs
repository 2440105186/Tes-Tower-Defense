using UnityEngine;

public class TallGrass : CellModifier
{
    [Header( "Tall Grass Settings")]
    [SerializeField] private VisionModes hideFrom = VisionModes.Visual;
    public VisionModes HideFrom => hideFrom;
}