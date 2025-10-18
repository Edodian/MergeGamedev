using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ItemPickupTarget : MonoBehaviour
{
    [Tooltip("ItemDefinition Id")]
    public string itemId = "item_id";
    public int amount = 1;
    [Tooltip("Optional UI name; empty = use ItemDefinition.displayName")]
    public string displayName;

    // Global registry (no colliders/raycast needed)
    public static readonly HashSet<ItemPickupTarget> All = new HashSet<ItemPickupTarget>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;
        if (ItemDatabase.Instance && ItemDatabase.Instance.TryGet(itemId, out var def))
            return string.IsNullOrEmpty(def.displayName) ? def.Id : def.displayName;
        return itemId;
    }
}
