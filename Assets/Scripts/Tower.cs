using UnityEngine;

/// <summary>
/// Represents a defensive tower that can attack enemies and take damage
/// </summary>
public class Tower : DamageableStructure
{
    protected override void Awake()
    {
        base.Awake();
        
        // Parse grid position from the object name (Tower_X_Y format)
        ParseGridPosition();
    }
    
    /// <summary>
    /// Extract the grid position from the tower's name
    /// </summary>
    private void ParseGridPosition()
    {
        string[] nameParts = gameObject.name.Split('_');
        
        // Check if the name follows the expected format (Tower_X_Y)
        if (nameParts.Length >= 3 && nameParts[0] == "Tower")
        {
            if (int.TryParse(nameParts[1], out int x) && int.TryParse(nameParts[2], out int y))
            {
                coordinate = new Vector2Int(x, y);
                Debug.Log($"Tower initialized at grid position {coordinate}");
            }
        }
    }
    
    /// <summary>
    /// Override the DestroyStructure method to free up the grid cell
    /// </summary>
    protected override void DestroyStructure()
    {
        if (gridManager)
        {
            gridManager.SetCellOccupied(coordinate, false);
        }
        
        // Call the base implementation to handle the rest of the destruction logic
        base.DestroyStructure();
    }
}