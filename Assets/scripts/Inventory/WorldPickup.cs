using System.Collections.Generic;
using UnityEngine;

public class WorldPickup : MonoBehaviour
{
    // ItemDefinition.Id to add when picked up
    public string itemId = "glock18_id";
    public int amount = 1;

    // Optional: where we measure distance from (defaults to this transform)
    public Transform pivot;

    public static readonly HashSet<WorldPickup> All = new();

    void OnEnable() { if (!pivot) pivot = transform; All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public string DisplayName
    {
        get
        {
            if (ItemDatabase.Instance && ItemDatabase.Instance.TryGet(itemId, out var def) && !string.IsNullOrEmpty(def.displayName))
                return def.displayName;
            return itemId;
        }
    }

#if UNITY_EDITOR
    // Just for level editing clarity
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((pivot ? pivot : transform).position, 0.12f);
    }
#endif
}
