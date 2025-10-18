using UnityEngine;

[CreateAssetMenu(menuName = "Inv/Item", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id = "item_id";
    public string displayName = "Item";

    [Header("Grid size (cells)")]
    public int gridWidth = 1;
    public int gridHeight = 1;
    public bool canRotate = true;

    [Header("Stacking & weight")]
    public int maxStack = 1;           // 1 = not stackable
    public float weightKg = 1f;        // weight per single unit

    [Header("Visual")]
    public Sprite icon;                // used in UI
    public GameObject worldPrefab;     // optional, for pickups
}
