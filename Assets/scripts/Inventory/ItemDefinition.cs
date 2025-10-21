using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Definition", fileName = "NewItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string Id;                 // used by DB lookups (match pickups)
    public string displayName;

    [Header("Grid")]
    public int gridWidth = 1;
    public int gridHeight = 1;
    public bool canRotate = true;
    public int maxStack = 1;

    [Header("Stats")]
    public float weightKg = 1f;

    [Header("UI")]
    public Sprite icon;

    [Header("World")]
    // Prefab to spawn when dropping from inventory (must have WorldPickup + Rigidbody)
    public GameObject worldPrefab;
}
